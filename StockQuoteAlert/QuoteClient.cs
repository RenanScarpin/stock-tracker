
using System.Text.Json;

public class QuoteClient
{
    private readonly HttpClient _httpClient;

    public QuoteClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<decimal> GetPriceAsync(string ativo)
    {
        string url = $"https://brapi.dev/api/v2/stocks/quote?symbols={ativo}";

        using HttpResponseMessage response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        using Stream stream = await response.Content.ReadAsStreamAsync();
        using JsonDocument json = await JsonDocument.ParseAsync(stream);

        JsonElement root = json.RootElement;

        if (!root.TryGetProperty("results", out JsonElement results) ||
            results.ValueKind != JsonValueKind.Array ||
            results.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Nenhuma cotação encontrada.");
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
