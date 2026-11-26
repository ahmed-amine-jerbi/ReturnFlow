"""Detects descriptions with no readable content, such as keyboard mashing or random letters.

A description counts as readable when at least one of its words is recognizable: a word the claim
classifier learned, a common English, French or German word (the languages the web app offers), or
one typo away from either. Real claims in any of those languages pass; "zeazeazeaz", "asdfgh" and
"???" do not. The rule is deliberately narrow: vague but real text ("refund please") is readable
and goes through the normal path.
"""

import re
from collections.abc import Iterable

UNREADABLE = "unreadable"

_WORD = re.compile(r"[a-zà-öø-ÿß]+")
_ALPHABET = "abcdefghijklmnopqrstuvwxyzàâäçéèêëîïôöùûüß"

# Short function words that almost every real sentence contains, per supported language.
_COMMON_WORDS = {
    *"""a i it is my me the and not no of to in on was has have with for but this that please order item
    refund broken help return money back received arrived wrong late""".split(),
    *"""le la les un une et est pas je il elle mon ma mes de du des en au avec pour ne commande colis
    cassé reçu arrivé remboursement produit abîmé écran fonctionne merci""".split(),
    *"""der die das und ist nicht ich es mein meine ein eine mit für von zu kaputt bestellung paket
    geliefert erhalten defekt rückerstattung bitte funktioniert falsch artikel beschädigt""".split(),
}


def is_unreadable(text: str, known_words: Iterable[str]) -> bool:
    """True when no word in `text` is recognizable. Empty text and text without letters are unreadable."""
    vocabulary = set(known_words) | _COMMON_WORDS
    words = _WORD.findall(text.lower())
    return not any(_is_recognizable(word, vocabulary) for word in words)


def _is_recognizable(word: str, vocabulary: set[str]) -> bool:
    if word in vocabulary:
        return True
    # Allow one typo, but only in longer words: short random strings would match too easily.
    return len(word) >= 4 and any(variant in vocabulary for variant in _one_edit_away(word))


def _one_edit_away(word: str) -> set[str]:
    splits = [(word[:i], word[i:]) for i in range(len(word) + 1)]
    deletes = {left + right[1:] for left, right in splits if right}
    swaps = {left + right[1] + right[0] + right[2:] for left, right in splits if len(right) > 1}
    replaces = {left + c + right[1:] for left, right in splits if right for c in _ALPHABET}
    inserts = {left + c + right for left, right in splits for c in _ALPHABET}
    return deletes | swaps | replaces | inserts
