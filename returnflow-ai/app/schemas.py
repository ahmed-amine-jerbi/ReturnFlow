from pydantic import BaseModel, Field


class History(BaseModel):
    previousClaims: int = Field(0, ge=0)
    previousRejected: int = Field(0, ge=0)
    accountAgeDays: int = Field(0, ge=0)
    claimsLast30Days: int = Field(0, ge=0)


class AnalyzeRequest(BaseModel):
    claimId: int
    description: str = Field("", max_length=1000)
    amountRequested: float = Field(..., ge=0)
    history: History = History()


class AnalyzeResponse(BaseModel):
    claimId: int
    category: str
    categoryConfidence: float
    riskScore: float
    confidence: float
    recommendedOutcome: str  # "Approve" or "Review" - the AI never rejects on its own
    reasons: list[str]
    modelVersion: str
