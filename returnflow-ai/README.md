# returnflow-ai

Claim analysis service for ReturnFlow. Given a claim and the customer's history it returns:

- a **category**: `damaged` (arrived damaged), `defective`, `wrong_item`, `missing_parts`, `not_as_described`, `late_delivery`, `not_delivered`, `change_of_mind`, `customer_damage` (damaged after delivery by the customer), or `other`
- a **confidence**: the probability that the category is right
- a **risk score** from 0 to 1 and a list of human-readable **reasons**

It never decides. The API's policy turns this into approve, reject or send to an agent.

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

- Text: TF-IDF on words (1-2 grams) and letter fragments (2-5 characters, so typos still match) + logistic regression. Trained on claims generated from hand-written phrases (`app/synthetic.py`).
- Risk: standardized features + logistic regression, so the contribution of each feature gives the reasons. Labels come from a hidden rule plus noise.

`python -m app.train` (`app/train.py`) writes `models/metrics.json` with three measurements:

| Set | What it is | Used for |
|---|---|---|
| `syntheticHoldout` | Generated claims from phrases held out of training | Choosing the confidence thresholds |
| `dev` | 150 of the 300 realistic claims in `data/eval_claims.jsonl`, written separately from the training phrases | Choosing the regularization, studying mistakes. Optimistic. |
| `test` | The other 150, never used for any choice | The number to quote: 82% correct, 54% confident, 97.5% correct when confident |

Once the test half has been looked at, changing the training data because of its mistakes would make it optimistic too; a new test set is needed for the next round. Replace the generated data with real labelled claims before relying on any of this.

## Safety

Claim text is only ever mapped to a label by the classifier. The service does not follow instructions found in it, and its response has no decision field to influence (covered by tests).

## License

MIT
