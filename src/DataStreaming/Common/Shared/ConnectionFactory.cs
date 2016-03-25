// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Shared
{
    using System;
    using Microsoft.ServiceFabric.Actors;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Remoting.Client;
    using StockAggregatorService.Interfaces;
    using StockService.Interfaces;
    using StockTrendPredictionActor.Interfaces;

    public static class ConnectionFactory
    {
        public static readonly string WebSocketServerName = @"ws://localhost:3251/PublicGatewayWS/";
        public static readonly string PublicGatewayApi = "http://localhost:3251/api/";
        public static readonly string ReserveStockApiController = PublicGatewayApi + "reservestock";
        public static readonly string StockApiController = PublicGatewayApi + "stockaggregator";
        public static readonly string StockAggregatorApiController = PublicGatewayApi + "stock";
        public static readonly Uri StockAggregatorServiceUri = new Uri("fabric:/PredictiveBackend/StockAggregatorService");
        public static readonly Uri StockServiceUri = new Uri("fabric:/PredictiveBackend/StockService");
        private static readonly string ApplicationName = "fabric:/PredictiveBackend";

        public static IStockService CreateStockService(int productId)
        {
            return ServiceProxy.Create<IStockService>(StockServiceUri, new ServicePartitionKey(productId));
        }

        public static IStockAggregatorService CreateStockAggregatorService(int productId)
        {
            return ServiceProxy.Create<IStockAggregatorService>(StockAggregatorServiceUri, new ServicePartitionKey(productId));
        }

        public static IStockTrendPredictionActor CreateStockTrendPredictionActor(int productId)
        {
            return ActorProxy.Create<IStockTrendPredictionActor>(new ActorId(productId), ApplicationName);
        }

        public static INotificationActor CreateNotificationActor()
        {
            return ActorProxy.Create<INotificationActor>(new ActorId(0), ApplicationName);
        }

        public static IStockTrendPredictionActor CreateBatchedStockTrendPredictionActor(string actorId)
        {
            return ActorProxy.Create<IStockTrendPredictionActor>(new ActorId(actorId), ApplicationName);
        }
    }
}