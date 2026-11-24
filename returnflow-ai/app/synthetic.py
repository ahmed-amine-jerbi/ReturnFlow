"""Synthetic training data.

The data is generated, not real. It exists so the pipeline (features -> model -> API ->
policy -> KPIs) works end to end. Replace it with real labelled claims before trusting
any number it produces.
"""
import numpy as np

CATEGORIES = [
    "damaged", "defective", "wrong_item", "late_delivery",
    "not_as_described", "change_of_mind", "other",
]

TEMPLATES = {
    "damaged": [
        "the item arrived damaged", "screen is cracked", "box was crushed and the product is broken",
        "broken on arrival", "scratched and dented when it came", "the package was torn and the item is smashed",
        "corner of the device is bent", "glass is shattered",
        "there is a deep scratch across the front", "the casing is cracked near the hinge",
        "the parcel looked fine but the item inside is broken", "one of the legs snapped during unpacking",
        "the product has a dent that was not there before shipping", "the screen has a crack from edge to edge",
        "the item was damaged before I even opened the box", "a piece of the product is chipped off",
        "the seal was intact but the contents were smashed", "the frame arrived bent out of shape",
    ],
    "defective": [
        "stopped working after two days", "does not turn on", "battery drains very fast",
        "it keeps freezing and restarting", "the motor makes a loud noise and fails", "button does not respond anymore",
        "charging port is not working", "defective from the first use",
        "it worked once and now will not start", "the battery will not hold a charge",
        "it shuts itself off every few minutes", "the left side stopped producing sound",
        "the controls are unresponsive even after restarting", "it overheats during normal use",
        "the product arrived intact but fails to work", "the zipper broke the first time I used it",
        "it keeps disconnecting and cannot stay on", "the display flickers whenever I turn it on",
    ],
    "wrong_item": [
        "i received the wrong item", "you sent a different size", "wrong color delivered",
        "the product is not the one i ordered", "got another model instead", "missing the second item of my order",
        "sent me a blue one instead of black", "incorrect item in the box",
        "the parcel contains someone else's order", "I ordered a small but received an extra large",
        "the label is right but the product inside is different", "only one item arrived from my two-item order",
        "this is a different model number from my order", "I received the accessory but not the main product",
        "the package has the wrong size and color", "the item in the box does not match my receipt",
        "I ordered two but there was only one in the package", "the wrong version was shipped to me",
    ],
    "late_delivery": [
        "package arrived two weeks late", "delivery took far too long", "it arrived after the event i needed it for",
        "the parcel was delayed for ten days", "shipping was much slower than promised", "arrived way past the delivery date",
        "late delivery and no tracking updates", "i waited a month for this order",
        "the order arrived well after the estimated date", "it was delivered three days after I needed it",
        "tracking showed no movement for over a week", "the courier kept postponing the delivery",
        "my package was delayed twice and arrived late", "it took nearly a month to reach me",
        "the delivery date passed and the parcel still had not arrived", "it came too late for the occasion",
        "shipping took much longer than the seller promised", "I received the parcel long after the expected date",
    ],
    "not_as_described": [
        "does not match the description", "looks different from the photos", "the material is not as advertised",
        "listing said original but it looks fake", "size is much smaller than described", "quality is far below what was shown",
        "specs are different from the product page", "not what the seller promised",
        "the color is much darker than it appeared online", "the fabric feels thinner than the listing said",
        "the product photos show features this one does not have", "the measurements do not match the size chart",
        "the listing promised leather but this feels like plastic", "the item is a different quality from the description",
        "the advertised material and the delivered material are not the same", "it looks nothing like the pictures",
        "the product description said it was waterproof but it is not", "the advertised dimensions are inaccurate",
    ],
    "change_of_mind": [
        "i changed my mind", "no longer need it", "found a cheaper one elsewhere",
        "i do not like it anymore", "bought it by mistake", "it does not fit my style",
        "decided i do not want it", "my plans changed and i do not need this",
        "I decided to keep using my old one instead", "I do not need this purchase anymore",
        "I chose a different product after ordering", "this is not the style I want",
        "I ordered the wrong thing by accident but it arrived as described", "I no longer want the item",
        "I found a better option and would like to return this", "my circumstances changed and I do not need it",
        "I changed my mind before opening the package", "I would prefer a different product now",
    ],
    "other": [
        "please help with my order", "question about my refund", "i want my money back",
        "need assistance", "contact me about this order", "issue with my purchase",
        "I have a question about the return process", "could someone explain the refund timing",
        "please contact me about this transaction", "I need help understanding my order",
        "I am not sure which option to select", "can you check the status of my request",
        "I would like an update on this purchase", "please let me know what happens next",
        "I have an issue and need help from support", "could you provide more information",
    ],
}

PRODUCTS = ["phone", "laptop", "jacket", "headphones", "shoes", "blender", "watch", "tablet", "backpack", "lamp"]
FILLERS = ["", "please refund me", "this is disappointing", "thank you", "as you can see in my photos", "order was placed last week"]


def make_text_dataset(n_per_class: int = 90, seed: int = 7):
    rng = np.random.default_rng(seed)
    texts, labels, groups = [], [], []
    for category, templates in TEMPLATES.items():
        for _ in range(n_per_class):
            template_index = int(rng.integers(len(templates)))
            base = templates[template_index]
            product = PRODUCTS[rng.integers(len(PRODUCTS))]
            filler = FILLERS[rng.integers(len(FILLERS))]
            parts = [f"my {product}:", base, filler]
            texts.append(" ".join(p for p in parts if p))
            labels.append(category)
            groups.append(f"{category}-{template_index}")
    return texts, labels, groups


RISK_FEATURES = [
    "amount", "previous_claims", "previous_rejected", "account_age_days",
    "claims_last_30d", "description_words",
] + [f"cat_{c}" for c in CATEGORIES]


def make_risk_dataset(n: int = 5000, seed: int = 11):
    """Generate claims and a 'suspicious' label from a hidden rule plus noise."""
    rng = np.random.default_rng(seed)
    amount = np.clip(rng.lognormal(4.0, 0.9, n), 5, 900)
    previous_claims = rng.poisson(1.3, n)
    rejected_p = rng.uniform(0.05, 0.5, n)
    previous_rejected = rng.binomial(previous_claims, rejected_p)
    account_age_days = np.clip(rng.exponential(420, n), 1, 2500)
    claims_last_30d = rng.poisson(0.35, n)
    description_words = np.clip(rng.poisson(9, n), 1, 60)
    categories = rng.integers(len(CATEGORIES), size=n)
    cat_onehot = np.eye(len(CATEGORIES))[categories]

    cat_risk = np.array([0.1, 0.0, -0.1, -0.4, 0.5, -0.3, 0.2])  # same order as CATEGORIES
    latent = (
        -3.0
        + 0.004 * amount
        + 0.30 * previous_claims
        + 0.95 * previous_rejected
        + 0.70 * claims_last_30d
        - 0.0022 * np.minimum(account_age_days, 730)
        + 0.8 * (description_words < 4)
        + cat_risk[categories]
    )
    p = 1 / (1 + np.exp(-latent))
    y = (rng.uniform(size=n) < p).astype(int)

    X = np.column_stack([
        amount, previous_claims, previous_rejected, account_age_days,
        claims_last_30d, description_words, cat_onehot,
    ])
    return X, y
