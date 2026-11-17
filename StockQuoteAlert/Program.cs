using System.Globalization;

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

return 0;
