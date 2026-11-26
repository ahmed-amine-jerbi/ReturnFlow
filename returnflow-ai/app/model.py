import json
from pathlib import Path

import joblib
import numpy as np
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import accuracy_score, precision_score, recall_score, roc_auc_score
from sklearn.model_selection import GroupShuffleSplit, train_test_split
from sklearn.pipeline import Pipeline
from sklearn.preprocessing import StandardScaler

from .schemas import AnalyzeRequest, AnalyzeResponse
from .synthetic import CATEGORIES, RISK_FEATURES, make_risk_dataset, make_text_dataset

MODEL_VERSION = "synthetic-lr-1.1"
MODEL_PATH = Path(__file__).resolve().parent.parent / "models" / "model.joblib"
METRICS_PATH = MODEL_PATH.with_name("metrics.json")

# Policy-style thresholds for the AI's *recommendation*. The final routing is decided by the API.
APPROVE_MAX_RISK = 0.35


def train() -> dict:
    """Train both models on synthetic data, save them, and return evaluation metrics."""
    texts, labels, groups = make_text_dataset()
    train_indices, test_indices = next(
        GroupShuffleSplit(n_splits=1, test_size=0.25, random_state=3).split(texts, labels, groups)
    )
    tx_train = [texts[i] for i in train_indices]
    tx_test = [texts[i] for i in test_indices]
    ty_train = [labels[i] for i in train_indices]
    ty_test = [labels[i] for i in test_indices]
    text_model = Pipeline(
        [
            ("tfidf", TfidfVectorizer(ngram_range=(1, 2), min_df=1, sublinear_tf=True)),
            ("clf", LogisticRegression(max_iter=2000, C=3.0)),
        ]
    )
    text_model.fit(tx_train, ty_train)
    text_accuracy = accuracy_score(ty_test, text_model.predict(tx_test))

    X, y = make_risk_dataset()
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.25, random_state=5, stratify=y)
    risk_model = Pipeline(
        [
            ("scale", StandardScaler()),
            ("clf", LogisticRegression(max_iter=2000, C=1.0)),
        ]
    )
    risk_model.fit(X_train, y_train)
    proba = risk_model.predict_proba(X_test)[:, 1]
    pred = (proba >= 0.5).astype(int)
    decision_pred = (proba >= APPROVE_MAX_RISK).astype(int)

    metrics = {
        "modelVersion": MODEL_VERSION,
        "dataSource": "synthetic",
        "text": {
            "accuracy": round(float(text_accuracy), 3),
            "classes": CATEGORIES,
            "testTemplates": len({groups[i] for i in test_indices}),
        },
        "risk": {
            "rocAuc": round(float(roc_auc_score(y_test, proba)), 3),
            "precision": round(float(precision_score(y_test, pred, zero_division=0)), 3),
            "recall": round(float(recall_score(y_test, pred, zero_division=0)), 3),
            "precisionAtDecisionThreshold": round(float(precision_score(y_test, decision_pred, zero_division=0)), 3),
            "recallAtDecisionThreshold": round(float(recall_score(y_test, decision_pred, zero_division=0)), 3),
            "decisionThreshold": APPROVE_MAX_RISK,
            "positiveRate": round(float(np.mean(y)), 3),
            "testSamples": len(y_test),
        },
    }
    MODEL_PATH.parent.mkdir(parents=True, exist_ok=True)
    joblib.dump({"text": text_model, "risk": risk_model}, MODEL_PATH)
    METRICS_PATH.write_text(json.dumps(metrics, indent=2))
    return metrics


class ClaimAnalyzer:
    def __init__(self) -> None:
        if not MODEL_PATH.exists():
            train()
        bundle = joblib.load(MODEL_PATH)
        self.text_model: Pipeline = bundle["text"]
        self.risk_model: Pipeline = bundle["risk"]
        self.metrics = json.loads(METRICS_PATH.read_text()) if METRICS_PATH.exists() else {}

    def analyze(self, req: AnalyzeRequest) -> AnalyzeResponse:
        text = req.description.strip()[:1000]

        # 1) Category from the description. The model only maps text to a label; it never follows instructions in it.
        if text:
            proba = self.text_model.predict_proba([text])[0]
            idx = int(np.argmax(proba))
            category = str(self.text_model.classes_[idx])
            category_conf = float(proba[idx])
        else:
            category, category_conf = "other", 0.0

        # 2) Risk score from claim facts and history.
        h = req.history
        words = len(text.split())
        cat_vec = [1.0 if c == category else 0.0 for c in CATEGORIES]
        x = np.array(
            [
                [
                    req.amount_requested,
                    h.previous_claims,
                    h.previous_rejected,
                    h.account_age_days,
                    h.claims_last_30_days,
                    words,
                    *cat_vec,
                ]
            ],
            dtype=float,
        )
        risk = float(self.risk_model.predict_proba(x)[0, 1])

        reasons = self._reasons(x, category, category_conf)

        # 3) Confidence: how far the risk is from the 0.5 coin-flip, blended with the text confidence.
        risk_certainty = 1.0 - 2.0 * min(risk, 1.0 - risk)
        confidence = 0.6 * risk_certainty + 0.4 * category_conf

        # 4) Recommendation. The AI can approve or ask for a human, but never rejects by itself.
        valid_category = category not in ("other", "change_of_mind")
        recommended = "Approve" if (risk < APPROVE_MAX_RISK and valid_category) else "Review"
        if recommended == "Review" and not reasons:
            reasons = ["Claim reason is not covered by the automatic approval rules"]

        return AnalyzeResponse(
            claim_id=req.claim_id,
            category=category,
            category_confidence=round(category_conf, 3),
            risk_score=round(risk, 3),
            confidence=round(float(confidence), 3),
            recommended_outcome=recommended,
            reasons=reasons,
            model_version=MODEL_VERSION,
        )

    def _reasons(self, x: np.ndarray, category: str, category_conf: float) -> list[str]:
        scaler: StandardScaler = self.risk_model.named_steps["scale"]
        clf: LogisticRegression = self.risk_model.named_steps["clf"]
        contrib = clf.coef_[0] * scaler.transform(x)[0]  # log-odds contribution per feature
        raw = dict(zip(RISK_FEATURES, x[0], strict=True))

        labels = {
            "amount": lambda: f"High claimed amount ({raw['amount']:.0f})",
            "previous_rejected": lambda: f"{int(raw['previous_rejected'])} earlier claim(s) were rejected",
            "previous_claims": lambda: f"{int(raw['previous_claims'])} earlier claims from this customer",
            "claims_last_30d": lambda: f"{int(raw['claims_last_30d'])} other claim(s) in the last 30 days",
            "account_age_days": lambda: f"New account ({int(raw['account_age_days'])} day(s) old)",
            "description_words": lambda: "Very short description",
        }
        order = np.argsort(-contrib)
        reasons: list[str] = []
        for i in order:
            name = RISK_FEATURES[i]
            if contrib[i] < 0.25 or name not in labels:
                continue
            reasons.append(labels[name]())
            if len(reasons) == 3:
                break
        if category_conf < 0.45:
            reasons.append("Description is unclear, so the category is uncertain")
        return reasons
