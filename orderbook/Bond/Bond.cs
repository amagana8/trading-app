namespace OrderBook.Bond;

public partial class Bond
{
    private const string ValidTickerSymbols = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

    private readonly DateOnly _maturity = DateOnly
        .FromDateTime(DateTime.Today)
        .AddYears(Random.Shared.Next(1, 21));
    private readonly decimal _coupon = 1 + (decimal)Random.Shared.NextDouble() * 7;

    partial void OnConstruction()
    {
        Id = Guid.NewGuid().ToString();
        Name = $"{GenerateTicker(5)} ${_coupon:0.###} {_maturity:MM/dd/yyyy}";
    }

    private static string GenerateTicker(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);
        return new string(Random.Shared.GetItems(ValidTickerSymbols.AsSpan(), length));
    }
}
