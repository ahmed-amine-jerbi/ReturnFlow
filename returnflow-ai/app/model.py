"""Claim analysis at request time. Training lives in train.py."""

import json
from pathlib import Path

import joblib
import numpy as np
from sklearn.linear_model import LogisticRegression
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

from .readability import UNREADABLE, is_unreadable
from .schemas import AnalyzeRequest, AnalyzeResponse
from .synthetic import CATEGORIES, RISK_FEATURES

MODEL_VERSION = "synthetic-lr-2.0"
MODELS_DIR = Path(__file__).resolve().parent.parent / "models"
MODEL_PATH = MODELS_DIR / "model.joblib"
METRICS_PATH = MODELS_DIR / "metrics.json"
MAX_DESCRIPTION_CHARS = 1000


def risk_feature_row(req: AnalyzeRequest, category: str, description_words: int) -> np.ndarray:
    """One row in RISK_FEATURES order, shared by training data and live requests."""
    h = req.history
    category_onehot = [1.0 if c == category else 0.0 for c in CATEGORIES]
    row = [
        req.amount_requested,
        h.previous_claims,
        h.previous_rejected,
        h.account_age_days,
        h.claims_last_30_days,
        description_words,
        *category_onehot,
    ]
    if len(row) != len(RISK_FEATURES):
        raise ValueError(f"Risk row has {len(row)} values but RISK_FEATURES lists {len(RISK_FEATURES)}")
    return np.array([row], dtype=float)


class ClaimAnalyzer:
    """Classifies what a claim is about and scores how risky it is. It never decides; the API's policy does."""

    def __init__(self) -> None:
        if not MODEL_PATH.exists():
            from .train import train  # trains on first start when no model was baked into the image

            train()
        bundle = joblib.load(MODEL_PATH)
        self.text_model: Pipeline = bundle["text"]
        self.risk_model: Pipeline = bundle["risk"]
        self.metrics = json.loads(METRICS_PATH.read_text()) if METRICS_PATH.exists() else {}

        word_features = self.text_model.named_steps["features"].transformer_list[0][1]
        self.known_words = {term for term in word_features.vocabulary_ if " " not in term}

    def analyze(self, req: AnalyzeRequest) -> AnalyzeResponse:
        text = req.description.strip()[:MAX_DESCRIPTION_CHARS]

        # The model only maps text to a label; it never follows instructions written in the text.
        if is_unreadable(text, self.known_words):
            category, confidence = UNREADABLE, 1.0
        else:
            probabilities = self.text_model.predict_proba([text])[0]
            best = int(np.argmax(probabilities))
            category = str(self.text_model.classes_[best])
            confidence = float(probabilities[best])

        features = risk_feature_row(req, category, len(text.split()))
        risk = float(self.risk_model.predict_proba(features)[0, 1])

        return AnalyzeResponse(
            claim_id=req.claim_id,
            category=category,
            confidence=round(confidence, 3),
            risk_score=round(risk, 3),
            reasons=self._risk_reasons(features),
            model_version=MODEL_VERSION,
        )

    def _risk_reasons(self, features: np.ndarray) -> list[str]:
        """Up to three features that pushed the risk score up the most, in plain words."""
        scaler: StandardScaler = self.risk_model.named_steps["scale"]
        classifier: LogisticRegression = self.risk_model.named_steps["clf"]
        contributions = classifier.coef_[0] * scaler.transform(features)[0]  # log-odds per feature
        raw = dict(zip(RISK_FEATURES, features[0], strict=True))

        labels = {
            "amount": lambda: f"High claimed amount ({raw['amount']:.0f})",
            "previous_rejected": lambda: f"{int(raw['previous_rejected'])} earlier claim(s) were rejected",
            "previous_claims": lambda: f"{int(raw['previous_claims'])} earlier claims from this customer",
            "claims_last_30d": lambda: f"{int(raw['claims_last_30d'])} other claim(s) in the last 30 days",
            "account_age_days": lambda: f"New account ({int(raw['account_age_days'])} day(s) old)",
            "description_words": lambda: "Very short description",
        }
        reasons: list[str] = []
        for i in np.argsort(-contributions):
            name = RISK_FEATURES[i]
            if contributions[i] < 0.25 or name not in labels:
                continue
            reasons.append(labels[name]())
            if len(reasons) == 3:
                break
        return reasons
