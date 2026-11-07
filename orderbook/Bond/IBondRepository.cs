namespace OrderBook.Bond;

/// <summary>
/// Repository interface for managing and accessing bonds.
/// </summary>
public interface IBondRepository
{
    /// <summary>
    /// Retrieves a bond by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the bond.</param>
    /// <returns>The <see cref="Bond"/> if found; otherwise, <c>null</c>.</returns>
    Bond? GetById(string id);

    /// <summary>
    /// Retrieves all bond identifiers.
    /// </summary>
    /// <returns>A read-only collection of bond identifiers.</returns>
    IReadOnlyCollection<string> GetIds();
}
