from typing import Literal

from pydantic import BaseModel, ConfigDict, Field
from pydantic.alias_generators import to_camel


class CamelModel(BaseModel):
    """Python attributes in snake_case, JSON in camelCase to match the API's contract."""

    model_config = ConfigDict(alias_generator=to_camel, populate_by_name=True)


class History(CamelModel):
    previous_claims: int = Field(0, ge=0)
    previous_rejected: int = Field(0, ge=0)
    account_age_days: int = Field(0, ge=0)
    claims_last_30_days: int = Field(0, ge=0)


class AnalyzeRequest(CamelModel):
    claim_id: int
    description: str = Field("", max_length=1000)
    amount_requested: float = Field(..., ge=0)
    history: History = Field(default_factory=History)


class AnalyzeResponse(CamelModel):
    claim_id: int
    category: str
    category_confidence: float
    risk_score: float
    confidence: float
    recommended_outcome: Literal["Approve", "Review"]  # the AI never rejects on its own
    reasons: list[str]
    model_version: str
