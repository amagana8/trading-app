using Grpc.Core;
using Microsoft.Extensions.Options;

namespace OrderBook.Order;

public class OrderService : OrderEventsService.OrderEventsServiceBase
{
    private readonly OrderSimulator _orderSimulator;
    private readonly int _orderEventsPerSecond;


    public OrderService(
        IOptions<AppOptions> options,
        OrderSimulator orderSimulator
    )
    {
        _orderEventsPerSecond = options.Value.OrderEventsPerSecond;
        _orderSimulator = orderSimulator;
    }

    public override async Task GetOrderEvents(
        GetOrderEventsRequest request,
        IServerStreamWriter<GetOrderEventsResponse> responseStream,
        ServerCallContext context
    )
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1.0 / _orderEventsPerSecond));
        do
        {
            var orderEvent = _orderSimulator.SimulateOrderEvent();
            if (orderEvent is not null)
            {
                await responseStream.WriteAsync(orderEvent, context.CancellationToken);
            }
        } while (await timer.WaitForNextTickAsync(context.CancellationToken));
    }




}
