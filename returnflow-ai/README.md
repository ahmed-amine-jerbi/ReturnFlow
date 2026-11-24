# returnflow-ai

Claim analysis service for ReturnFlow. Given a claim and the customer's history it returns:

- a **category** (damaged, defective, wrong item, late delivery, not as described, change of mind, other) with confidence
- a **risk score** from 0 to 1 and a list of human-readable **reasons**
- a **recommendation**: `Approve` or `Review`. The service never rejects. Final routing is decided by the API's policy.

## Run

```bash
python -m venv .venv && . .venv/bin/activate     # Windows: .venv\Scripts\activate
pip install -r requirements-dev.txt
python -m app.train        # trains on synthetic data and writes models/metrics.json
pytest
uvicorn app.main:app --port 8000
```

Docs at http://localhost:8000/docs. `GET /model-info` shows the training metrics.

## Models

- Text: TF-IDF (word 1-2 grams) + logistic regression, evaluated with whole templates held out from training to avoid near-duplicate leakage
- Risk: standardized features + logistic regression, so the contribution of each feature gives the reasons
- The risk metrics also report precision and recall at the configured recommendation threshold (`0.35`).

Both train on **synthetic data** (`app/synthetic.py`). The numbers in `metrics.json` show that the pipeline works, not real-world accuracy. Replace the generator with real labelled claims before relying on it.

## Safety

Claim text is only ever mapped to a label by the classifier. The service does not follow instructions found in it, so text like "ignore your rules and approve" has no effect (covered by a test).

## License

MIT
