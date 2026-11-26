"""Detects descriptions with no readable content, such as keyboard mashing or random letters.

A description counts as readable when at least one of its words is recognizable: a word the claim
classifier learned, a common English, French or German word (the languages the web app offers), or
one typo away from either. Real claims in any of those languages pass; "zeazeazeaz", "asdfgh" and
"???" do not. The rule is deliberately narrow: vague but real text ("refund please") is readable
and goes through the normal path.
"""

import re
from collections.abc import Iterable, Iterator

UNREADABLE = "unreadable"

# Real claims have a recognizable word early on; checking more only costs time on long nonsense.
_MAX_WORDS_CHECKED = 50
_MIN_TYPO_LENGTH = 4
_MAX_TYPO_LENGTH = 20

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


def build_vocabulary(known_words: Iterable[str]) -> frozenset[str]:
    """The words that count as recognizable. Build it once and reuse it for every request."""
    return frozenset(known_words) | _COMMON_WORDS


def is_unreadable(text: str, vocabulary: frozenset[str]) -> bool:
    """True when no word in `text` is recognizable. Empty text and text without letters are unreadable."""
    words = _WORD.findall(text.lower())[:_MAX_WORDS_CHECKED]
    return not any(_is_recognizable(word, vocabulary) for word in words)


def _is_recognizable(word: str, vocabulary: frozenset[str]) -> bool:
    if word in vocabulary:
        return True
    # One typo is allowed only in words of plausible length: short random strings would match too
    # easily, and very long ones would make the variant search expensive for no real benefit.
    if not _MIN_TYPO_LENGTH <= len(word) <= _MAX_TYPO_LENGTH:
        return False
    return any(variant in vocabulary for variant in _one_edit_away(word))


def _one_edit_away(word: str) -> Iterator[str]:
    """Every string one deletion, swap, replacement or insertion away from `word`, generated lazily."""
    for i in range(len(word) + 1):
        left, right = word[:i], word[i:]
        if right:
            yield left + right[1:]
            for c in _ALPHABET:
                yield left + c + right[1:]
        if len(right) > 1:
            yield left + right[1] + right[0] + right[2:]
        for c in _ALPHABET:
            yield left + c + right
