using System.Globalization;
using System.Text.Json;
DotNetEnv.Env.Load();


if (args.Length == 1 && args[0] == "--test-email")
{
    try
    {
        string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        string json = await File.ReadAllTextAsync(path);

        EmailSettings? settings = JsonSerializer.Deserialize<EmailSettings>(json);

        if (settings is null ||
            string.IsNullOrWhiteSpace(settings.To) ||
            string.IsNullOrWhiteSpace(settings.From) ||
            string.IsNullOrWhiteSpace(settings.Host) ||
            string.IsNullOrWhiteSpace(settings.Username) ||
            settings.Port <= 0)
        {
            throw new InvalidOperationException("Configuração SMTP inválida.");
        }

        string? password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Variável SMTP_PASSWORD não definida.");
        }

        EmailSender sender = new EmailSender(settings, password);

        await sender.SendAsync(
            "Teste StockQuoteAlert",
            "Este é um e-mail de teste do StockQuoteAlert."
        );

        Console.WriteLine("Mensagem aceita pelo servidor SMTP.");
        return 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Erro ao enviar e-mail: {ex.Message}");
        return 1;
    }
}

// Verifica a quantidade de argumentos
if (args.Length != 3)
{
    Console.Error.WriteLine("Uso: StockQuoteAlert <ativo> <preco_venda> <preco_compra>");
    return 1;
}

// Obtém o ativo
string ativo = args[0].Trim().ToUpperInvariant();

if (string.IsNullOrWhiteSpace(ativo) || !ativo.All(char.IsLetterOrDigit))
{
    Console.Error.WriteLine("Erro: ativo inválido.");
    return 1;
}

// Obtém o preço de venda
if (!decimal.TryParse(args[1], NumberStyles.AllowDecimalPoint,
    CultureInfo.InvariantCulture, out decimal precoVenda) || precoVenda <= 0)
{
    Console.Error.WriteLine("Erro: preço de venda inválido.");
    return 1;
}

// Obtém o preço de compra
if (!decimal.TryParse(args[2], NumberStyles.AllowDecimalPoint,
    CultureInfo.InvariantCulture, out decimal precoCompra) || precoCompra <= 0)
{
    Console.Error.WriteLine("Erro: preço de compra inválido.");
    return 1;
}

// Verifica a relação entre os preços
if (precoCompra >= precoVenda)
{
    Console.Error.WriteLine("Erro: o preço de compra deve ser menor que o de venda.");
    return 1;
}

// Exibe os parâmetros recebidos
Console.WriteLine($"Ativo: {ativo}");
Console.WriteLine($"Preço de venda: R$ {precoVenda:F2}");
Console.WriteLine($"Preço de compra: R$ {precoCompra:F2}");


using HttpClient httpClient = new HttpClient();
httpClient.Timeout = TimeSpan.FromSeconds(10);

QuoteClient quoteClient = new QuoteClient(httpClient);

try
{
    Console.WriteLine("\nConsultando cotação...");

    decimal cotacao = await quoteClient.GetPriceAsync(ativo);

    Console.WriteLine($"Cotação atual: R$ {cotacao:F2}");
}
catch (HttpRequestException ex)
{
    Console.Error.WriteLine($"Erro na requisição: {ex.Message}");
    return 1;
}
catch (TaskCanceledException)
{
    Console.Error.WriteLine("Erro: tempo limite da consulta excedido.");
    return 1;
}
catch (JsonException)
{
    Console.Error.WriteLine("Erro: resposta JSON inválida.");
    return 1;
}
catch (InvalidOperationException ex)
{
    Console.Error.WriteLine($"Erro: {ex.Message}");
    return 1;
}

return 0;

