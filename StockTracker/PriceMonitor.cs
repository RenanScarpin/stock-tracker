
public class PriceMonitor
{
    private readonly QuoteClient _quoteClient;
    private readonly PriceAlert _priceAlert;
    private readonly EmailSender _emailSender;
    private readonly string _ativo;
    private readonly TimeSpan _interval;

    public PriceMonitor(
        QuoteClient quoteClient,
        PriceAlert priceAlert,
        EmailSender emailSender,
        string ativo,
        TimeSpan interval)
    {
        _quoteClient = quoteClient;
        _priceAlert = priceAlert;
        _emailSender = emailSender;
        _ativo = ativo;
        _interval = interval;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                decimal price = await _quoteClient.GetPriceAsync(_ativo);

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {_ativo}: R$ {price:F2}");

                if (cancellationToken.IsCancellationRequested)
                    break;

                AlertType alert = _priceAlert.CheckPrice(price);

                if (alert != AlertType.None)
                {
                    string action = alert == AlertType.Sell ? "venda" : "compra";

                    string subject = $"Alerta de {action}: {_ativo}";
                    string body = $"O ativo {_ativo} atingiu R$ {price:F2}.\n" +
                                  $"Considere realizar uma operação de {action}.";

                    await _emailSender.SendAsync(subject, body);

                    _priceAlert.MarkAsSent();

                    Console.WriteLine($"Alerta de {action} enviado!");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro no monitoramento: {ex.Message}");
            }

            await Task.Delay(_interval, cancellationToken);
        }
    }
}
