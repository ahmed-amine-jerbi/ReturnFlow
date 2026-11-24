namespace ReturnFlow.Api.Services;

public class AiClient(HttpClient http, ILogger<AiClient> log)
{
    /// <summary>Returns null when the AI service is down, so the claim falls back to a human.</summary>
    public async Task<AiAnalysis?> AnalyzeAsync(AiRequest request, CancellationToken ct)
    {
        try
        {
            using var response = await http.PostAsJsonAsync("analyze", request, ct);
            if (!response.IsSuccessStatusCode)
            {
                log.LogWarning("AI service returned {Status} for claim {ClaimId}", (int)response.StatusCode, request.ClaimId);
                return null;
            }
            return await response.Content.ReadFromJsonAsync<AiAnalysis>(cancellationToken: ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            log.LogWarning(ex, "AI service unavailable for claim {ClaimId}", request.ClaimId);
            return null;
        }
    }
}
