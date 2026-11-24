from contextlib import asynccontextmanager

from fastapi import FastAPI

from .model import ClaimAnalyzer
from .schemas import AnalyzeRequest, AnalyzeResponse


@asynccontextmanager
async def lifespan(app: FastAPI):
    app.state.analyzer = ClaimAnalyzer()
    yield


app = FastAPI(title="ReturnFlow AI", version="1.0.0", lifespan=lifespan)


@app.get("/health")
def health():
    return {"status": "ok"}


@app.get("/model-info")
def model_info():
    return app.state.analyzer.metrics


@app.post("/analyze", response_model=AnalyzeResponse)
def analyze(req: AnalyzeRequest) -> AnalyzeResponse:
    return app.state.analyzer.analyze(req)
