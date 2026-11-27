namespace ReturnFlow.Api.Services;

public partial class AiClient(HttpClient http, ILogger<AiClient> log)
{
    /// <summary>Returns null when the AI service is down, so the claim falls back to a human.</summary>
    public async Task<AiAnalysis?> AnalyzeAsync(AiRequest request, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsJsonAsync("analyze", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                LogErrorStatus(log, (int)response.StatusCode, request.ClaimId);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<AiAnalysis>(cancellationToken: ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            LogUnavailable(log, ex, request.ClaimId);
            return null;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI service returned {Status} for claim {ClaimId}")]
    private static partial void LogErrorStatus(ILogger logger, int status, int claimId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "AI service unavailable for claim {ClaimId}")]
    private static partial void LogUnavailable(ILogger logger, Exception exception, int claimId);
}
