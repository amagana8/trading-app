using Microsoft.Extensions.Options;

namespace OrderBook.Account;

/// <summary>
/// Service implementation for managing and accessing generated accounts.
/// </summary>
public class AccountService : IAccountService
{
    private readonly Dictionary<Guid, Account> _accountMap;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountService"/> class.
    /// </summary>
    /// <param name="options">The application options containing AccountService configurations.</param>
    public AccountService(IOptions<AppOptions> options)
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
