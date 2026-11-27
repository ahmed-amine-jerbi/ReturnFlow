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
    history: History = Field(default_factory=lambda: History.model_validate({}))


class AnalyzeResponse(CamelModel):
    """What the claim is about and how risky it looks. The API's policy turns this into a decision."""

    claim_id: int
    category: str
    confidence: float = Field(description="Probability that the category is right, 0 to 1")
    risk_score: float = Field(description="Probability that the claim is suspicious, 0 to 1")
    reasons: list[str] = Field(description="Plain-language factors that raised the risk score")
    model_version: str
