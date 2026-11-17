using System.Text.Json;
using System.Net.Http.Headers;

public class QuoteClient
{
    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;

    public QuoteClient(HttpClient httpClient, string? apiKey = null)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<decimal> GetPriceAsync(string ativo)
    {
        string url = $"https://brapi.dev/api/v2/stocks/quote?symbols={ativo}";

        using HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrWhiteSpace(_apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using HttpResponseMessage response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument json = await JsonDocument.ParseAsync(stream);

        JsonElement root = json.RootElement;

        
        if (!root.TryGetProperty("results", out JsonElement results) ||
            results.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException("Resposta inválida da brapi.");
        }

        if (results.GetArrayLength() == 0)
        {
            throw new KeyNotFoundException($"Nenhuma cotação encontrada para {ativo}.");
        }

        JsonElement quote = results[0];

        if (!quote.TryGetProperty("data", out JsonElement data) ||
            data.ValueKind != JsonValueKind.Object ||
            !data.TryGetProperty("regularMarketPrice", out JsonElement price) ||
            price.ValueKind != JsonValueKind.Number)
        {
            throw new InvalidOperationException("Cotação inválida.");
        }

        return price.GetDecimal();
    }
}
