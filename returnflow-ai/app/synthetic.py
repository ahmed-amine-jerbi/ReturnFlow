"""Synthetic training data.

The data is generated, not real. It exists so the pipeline (features -> model -> API -> policy ->
KPIs) works end to end. Claim text is built from hand-written phrases for each cause, combined
with products, openers, closers, casing and typos. Accuracy is measured on a separately written
set of realistic claims (`data/eval_claims.jsonl`), never on these phrases. Replace this with real
labelled claims before trusting any number in production.
"""

import numpy as np

# Order matters: the risk model one-hot encodes categories in this order.
CATEGORIES = [
    "damaged",
    "defective",
    "wrong_item",
    "missing_parts",
    "not_as_described",
    "late_delivery",
    "not_delivered",
    "change_of_mind",
    "customer_damage",
    "other",
]

# `{p}` is replaced by a product name. Phrases are lower case; casing is varied at generation time.
PHRASES: dict[str, list[str]] = {
    # Arrived damaged: the damage happened before or during delivery.
    "damaged": [
        "the {p} was cracked inside the box when it was delivered",
        "the van driver dropped the parcel and the {p} broke",
        "the rain got into the parcel the courier left outside and the {p} is ruined",
        "it came smashed, the courier must have thrown it",
        "the {p} was already dented when the driver handed it over",
        "the {p} was delivered broken",
        "the {p} arrived damaged",
        "the {p} came with a cracked screen",
        "the box was crushed and the {p} inside is broken",
        "it was already broken when i opened the package",
        "arrived scratched and dented",
        "the package was torn open and the {p} is smashed",
        "the corner is bent, it must have happened in shipping",
        "the glass was shattered on arrival",
        "deep scratch across the front straight out of the box",
        "the casing was cracked when it arrived",
        "the parcel looked fine but the {p} inside was broken",
        "one of the legs was already snapped when i unpacked it",
        "the courier clearly dropped the parcel, the {p} is broken",
        "the delivery driver threw the box and now the {p} is damaged",
        "it was damaged in transit",
        "the bottle leaked all over the box during shipping",
        "the {p} arrived in pieces",
        "the {p} has a big dent from the delivery",
        "the packaging was soaked and the {p} is ruined",
        "broken on arrival, the box had a hole in it",
        "the screen was cracked before i even turned it on",
        "the frame came bent out of shape",
        "the {p} was smashed in the post",
        "it was chipped when it arrived",
        "the parcel was crushed by the carrier",
        "it arrived with a crack in the glass",
        "the handle was broken off when it arrived",
        "the delivery guy dropped it at my door and it broke",
        "the box arrived wet and the {p} is water damaged",
        "scuffed and scratched, it was mishandled in shipping",
        "the {p} came damaged and the box was destroyed",
        "opened the box and the {p} was cracked",
        "the shipping box was dented and so is the {p}",
        "the {p} was broken in the parcel",
        "the postman damaged the package",
        "received it with a shattered screen",
    ],
    # Fails through no fault of the customer.
    "defective": [
        "the {p} lost power after a few uses",
        "it stopped working properly after three uses even though i followed the instructions",
        "the {p} leaks every time i use it",
        "it lost suction after a week of normal use",
        "the {p} works for a minute and then dies",
        "the colour faded after one wash on the setting the label says",
        "it keeps beeping with an error",
        "the {p} stopped working after two days",
        "it does not turn on",
        "the battery drains in an hour",
        "it keeps freezing and restarting",
        "the motor makes a grinding noise and then stops",
        "the button does not respond",
        "the charging port does not work",
        "faulty from the first use",
        "it worked once and now it will not start",
        "the battery will not hold a charge",
        "it shuts itself off every few minutes",
        "the left earbud has no sound",
        "the controls are unresponsive even after a reset",
        "it overheats during normal use",
        "it arrived in perfect condition but does not work",
        "the zipper broke the first time i used it",
        "it keeps disconnecting from bluetooth",
        "the display flickers",
        "the seams came apart after one gentle wash following the care label",
        "it stopped working on its own after a week of normal use",
        "dead on arrival, no power at all",
        "the {p} makes a buzzing noise and cuts out",
        "one of the speakers crackles",
        "the app cannot connect to the {p}",
        "the heating element never gets warm",
        "the strap broke while i was wearing it normally",
        "the sole came off after a few days of normal walking",
        "the {p} leaks even though i used it as instructed",
        "manufacturing defect, the stitching is coming undone",
        "it broke by itself, i did not do anything to it",
        "the touchscreen does not register taps",
        "the wifi keeps dropping out",
        "it stopped charging after a week",
        "it shows an error code every time it starts",
        "the lid does not close properly, factory fault",
        "the {p} randomly turns off",
        "half the keys do not work",
        "the {p} is faulty",
        "it is not working properly",
        "the fan is extremely loud and rattles",
    ],
    "wrong_item": [
        "the package had a {p} in it but i ordered something else",
        "this is not my order at all",
        "the box has someone else's item in it",
        "i ordered a rug and got a lamp",
        "you sent me the wrong product entirely",
        "the model in the box is not the model on my order",
        "i ordered the large and you sent the small",
        "i received the wrong item",
        "you sent a different size",
        "the wrong colour was delivered",
        "this is not the {p} i ordered",
        "i got a different model instead",
        "you sent me blue instead of black",
        "the wrong item was in the box",
        "i ordered a small but received an extra large",
        "the label matches my order but the product inside is different",
        "this is a different model number from my order",
        "the box contains someone else's order",
        "i received a {p} i never ordered",
        "i ordered the 128gb version and got the 64gb one",
        "i got the men's version instead of the women's",
        "i ordered size 42 and received 39",
        "the colour is not the one i selected",
        "i asked for the matte finish and got glossy",
        "the wrong flavour was sent",
        "this is the old version, not the one i ordered",
        "i ordered a table and received a chair",
        "the item does not match my order confirmation",
        "you shipped the wrong variant",
        "i received a completely different product",
        "it is the wrong size, i ordered a medium",
        "you sent the us plug version instead of the eu one",
        "got the wrong {p}",
        "the product i received is not what i bought",
    ],
    "missing_parts": [
        "the {p} arrived with three legs but it needs four",
        "the set came without one of the pieces",
        "the cushions were not delivered with the sofa",
        "the knife set is missing one of the knives",
        "only part of the {p} was in the box",
        "the second box of the {p} never came, only the first",
        "the remote is not in the box",
        "it came without the batteries it says are included",
        "the second item of my order is missing",
        "only one of the two items arrived",
        "the charger was not in the box",
        "parts are missing so i cannot assemble it",
        "no screws were included",
        "the manual and the remote are missing",
        "the set should have six glasses but there are only four",
        "the box was incomplete",
        "the cable is missing",
        "i only got three of the four chairs",
        "the lid was not included",
        "half of my order is missing",
        "the accessories listed are not in the package",
        "the battery was missing from the box",
        "one shoe was missing",
        "the {p} came without the stand",
        "a piece is missing from the kit",
        "the package only had the case, not the {p}",
        "the mounting hardware is missing",
        "the bundle was supposed to include a case but it did not",
        "i received the jacket but not the trousers from the same order",
        "there should be two shelves but i only got one",
        "the {p} is missing its power adapter",
        "not everything i ordered was in the parcel",
        "some pieces of the set are missing",
    ],
    "not_as_described": [
        "sold as real marble but it is resin",
        "it was sold as noise cancelling but it has no noise cancelling",
        "sold as stainless steel but it is already rusting",
        "the listing says one thing and the {p} is another",
        "these are fake, the serial number does not exist",
        "the size guide is wrong, it is much longer than it says",
        "the page said 8gb but it only has 4gb",
        "it is not the quality shown in the photos",
        "the description promised features it does not have",
        "it does not match the description",
        "it looks different from the photos",
        "the material is not as advertised",
        "the listing said original but it looks fake",
        "it is much smaller than the listing said",
        "the quality is far below what was shown",
        "the specs are different from the product page",
        "it is not what the seller promised",
        "the colour is much darker than it looked online",
        "the fabric is thinner than described",
        "the photos show features this one does not have",
        "the measurements do not match the size chart",
        "the listing said leather but it is plastic",
        "it was advertised as waterproof but it is not",
        "the dimensions in the listing are wrong",
        "the {p} is a counterfeit",
        "the battery life is nowhere near what was advertised",
        "described as solid wood but it is particle board",
        "the {p} looks cheap compared to the pictures",
        "the listing said it fits a 15 inch laptop and it does not",
        "it is not compatible even though the page said it was",
        "the resolution is lower than the description says",
        "the capacity is half of what was listed",
        "described as new but it was clearly used",
        "the product page is misleading",
    ],
    "late_delivery": [
        "it came after the school term had already started",
        "it arrived the day after the wedding",
        "it got here eventually but weeks later than the estimate",
        "the {p} turned up late, after i had already bought another one",
        "delivery was days late",
        "the package arrived two weeks late",
        "the delivery took far too long",
        "it arrived after the event i needed it for",
        "the parcel was delayed by ten days",
        "shipping was much slower than promised",
        "it arrived well past the delivery date",
        "it came late with no tracking updates",
        "i waited a month for this order to arrive",
        "it was delivered three days after i needed it",
        "the courier kept postponing and it came late",
        "it came too late for the birthday",
        "it finally showed up a week after the promised date",
        "it arrived too late, i needed it for christmas",
        "i paid for express shipping but it took nine days",
        "it arrived after the trip i bought it for",
        "delivered late so it is no longer useful",
        "the {p} arrived days after the estimated date",
    ],
    "not_delivered": [
        "i didnt get it",
        "never received",
        "the proof of delivery photo shows a door that is not mine",
        "the courier says they left it in my safe place but it is not there",
        "it says delivered but there is nothing at my door",
        "the parcel went missing",
        "i still have not got my {p}",
        "where is my order, it never came",
        "my order never arrived",
        "the parcel is lost",
        "tracking says delivered but i never received it",
        "the package has been stuck in transit for a month",
        "it was sent to the wrong address",
        "nothing has arrived",
        "i am still waiting, it never came",
        "the courier says it was delivered but it is not here",
        "i have not received my order at all",
        "the parcel was returned to the sender without reaching me",
        "the {p} was never delivered",
        "no package and no updates for weeks",
        "the delivery never happened",
        "the tracking has not moved in three weeks",
        "the order shows as delivered in a different city",
        "it was taken from the parcel locker before i could collect it",
        "i never got the {p}",
    ],
    # The product is fine; the customer no longer wants it.
    "change_of_mind": [
        "it is exactly as pictured, i just do not like how it looks in the room",
        "someone else in my family bought the same one, we only need one",
        "my son wanted a different colour, there is nothing wrong with this one",
        "i saw it on sale for less after i bought it",
        "we got a better offer elsewhere and want to return it unopened",
        "duplicate order, please refund one of them",
        "it is fine, it is just not for me",
        "i do not need it any more",
        "i picked the wrong size when i ordered",
        "it does not look good on me",
        "i changed my mind",
        "i no longer need it",
        "i found it cheaper elsewhere",
        "i do not like it anymore",
        "i bought it by mistake",
        "it does not suit my style",
        "i decided i do not want it",
        "my plans changed",
        "i will keep using my old one",
        "i ordered two by accident",
        "it is too small for me",
        "it does not fit me",
        "it is too big for my space",
        "it was a gift and they already have one",
        "i do not like the colour on me",
        "not what i expected but there is nothing wrong with it",
        "i would rather have a different model",
        "the {p} is fine, i just do not want it",
        "it does not go with my furniture",
        "i ordered the wrong size myself",
        "i accidentally ordered it twice",
        "it is still unopened, i do not need it",
        "it is too heavy for me to use",
        "it is uncomfortable for me",
        "i prefer the one i already had",
    ],
    # Damaged after delivery by the customer, their household, pets or misuse.
    "customer_damage": [
        "i dropped my {p} the day after it arrived and it cracked",
        "a few days after delivery i dropped it and now it is broken",
        "my partner dropped the {p} and broke it",
        "the {p} slipped off the sofa and the screen cracked",
        "i tumble dried it even though the label says not to",
        "i ignored the care instructions and ruined it",
        "i used it wrong and broke it",
        "i used a charger from my old phone and now it is dead",
        "i broke it by accident while using it",
        "it fell off my desk and cracked",
        "i damaged it myself",
        "my child dropped it in the bath",
        "i dropped it and now it is broken",
        "i dropped it by accident and the screen cracked",
        "it fell off the table and stopped working",
        "i spilled coffee on it",
        "i spilled water on it and now it will not turn on",
        "i sat on them and they snapped",
        "my dog chewed the cable",
        "my son broke it",
        "my kids broke the handle",
        "i stepped on it",
        "i washed it on hot and it shrank",
        "i used the wrong charger and it burned out",
        "i cracked it while installing it",
        "it slipped out of my hand and broke",
        "i knocked it over",
        "it got soaked in the rain",
        "i left it in the car and it melted",
        "i accidentally broke it",
        "my cat knocked it off the shelf",
        "i put it in the dishwasher and it warped",
        "i tried to repair it myself and broke it",
        "it broke when i dropped it down the stairs",
        "i overloaded it and the motor burned out",
        "it fell into the pool",
        "i bent it trying to force it in",
        "dropped the {p} on the floor and now it does not work",
        "my fault, i broke it",
        "i scratched it with my keys",
        "the dog got to it",
        "it broke after i dropped it",
        "i spilled juice on the {p}",
        "i fell on it and it broke",
        "my toddler threw it and it cracked",
        "i plugged it into the wrong voltage",
        "the {p} fell out of my pocket and cracked",
    ],
    "other": [
        "how long does a refund take to show on my card",
        "i submitted a claim last week and have not heard back",
        "i am following up as you asked",
        "do you collect returns or do i drop it off",
        "the product is not ok",
        "something is wrong",
        "i am not sure what is wrong with it",
        "can you tell me where my refund is",
        "thanks for your reply",
        "i want to talk to someone",
        "please help with my order",
        "i have a question about my refund",
        "i want my money back",
        "i need assistance",
        "please contact me about this order",
        "there is an issue with my purchase",
        "how does the return process work",
        "when will i get my refund",
        "please call me",
        "can you check the status",
        "i need an update",
        "what happens next",
        "there is a problem",
        "not happy",
        "refund please",
        "see attached",
        "i would like to return this",
        "hello",
        "can i exchange this",
        "please advise",
        "return",
        "see my previous message",
    ],
}

PRODUCTS = [
    "phone",
    "laptop",
    "jacket",
    "headphones",
    "shoes",
    "blender",
    "watch",
    "tablet",
    "backpack",
    "lamp",
    "sofa",
    "chair",
    "mug",
    "kettle",
    "speaker",
    "camera",
    "dress",
    "toy",
    "monitor",
    "keyboard",
    "vacuum",
    "printer",
    "earbuds",
    "coffee machine",
    "mirror",
    "bike helmet",
]
OPENERS = [
    "",
    "",
    "",
    "hi,",
    "hello,",
    "hey,",
    "dear support,",
    "unfortunately",
    "sorry but",
    "not happy:",
    "order issue:",
    "good morning,",
]
CLOSERS = [
    "",
    "",
    "",
    "please refund me",
    "this is very disappointing",
    "thank you",
    "please help",
    "i want a refund",
    "what can you do?",
    "photos attached",
    "i want my money back",
    "please sort this out",
    "thanks in advance",
]


def make_text_dataset(n_per_class: int = 400, seed: int = 7) -> tuple[list[str], list[str], list[str]]:
    """Returns texts, labels, and a group id per text (its source phrase) for leak-free splits."""
    rng = np.random.default_rng(seed)
    texts: list[str] = []
    labels: list[str] = []
    groups: list[str] = []
    for category in CATEGORIES:
        phrases = PHRASES[category]
        for _ in range(n_per_class):
            index = int(rng.integers(len(phrases)))
            texts.append(_compose(phrases[index], rng))
            labels.append(category)
            groups.append(f"{category}-{index}")
    return texts, labels, groups


def _compose(phrase: str, rng: np.random.Generator) -> str:
    body = phrase.replace("{p}", str(rng.choice(PRODUCTS)))
    parts = [str(rng.choice(OPENERS)), body, str(rng.choice(CLOSERS))]
    text = " ".join(part for part in parts if part)
    if rng.random() < 0.3:
        text = _add_typo(text, rng)
    style = rng.random()
    if style < 0.45:
        text = text[:1].upper() + text[1:]
    elif style < 0.5:
        text = text.upper()
    if rng.random() < 0.4:
        text += "."
    return text


def _add_typo(text: str, rng: np.random.Generator) -> str:
    """Swaps or drops one letter in a longer word, as people do when typing on a phone."""
    words = text.split()
    candidates = [i for i, word in enumerate(words) if len(word) > 4]
    if not candidates:
        return text
    i = int(rng.choice(candidates))
    word = words[i]
    j = int(rng.integers(1, len(word) - 2))
    swapped = word[:j] + word[j + 1] + word[j] + word[j + 2 :]
    dropped = word[:j] + word[j + 1 :]
    words[i] = swapped if rng.random() < 0.5 else dropped
    return " ".join(words)


RISK_FEATURES = [
    "amount",
    "previous_claims",
    "previous_rejected",
    "account_age_days",
    "claims_last_30d",
    "description_words",
] + [f"cat_{c}" for c in CATEGORIES]

# How much each category raises or lowers the hidden "suspicious" score. Same order as CATEGORIES.
_CATEGORY_RISK = np.array([0.1, 0.0, -0.1, 0.2, 0.5, -0.4, 0.6, -0.3, 0.3, 0.2])


def make_risk_dataset(n: int = 5000, seed: int = 11) -> tuple[np.ndarray, np.ndarray]:
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

    latent = (
        -3.0
        + 0.004 * amount
        + 0.30 * previous_claims
        + 0.95 * previous_rejected
        + 0.70 * claims_last_30d
        - 0.0022 * np.minimum(account_age_days, 730)
        + 0.8 * (description_words < 4)
        + _CATEGORY_RISK[categories]
    )
    p = 1 / (1 + np.exp(-latent))
    y = (rng.uniform(size=n) < p).astype(int)

    X = np.column_stack(
        [
            amount,
            previous_claims,
            previous_rejected,
            account_age_days,
            claims_last_30d,
            description_words,
            cat_onehot,
        ]
    )
    return X, y
