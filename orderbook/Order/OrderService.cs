using Grpc.Core;

namespace OrderBook.Order;

public class OrderService : OrderEventsService.OrderEventsServiceBase
{
    private readonly OrderSimulator _orderSimulator;

    public OrderService(
        OrderSimulator orderSimulator
    )
    {
        _orderSimulator = orderSimulator;
    }

    public override async Task GetOrderEvents(
        GetOrderEventsRequest request,
        IServerStreamWriter<GetOrderEventsResponse> responseStream,
        ServerCallContext context
    )
    {
        await foreach (var orderEvent in _orderSimulator.GetOrderEventsAsync(context.CancellationToken))
        {
            await responseStream.WriteAsync(orderEvent, context.CancellationToken);
        }
    }
}
