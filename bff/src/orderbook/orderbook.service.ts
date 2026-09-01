import { Inject, Injectable, OnModuleInit } from '@nestjs/common';
import { ORDER_EVENTS_SERVICE_NAME, type GetOrderEventsResponse, type OrderEventsServiceClient } from '../pb/orderbook/v1/order.js';
import type { ClientGrpc } from '@nestjs/microservices';
import { Observable, Subject, share } from 'rxjs';


@Injectable()
export class OrderbookService implements OnModuleInit {
    private orderService: OrderEventsServiceClient;
    private orderBook$: Observable<GetOrderEventsResponse>;
    private readonly subscriptions = new Map<string, Subject<void>>();

    constructor(@Inject(ORDER_EVENTS_SERVICE_NAME) private client: ClientGrpc) { }

    onModuleInit() {
        this.orderService = this.client.getService<OrderEventsServiceClient>(ORDER_EVENTS_SERVICE_NAME);
        this.orderBook$ = this.orderService.getOrderEvents({}).pipe(share());
    }

    public subscribe(clientId: string) {
        const unsubscribe$ = new Subject<void>();
        this.subscriptions.set(clientId, unsubscribe$);
        return { orderBook$: this.orderBook$, unsubscribe$ };
    }

    public unsubscribe(clientId: string) {
        const unsubscribe$ = this.subscriptions.get(clientId);
        unsubscribe$?.next();
        unsubscribe$?.complete();
        this.subscriptions.delete(clientId);
    }
} 
