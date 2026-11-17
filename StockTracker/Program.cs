using System.Globalization;
using System.Text.Json;

if (File.Exists(".env"))
    DotNetEnv.Env.Load();

// Teste da lógica de alertas
if (args.Length == 1 && args[0] == "--test-alerts")
{
    PriceAlert alert = new PriceAlert(22.67m, 22.59m);

    decimal[] prices =
    {
        22.63m, 22.68m, 22.70m,
        22.67m, 22.59m, 22.58m,
        22.57m, 22.63m, 22.68m
    };

    foreach (decimal price in prices)
    {
        AlertType action = alert.CheckPrice(price);

        Console.WriteLine($"Preço: {price:F2} | Alerta: {action}");

        if (action != AlertType.None)
            alert.MarkAsSent();
    }

    return 0;
}

// Teste de envio de e-mail
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

bool simulate = args.Length > 0 && args[0] == "--simulate";

if (simulate)
    args = args[1..];

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


if (simulate)
{
    decimal middle = precoCompra + (precoVenda - precoCompra) / 2m;

    decimal[] prices =
    {
        middle,
        precoVenda * 1.01m,
        precoVenda * 1.02m,
        middle,
        precoCompra * 0.99m,
        precoCompra * 0.98m,
        middle,
        precoVenda * 1.01m
    };

    int index = 0;

    PriceMonitor monitor = new PriceMonitor(
        ativo,
        new PriceAlert(precoVenda, precoCompra),
        TimeSpan.FromSeconds(1),

        () => Task.FromResult(prices[index++]),

        (subject, body) =>
        {
            Console.WriteLine($"\n--- E-MAIL SIMULADO ---\nAssunto: {subject}\n{body}\n----------------------\n");
            return Task.CompletedTask;
        }
    );

    Logger.Info("Simulação iniciada (sem API e sem SMTP).");

    await monitor.RunAsync(CancellationToken.None, prices.Length);

    Logger.Info("Simulação concluída.");

    return 0;
}

try
{
    // Carrega as configurações SMTP
    string path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    string json = await File.ReadAllTextAsync(path);

    EmailSettings settings = JsonSerializer.Deserialize<EmailSettings>(json)
        ?? throw new InvalidOperationException("Configuração SMTP inválida.");

    if (string.IsNullOrWhiteSpace(settings.To) ||
        string.IsNullOrWhiteSpace(settings.From) ||
        string.IsNullOrWhiteSpace(settings.Host) ||
        string.IsNullOrWhiteSpace(settings.Username) ||
        settings.Port <= 0)
    {
        throw new InvalidOperationException("Configuração SMTP inválida.");
    }

    string? password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");

    if (string.IsNullOrWhiteSpace(password))
        throw new InvalidOperationException("SMTP_PASSWORD não definida.");

    // Obtém o intervalo de consulta
    string intervalText = Environment.GetEnvironmentVariable("POLL_INTERVAL_SECONDS") ?? "300";

    if (!int.TryParse(intervalText, out int seconds) || seconds <= 0)
        throw new InvalidOperationException("Intervalo de consulta inválido.");

    // Cria os componentes
    using HttpClient httpClient = new HttpClient();
    httpClient.Timeout = TimeSpan.FromSeconds(10);

    QuoteClient quoteClient = new QuoteClient(httpClient);
    EmailSender emailSender = new EmailSender(settings, password);
    PriceAlert priceAlert = new PriceAlert(precoVenda, precoCompra);

    PriceMonitor monitor = new PriceMonitor(
        quoteClient,
        priceAlert,
        emailSender,
        ativo,
        TimeSpan.FromSeconds(seconds)
    );

    // Configura o encerramento com Ctrl+C
    using CancellationTokenSource cancellation = new CancellationTokenSource();

    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cancellation.Cancel();
    };

    Console.WriteLine("\nMonitoramento iniciado. Pressione Ctrl+C para encerrar.\n");

    try
    {
        await monitor.RunAsync(cancellation.Token);
    }
    catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
    {
        // Encerramento solicitado pelo usuário
    }

    Console.WriteLine("Monitoramento encerrado.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Erro ao iniciar monitoramento: {ex.Message}");
    return 1;
}
