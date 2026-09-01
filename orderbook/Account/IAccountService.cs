namespace OrderBook.Account;

/// <summary>
/// Service interface for managing and accessing trading accounts.
/// </summary>
public interface IAccountService
{
    /// <summary>
    /// Retrieves an account by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the account.</param>
    /// <returns>The <see cref="Account"/> if found; otherwise, <c>null</c>.</returns>
    Account? GetById(Guid id);

    /// <summary>
    /// Retrieves all account identifiers.
    /// </summary>
    /// <returns>A read-only collection of account identifiers.</returns>
    IReadOnlyCollection<Guid> GetIds();
}
