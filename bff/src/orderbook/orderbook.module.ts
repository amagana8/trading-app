import { Module } from '@nestjs/common';
import { OrderbookService } from './orderbook.service.js';
import { ClientsModule, Transport } from '@nestjs/microservices';
import { join } from 'path';
import { ORDER_EVENTS_SERVICE_NAME, ORDERBOOK_V1_PACKAGE_NAME } from '../pb/orderbook/v1/order.js';

@Module({
    imports: [
        ClientsModule.register([
            {
                name: ORDER_EVENTS_SERVICE_NAME,
                transport: Transport.GRPC,
                options: {
                    package: ORDERBOOK_V1_PACKAGE_NAME,
                    protoPath: join(import.meta.dirname, '../../../protos/orderbook/v1/order.proto'),
                    loader: {
                        includeDirs: [join(import.meta.dirname, '../../../protos')],
                    },
                    url: 'localhost:5023'
                }
            }
        ]),
    ],
    providers: [OrderbookService],
    exports: [OrderbookService],
})
export class OrderbookModule { }
