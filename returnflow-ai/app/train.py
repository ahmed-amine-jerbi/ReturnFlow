"""Train the claim models and measure them honestly.

Run `python -m app.train` to retrain and print the metrics.

Three kinds of evaluation, written to models/metrics.json:
- syntheticHoldout: generated claims whose phrases were held out of training. The confidence
  thresholds are chosen here: unseen phrasing is the case they must protect against.
- dev: half of the realistic, independently written claims in data/eval_claims.jsonl. Used to choose
  the regularization strength and to study mistakes while writing training phrases, so its numbers
  are optimistic.
- test: the other half, never used for any choice. This is the number to quote.
"""

import json
from pathlib import Path

import joblib
import numpy as np
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.linear_model import LogisticRegression
from sklearn.metrics import accuracy_score, precision_score, recall_score, roc_auc_score
from sklearn.model_selection import GroupShuffleSplit, train_test_split
from sklearn.pipeline import FeatureUnion, Pipeline
from sklearn.preprocessing import StandardScaler

from .model import METRICS_PATH, MODEL_PATH, MODEL_VERSION
from .synthetic import CATEGORIES, make_risk_dataset, make_text_dataset

EVAL_PATH = Path(__file__).resolve().parent.parent / "data" / "eval_claims.jsonl"

# Share of automatic decisions that must be right; the confidence thresholds are chosen to meet it.
TARGET_APPROVE_ACCURACY = 0.97
TARGET_REJECT_PRECISION = 0.98
REJECT_CATEGORY = "customer_damage"
RISK_DECISION_THRESHOLD = 0.35
C_CANDIDATES = (1.0, 3.0, 10.0, 30.0)


def train() -> dict:
    texts, labels, groups = make_text_dataset()
    dev, test = _load_realistic_split()

    text_model, chosen_c = _fit_text_model(texts, labels, dev)
    risk_model, risk_metrics = _fit_risk_model()

    holdout_model, holdout = _synthetic_holdout(texts, labels, groups, chosen_c)
    thresholds = _choose_thresholds(holdout_model, holdout)
    metrics = {
        "modelVersion": MODEL_VERSION,
        "dataSource": "synthetic training data; realistic evaluation set written separately",
        "categories": CATEGORIES,
        "thresholds": thresholds,
        "text": {
            "regularizationC": chosen_c,
            "syntheticHoldout": _evaluate(holdout_model, holdout, thresholds, include_mistakes=False),
        },
        "risk": risk_metrics,
    }
    if dev and test:
        metrics["text"]["dev"] = _evaluate(text_model, dev, thresholds)
        metrics["text"]["test"] = _evaluate(text_model, test, thresholds)

    MODEL_PATH.parent.mkdir(parents=True, exist_ok=True)
    joblib.dump({"text": text_model, "risk": risk_model}, MODEL_PATH)
    METRICS_PATH.write_text(json.dumps(metrics, indent=2) + "\n", encoding="utf-8")
    return metrics


def _new_text_model(c: float) -> Pipeline:
    return Pipeline(
        [
            (
                "features",
                FeatureUnion(
                    [
                        ("words", TfidfVectorizer(ngram_range=(1, 2), sublinear_tf=True, min_df=2)),
                        # Letter fragments keep typos and unseen word forms recognizable.
                        ("chars", TfidfVectorizer(analyzer="char_wb", ngram_range=(2, 5), sublinear_tf=True, min_df=2)),
                    ]
                ),
            ),
            ("clf", LogisticRegression(max_iter=3000, C=c)),
        ]
    )


def _fit_text_model(texts: list[str], labels: list[str], dev: list[tuple[str, str]]) -> tuple[Pipeline, float]:
    """Fits one model per C candidate and keeps the best on the dev half (or C=10 without one)."""
    if not dev:
        return _new_text_model(10.0).fit(texts, labels), 10.0

    dev_texts = [t for t, _ in dev]
    dev_labels = [label for _, label in dev]
    best_model, best_c, best_accuracy = None, 0.0, -1.0
    for c in C_CANDIDATES:
        model = _new_text_model(c).fit(texts, labels)
        accuracy = accuracy_score(dev_labels, model.predict(dev_texts))
        if accuracy > best_accuracy:
            best_model, best_c, best_accuracy = model, c, accuracy
    assert best_model is not None
    return best_model, best_c


def _synthetic_holdout(
    texts: list[str], labels: list[str], groups: list[str], c: float
) -> tuple[Pipeline, list[tuple[str, str]]]:
    """A model trained without a quarter of the phrases, and the claims built from those phrases."""
    split = GroupShuffleSplit(n_splits=1, test_size=0.25, random_state=3)
    train_idx, test_idx = next(split.split(texts, labels, groups))
    model = _new_text_model(c).fit([texts[i] for i in train_idx], [labels[i] for i in train_idx])
    return model, [(texts[i], labels[i]) for i in test_idx]


def _load_realistic_split() -> tuple[list[tuple[str, str]], list[tuple[str, str]]]:
    if not EVAL_PATH.exists():
        return [], []
    rows = [json.loads(line) for line in EVAL_PATH.read_text(encoding="utf-8").splitlines() if line.strip()]
    pairs = [(row["text"], row["label"]) for row in rows]
    dev, test = train_test_split(pairs, test_size=0.5, random_state=13, stratify=[label for _, label in pairs])
    return list(dev), list(test)


def _predict(model: Pipeline, rows: list[tuple[str, str]]) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    probabilities = model.predict_proba([text for text, _ in rows])
    best = probabilities.argmax(axis=1)
    predicted = model.classes_[best]
    confidence = probabilities[np.arange(len(rows)), best]
    actual = np.array([label for _, label in rows])
    return predicted, confidence, actual


def _choose_thresholds(model: Pipeline, rows: list[tuple[str, str]]) -> dict:
    """Lowest confidence at which automatic decisions on `rows` still meet the accuracy targets."""
    predicted, confidence, actual = _predict(model, rows)
    candidates = np.round(np.arange(0.30, 0.96, 0.01), 2)

    approve = next(
        (
            float(t)
            for t in candidates
            if (confidence >= t).sum() > 0 and (predicted == actual)[confidence >= t].mean() >= TARGET_APPROVE_ACCURACY
        ),
        0.95,
    )
    is_reject = predicted == REJECT_CATEGORY
    reject = next(
        (
            float(t)
            for t in candidates
            if (is_reject & (confidence >= t)).sum() > 0
            and (actual == REJECT_CATEGORY)[is_reject & (confidence >= t)].mean() >= TARGET_REJECT_PRECISION
        ),
        0.95,
    )
    # A wrong rejection hurts a customer more than an extra review, so rejecting is never easier.
    return {"autoApprove": approve, "autoReject": max(approve, reject)}


def _evaluate(model: Pipeline, rows: list[tuple[str, str]], thresholds: dict, include_mistakes: bool = True) -> dict:
    predicted, confidence, actual = _predict(model, rows)
    automated = confidence >= thresholds["autoApprove"]
    per_category = {
        c: {
            "precision": _score(precision_score, actual, predicted, c),
            "recall": _score(recall_score, actual, predicted, c),
        }
        for c in CATEGORIES
    }
    mistakes = [
        {"text": text, "actual": a, "predicted": p, "confidence": round(float(conf), 2)}
        for (text, _), a, p, conf in zip(rows, actual, predicted, confidence, strict=True)
        if a != p
    ]
    result = {
        "samples": len(rows),
        "accuracy": round(float((predicted == actual).mean()), 3),
        "confidentShare": round(float(automated.mean()), 3),
        "accuracyWhenConfident": round(float((predicted == actual)[automated].mean()), 3) if automated.any() else None,
        "perCategory": per_category,
    }
    if include_mistakes:
        result["mistakes"] = sorted(mistakes, key=lambda m: -m["confidence"])
    return result


def _score(metric, actual: np.ndarray, predicted: np.ndarray, category: str) -> float:
    return round(float(metric(actual, predicted, labels=[category], average="micro", zero_division=0)), 3)


def _fit_risk_model() -> tuple[Pipeline, dict]:
    X, y = make_risk_dataset()
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.25, random_state=5, stratify=y)
    model = Pipeline([("scale", StandardScaler()), ("clf", LogisticRegression(max_iter=2000, C=1.0))])
    model.fit(X_train, y_train)
    proba = model.predict_proba(X_test)[:, 1]
    flagged = (proba >= RISK_DECISION_THRESHOLD).astype(int)
    return model, {
        "rocAuc": round(float(roc_auc_score(y_test, proba)), 3),
        "precisionAtDecisionThreshold": round(float(precision_score(y_test, flagged, zero_division=0)), 3),
        "recallAtDecisionThreshold": round(float(recall_score(y_test, flagged, zero_division=0)), 3),
        "decisionThreshold": RISK_DECISION_THRESHOLD,
        "positiveRate": round(float(np.mean(y)), 3),
        "testSamples": len(y_test),
    }


if __name__ == "__main__":
    result = train()
    result["text"].get("dev", {}).pop("mistakes", None)
    print(json.dumps(result, indent=2))
