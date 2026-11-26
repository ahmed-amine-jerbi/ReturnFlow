"""Run `python -m app.train` to (re)train the models and print the metrics."""

import json

from .model import train

if __name__ == "__main__":
    print(json.dumps(train(), indent=2))
