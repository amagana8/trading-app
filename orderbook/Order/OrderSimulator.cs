using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using OrderBook.Account;
using OrderBook.Bond;
using OrderId = (System.Guid AccountId, string BondId);

namespace OrderBook.Order;

public class OrderSimulator : BackgroundService
{
    private readonly HashSet<OrderId> _availablePool;

    private readonly IAccountRepository _accountRepository;
    private readonly IBondRepository _bondRepository;
    private readonly Dictionary<OrderId, Order> _orderBook;
    private readonly int _minOrderBookSize;
    private readonly int _orderEventsPerSecond;
    private readonly ConcurrentDictionary<Guid, Channel<GetOrderEventsResponse>> _subscribers =
        new();

    public OrderSimulator(
        IOptions<AppOptions> options,
        IAccountRepository accountRepository,
        IBondRepository bondRepository
    )
    {
        _minOrderBookSize = options.Value.MinOrderBookSize;
        _orderEventsPerSecond = options.Value.OrderEventsPerSecond;
        _orderBook = new(_minOrderBookSize);
        _accountRepository = accountRepository;
        _bondRepository = bondRepository;
        _availablePool = new HashSet<OrderId>(
            _accountRepository.GetIds().Count() * _bondRepository.GetIds().Count()
        );

        foreach (var accountId in _accountRepository.GetIds())
        foreach (var bondId in _bondRepository.GetIds())
            _availablePool.Add((accountId, bondId));
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1.0 / _orderEventsPerSecond));
        do
        {
            var orderEvent = SimulateOrderEvent();
            if (orderEvent is not null)
            {
                Broadcast(orderEvent);
            }
        } while (await timer.WaitForNextTickAsync(cancellationToken));
    }

    private void Broadcast(GetOrderEventsResponse orderEvent)
    {
        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Writer.TryWrite(orderEvent);
        }
    }

    public async IAsyncEnumerable<GetOrderEventsResponse> GetOrderEventsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var channel = Channel.CreateUnbounded<GetOrderEventsResponse>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = true }
        );

        var id = Guid.NewGuid();
        _subscribers.TryAdd(id, channel);

        try
        {
            await foreach (var orderEvent in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return orderEvent;
            }
        }
        finally
        {
            _subscribers.TryRemove(id, out _);
            channel.Writer.Complete();
        }
    }

    private GetOrderEventsResponse? SimulateOrderEvent()
    {
        var action = GetOrderEventType();

        return action switch
        {
            OrderAction.Add => CreateAddOrderEvent(),
            OrderAction.Remove => CreateRemoveOrderEvent(),
            OrderAction.Update => CreateUpdateOrderEvent(),
            _ => throw new ArgumentException("Invalid order action", nameof(action)),
        };
    }

    private OrderAction GetOrderEventType()
    {
        if (_orderBook.Count < _minOrderBookSize)
        {
            return OrderAction.Add;
        }

        if (_availablePool.Count > 0)
        {
            return WeightedSample([
                (OrderAction.Add, 4),
                (OrderAction.Remove, 1),
                (OrderAction.Update, 5),
            ]);
        }
        else
        {
            return WeightedSample([(OrderAction.Remove, 3), (OrderAction.Update, 7)]);
        }
    }

    private GetOrderEventsResponse? CreateAddOrderEvent()
    {
        if (_availablePool.Count == 0)
            return null;

        var orderId = _availablePool.ElementAt(Random.Shared.Next(_availablePool.Count));
        _availablePool.Remove(orderId);
        var (accountId, bondId) = orderId;

        if (_bondRepository.GetById(bondId) is null)
            return null;

        if (_accountRepository.GetById(accountId) is null)
            return null;

        var now = DateTime.UtcNow;

        var order = new Order
        {
            AccountId = accountId.ToString(),
            BondId = bondId,
            CreatedAt = Timestamp.FromDateTime(now),
            OrderId = Guid.NewGuid().ToString(),
            Price = 50m + (decimal)Random.Shared.NextDouble() * (150m - 50m),
            Quantity = Random.Shared.NextInt64(1_000_000, 10_000_000),
            Side = Random.Shared.Next(2) == 0 ? Side.Buy : Side.Sell,
            UpdatedAt = Timestamp.FromDateTime(now),
        };

        _orderBook[orderId] = order;

        return new GetOrderEventsResponse { Order = order, UpdateType = OrderAction.Add };
    }

    private GetOrderEventsResponse? CreateRemoveOrderEvent()
    {
        if (_orderBook.Count == 0)
            return null;

        var order = _orderBook.ElementAt(Random.Shared.Next(_orderBook.Count));
        _orderBook.Remove(order.Key);
        _availablePool.Add(order.Key);

        return new GetOrderEventsResponse { Order = order.Value, UpdateType = OrderAction.Remove };
    }

    private GetOrderEventsResponse? CreateUpdateOrderEvent()
    {
        if (_orderBook.Count == 0)
            return null;

        var order = _orderBook.ElementAt(Random.Shared.Next(_orderBook.Count));
        if (_bondRepository.GetById(order.Key.BondId) is null)
            return null;

        var updatedOrder = order.Value.Clone();

        if (Random.Shared.Next(2) == 0)
        {
            var priceDelta = 0.1m + (decimal)Random.Shared.NextDouble() * (4m - 0.1m);
            updatedOrder.Price += updatedOrder.Side == Side.Buy ? -priceDelta : priceDelta;
        }
        else
        {
            updatedOrder.Quantity += Random.Shared.NextInt64(1_000_000, 10_000_000);
        }

        updatedOrder.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        _orderBook[order.Key] = updatedOrder;
        return new GetOrderEventsResponse { Order = updatedOrder, UpdateType = OrderAction.Update };
    }

    private T WeightedSample<T>(IReadOnlyList<(T item, int weight)> items)
    {
        if (items.Count == 0)
            throw new ArgumentException("The items list cannot be empty.", nameof(items));
        var totalWeights = items.Sum(x => x.weight);
        var roll = Random.Shared.Next(totalWeights);

        foreach (var (item, weight) in items)
        {
            roll -= weight;
            if (roll < 0)
                return item;
        }

        return items.Last().item;
    }
}
