namespace OrderBook.Api;

public partial class DecimalValue
{
    private const decimal NanoFactor = 1_000_000_000m;

    public static implicit operator decimal(DecimalValue decimalValue) =>
        decimalValue.Units + (decimalValue.Nanos / NanoFactor);

    public static implicit operator DecimalValue(decimal value)
    {
        var units = decimal.Truncate(value);
        var nanos = decimal.ToInt32((value - units) * NanoFactor);
        return new DecimalValue { Units = (long)units, Nanos = nanos };
    }
}
