// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Model;
    using global::StockTrendPredictionActor.Interfaces;
    using Microsoft.ServiceFabric.Actors.Runtime;

    [StatePersistence(StatePersistence.None)]
    public class NotificationActor : Actor, INotificationActor
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(NotificationActor));

        public Task NotifyLowStockProducts(List<ProductStockPrediction> productStocks)
        {
            Logger.Debug(nameof(this.NotifyLowStockProducts));

            IStockEvents events = this.GetEvent<IStockEvents>();
            events.OnLowStockProducts(productStocks);

            return Task.FromResult(0);
        }

        protected override Task OnActivateAsync()
        {
            Logger.Debug(nameof(this.OnActivateAsync));

            return base.OnActivateAsync();
        }
    }
}