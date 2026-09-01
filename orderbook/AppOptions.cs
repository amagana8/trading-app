using System.ComponentModel.DataAnnotations;

namespace OrderBook;

public class AppOptions : IValidatableObject
{
    public const string SectionName = "AppConfig";

    [Range(1, int.MaxValue)]
    public int MinOrderBookSize { get; set; }

    [Range(1, 10_000)]
    public int BondMapSize { get; set; }

    [Range(1, 5_000)]
    public int AccountMapSize { get; set; }

    [Range(1, int.MaxValue)]
    public int OrderEventsPerSecond { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinOrderBookSize > AccountMapSize * BondMapSize)
        {
            yield return new ValidationResult(
                $"{nameof(MinOrderBookSize)} must be at most {nameof(AccountMapSize)} * {nameof(BondMapSize)}",
                [nameof(MinOrderBookSize)]
            );
        }
    }
}
