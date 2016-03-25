// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway
{
    using System.Collections.Generic;
    using Common.Logging;
    using Common.Model;
    using global::PublicGateway.Hubs;
    using Microsoft.AspNet.SignalR;
    using StockTrendPredictionActor.Interfaces;

    internal class StockEventsHandler : IStockEvents
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(StockEventsHandler));

        public void OnLowStockProducts(List<ProductStockPrediction> productStocks)
        {
            Logger.Debug(nameof(this.OnLowStockProducts));

            GlobalHost.ConnectionManager.GetHubContext<StockHub>()
                .Clients.All
                .notifyLowStockProducts(productStocks);
        }
    }
}