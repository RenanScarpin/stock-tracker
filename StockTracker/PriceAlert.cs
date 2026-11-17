public enum AlertType
{
    None,
    Buy,
    Sell
}

public class PriceAlert
{
    private readonly decimal _sellPrice;
    private readonly decimal _buyPrice;

    private AlertType _currentZone = AlertType.None;
    private bool _alertSent = false;

    public PriceAlert(decimal sellPrice, decimal buyPrice)
    {
        _sellPrice = sellPrice;
        _buyPrice = buyPrice;
    }

    public AlertType CheckPrice(decimal price)
    {
        AlertType zone = AlertType.None;

        if (price > _sellPrice)
            zone = AlertType.Sell;
        else if (price < _buyPrice)
            zone = AlertType.Buy;

        if (zone != _currentZone)
        {
            _currentZone = zone;
            _alertSent = false;
        }

        return _alertSent ? AlertType.None : _currentZone;
    }

    public void MarkAsSent()
    {
        _alertSent = true;
    }
}
