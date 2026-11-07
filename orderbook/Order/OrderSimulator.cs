using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Options;
using OrderBook.Account;
using OrderBook.Bond;
using OrderId = (System.Guid AccountId, string BondId);

namespace OrderBook.Order;

public class OrderSimulator : BackgroundService
{
    private readonly HashSet<OrderId> _availablePool;

    private readonly IAccountService _accountService;
    private readonly IBondService _bondService;
    private readonly Dictionary<OrderId, Order> _orderBook;
    private readonly int _minOrderBookSize;

    public OrderSimulator(
        IOptions<AppOptions> options,
        IAccountService accountService,
        IBondService bondService
    )
    {
        _minOrderBookSize = options.Value.MinOrderBookSize;
        _orderBook = new(_minOrderBookSize);
        _accountService = accountService;
        _bondService = bondService;
        _availablePool = new HashSet<OrderId>(
            _accountService.GetIds().Count() * _bondService.GetIds().Count()
        );

        foreach (var accountId in _accountService.GetIds())
            foreach (var bondId in _bondService.GetIds())
                _availablePool.Add((accountId, bondId));
    }

    public GetOrderEventsResponse? SimulateOrderEvent()
    {
        var action = GetOrderEventType();

        return action switch
        {
            OrderAction.New => CreateNewOrderEvent(),
            OrderAction.Cancel => CreateCancelOrderEvent(),
            OrderAction.Update => CreateUpdateOrderEvent(),
            _ => throw new ArgumentException("Invalid order action", nameof(action)),
        };
    }

    private OrderAction GetOrderEventType()
    {
        if (_orderBook.Count < _minOrderBookSize)
        {
            return OrderAction.New;
        }

        if (_availablePool.Count > 0)
        {
            return WeightedSample([
                (OrderAction.New, 4),
                (OrderAction.Cancel, 1),
                (OrderAction.Update, 5),
            ]);
        }
        else
        {
            return WeightedSample([(OrderAction.Cancel, 3), (OrderAction.Update, 7)]);
        }
    }

    private GetOrderEventsResponse? CreateNewOrderEvent()
    {
        if (_availablePool.Count == 0)
            return null;

        var orderId = _availablePool.ElementAt(Random.Shared.Next(_availablePool.Count));
        _availablePool.Remove(orderId);
        var (accountId, bondId) = orderId;

        if (_bondService.GetById(bondId) is null)
            return null;

        if (_accountService.GetById(accountId) is null)
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

        return new GetOrderEventsResponse { Order = order, UpdateType = OrderAction.New };
    }

    private GetOrderEventsResponse? CreateCancelOrderEvent()
    {
        if (_orderBook.Count == 0)
            return null;

        var order = _orderBook.ElementAt(Random.Shared.Next(_orderBook.Count));
        _orderBook.Remove(order.Key);
        _availablePool.Add(order.Key);

        return new GetOrderEventsResponse { Order = order.Value, UpdateType = OrderAction.Cancel };
    }

    private GetOrderEventsResponse? CreateUpdateOrderEvent()
    {
        if (_orderBook.Count == 0)
            return null;

        var order = _orderBook.ElementAt(Random.Shared.Next(_orderBook.Count));
        if (_bondService.GetById(order.Key.BondId) is null)
            return null;

        if (Random.Shared.Next(2) == 0)
        {
            var priceDelta = 0.1m + (decimal)Random.Shared.NextDouble() * (4m - 0.1m);
            order.Value.Price += order.Value.Side == Side.Buy ? -priceDelta : priceDelta;
        }
        else
        {
            order.Value.Quantity += Random.Shared.NextInt64(1_000_000, 10_000_000);
        }

        order.Value.UpdatedAt = Timestamp.FromDateTime(DateTime.UtcNow);
        return new GetOrderEventsResponse { Order = order.Value, UpdateType = OrderAction.Update };
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