using System.Globalization;

public class PriceMonitor
{
    private readonly Func<Task<decimal>> _getPriceAsync;
    private readonly Func<string, string, Task> _sendEmailAsync;
    private readonly PriceAlert _priceAlert;
    private readonly string _ativo;
    private readonly TimeSpan _interval;

    public PriceMonitor(
        string ativo,
        PriceAlert priceAlert,
        TimeSpan interval,
        Func<Task<decimal>> getPriceAsync,
        Func<string, string, Task> sendEmailAsync)
    {
        _ativo = ativo;
        _priceAlert = priceAlert;
        _interval = interval;
        _getPriceAsync = getPriceAsync;
        _sendEmailAsync = sendEmailAsync;
    }

    public async Task RunAsync(CancellationToken cancellationToken, int? maxConsultas = null)
    {
        int consultas = 0;

        while (!cancellationToken.IsCancellationRequested &&
               (!maxConsultas.HasValue || consultas < maxConsultas.Value))
        {
            consultas++;

            try
            {
                decimal price = await _getPriceAsync();

                Logger.Info($"{_ativo}: R$ {price:F2}");

                if (cancellationToken.IsCancellationRequested)
                    break;

                AlertType alert = _priceAlert.CheckPrice(price);

                if (alert != AlertType.None)
                {
                    bool sell = alert == AlertType.Sell;

                    string action = sell ? "venda" : "compra";
                    decimal limit = sell ? _priceAlert.SellPrice : _priceAlert.BuyPrice;
                    decimal variation = (price - limit) / limit * 100m;

                    CultureInfo culture = CultureInfo.GetCultureInfo("pt-BR");

                    string subject = $"StockTracker | Alerta de {action}: {_ativo}";

                    string body =
                        $"Ativo: {_ativo}\n" +
                        $"Cotação consultada: {price.ToString("C2", culture)}\n" +
                        $"Limite de {action}: {limit.ToString("C2", culture)}\n" +
                        $"Diferença em relação ao limite: {variation.ToString("+0.00;-0.00;0.00", culture)}%\n" +
                        $"Horário da consulta: {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n\n" +
                        $"Considere avaliar uma operação de {action}.";

                    await _sendEmailAsync(subject, body);

                    _priceAlert.MarkAsSent();

                    Logger.Alert($"Alerta de {action} de {_ativo} processado.");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Falha ao monitorar {_ativo}: {ex.Message}");
            }

            if (maxConsultas.HasValue && consultas >= maxConsultas.Value)
                break;

            await Task.Delay(_interval, cancellationToken);
        }
    }
}
