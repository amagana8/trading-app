using Microsoft.Extensions.Options;

namespace OrderBook.Account;

/// <summary>
/// In-memory repository implementation for managing and accessing generated accounts.
/// </summary>
public class AccountRepository : IAccountRepository
{
    private readonly Dictionary<Guid, Account> _accountMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRepository"/> class.
    /// </summary>
    /// <param name="options">The application options containing account repository configurations.</param>
    public AccountRepository(IOptions<AppOptions> options)
    {
        _accountMap = new(options.Value.AccountMapSize);
        for (int i = 0; i < options.Value.AccountMapSize; i++)
        {
            var account = new Account(Guid.NewGuid(), $"Account {i + 1}");
            _accountMap.Add(account.Id, account);
        }
    }

    /// <inheritdoc />
    public Account? GetById(Guid id) =>
        _accountMap.TryGetValue(id, out var account) ? account : null;

    /// <inheritdoc />
    public IReadOnlyCollection<Guid> GetIds() => _accountMap.Keys;
}
