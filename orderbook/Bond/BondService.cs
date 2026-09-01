using Microsoft.Extensions.Options;

namespace OrderBook.Bond;

/// <summary>
/// Service implementation for managing and accessing generated bonds.
/// </summary>
public class BondService : IBondService
{
    private readonly Dictionary<string, Bond> _bondMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="BondService"/> class.
    /// </summary>
    /// <param name="options">The application options containing bond configuration.</param>
    public BondService(IOptions<AppOptions> options)
    {
        _bondMap = new(options.Value.BondMapSize);
        for (int i = 0; i < options.Value.BondMapSize; i++)
        {
            var bond = new Bond();
            _bondMap.Add(bond.Id, bond);
        }
    }

    /// <inheritdoc />
    public Bond? GetById(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return _bondMap.TryGetValue(id, out var bond) ? bond : null;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<string> GetIds() => _bondMap.Keys;
}
