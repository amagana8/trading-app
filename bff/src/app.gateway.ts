import { Logger } from '@nestjs/common';
import {
    WebSocketGateway,
    OnGatewayDisconnect,
    OnGatewayConnection,
    ConnectedSocket,
    SubscribeMessage,
} from '@nestjs/websockets';
import { finalize, map, takeUntil } from 'rxjs';
import { OrderbookService } from './orderbook/orderbook.service.js';
import { Socket } from 'socket.io';


@WebSocketGateway({
    cors: {
        origin: '*',
    },
})
export class AppGateway implements OnGatewayDisconnect, OnGatewayConnection {
    private readonly logger = new Logger(AppGateway.name);

    constructor(private readonly orderbookService: OrderbookService) { }

    handleDisconnect(socket: Socket): void {
        this.logger.log(`${socket.id} has disconnected`);
    }

    handleConnection(socket: Socket): void {
        this.logger.log(`${socket.id} has connected`);
    }

    @SubscribeMessage('orderBook.subscribe')
    handleOrderBookSubscribe(@ConnectedSocket() socket: Socket) {
        this.logger.log(`client ${socket.id} has subscribed to orderbook`);

        const { orderBook$, unsubscribe$ } = this.orderbookService.subscribe(
            socket.id,
        );

        return orderBook$.pipe(
            takeUntil(unsubscribe$),
            map((data) => ({ event: 'orderBook.update', data })),
            finalize(() => {
                this.logger.log(`client ${socket.id} has unsubscribed from orderbook`);
            }),
        );
    }

    @SubscribeMessage('orderBook.unsubscribe')
    handleOrderBookUnsubscribe(@ConnectedSocket() socket: Socket) {
        this.orderbookService.unsubscribe(socket.id);
    }
}