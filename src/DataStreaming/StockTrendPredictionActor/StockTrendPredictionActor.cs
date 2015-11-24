// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Shared;
    using global::StockTrendPredictionActor.AzureML;
    using global::StockTrendPredictionActor.Interfaces;
    using Microsoft.ServiceFabric.Actors;

    public class StockTrendPredictionActor :
        StatefulActor<StockTrendPredictionActorState>, IStockTrendPredictionActor
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(StockTrendPredictionActor));

        private int notificationAttempts;

        private IActorTimer notificationTimer;
        private IAzureMlClient mlClient;

        public async Task ProcessPurchasesAsync(List<ProductPurchase> orders)
        {
            Logger.Debug("{0}: {1} orders", nameof(this.ProcessPurchasesAsync), orders?.Count);

            if (orders == null)
                return;

            try
            {
                Dictionary<int, int> productIds = new Dictionary<int, int>();

                DateTime now = DateTime.UtcNow;
                Dictionary<int, ProductStockTrend> productStockTrends = this.State.ProductStockTrends;
                foreach (ProductPurchase order in orders)
                {
                    // Add this product trend if we don't track it yet
                    if (!productStockTrends.ContainsKey(order.ProductId))
                        productStockTrends.Add(
                            order.ProductId,
                            new ProductStockTrend()
                            {
                                ProductId = order.ProductId,
                                ProductName = order.ProductName
                            });

                    ProductStockTrend trend = productStockTrends[order.ProductId];
                    trend.Reset(now.AddMonths(-1), DateTime.UtcNow);
                    trend.AddOrder(order, this.notificationAttempts);

                    productIds[order.ProductId] = 0;
                }

                await this.CalculatePredictionsAsync(productIds.Keys);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(this.ProcessPurchasesAsync));
                throw;
            }
        }

        protected override async Task OnActivateAsync()
        {
            try
            {
                if (this.State == null)
                {
                    this.State = new StockTrendPredictionActorState()
                    {
                        ProductStockTrends = new Dictionary<int, ProductStockTrend>()
                    };
                }

                TimeSpan startDelay = TimeSpan.Parse(ConfigurationHelper.ReadValue("AppSettings", "PredictionTimerStartDelay"));
                TimeSpan interval = TimeSpan.Parse(ConfigurationHelper.ReadValue("AppSettings", "PredictionTimerInterval"));

                if (!int.TryParse(ConfigurationHelper.ReadValue("AppSettings", "PredictionNotificationAttempts"), out this.notificationAttempts))
                    this.notificationAttempts = 10;

                // register timer to regularly check for updates
                this.notificationTimer = this.RegisterTimer(
                    this.RunNotificationAsync,
                    null,
                    startDelay,
                    interval
                    );

                this.mlClient = AzureMlClientFactory.CreateClient();

                await base.OnActivateAsync();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(this.OnActivateAsync));
                throw;
            }
        }


        private async Task CalculatePredictionsAsync(IEnumerable<int> productIds)
        {
            Logger.Debug(nameof(this.CalculatePredictionsAsync));

            Dictionary<int, ProductStockTrend> productTrends = this.State.ProductStockTrends;

            AzureMlBatchResponse response = await this.mlClient.InvokeBatchAsync(new AzureMlBatchRequest(productIds.Select(x => productTrends[x])));

            foreach (AzureMlBatchResponse.ResponseLine r in response.Lines)
            {
                ProductStockTrend trend = productTrends[r.ProductId];
                trend.Probability = r.Probability;
                trend.Reorder = r.Reorder;
            }
        }

        private async Task RunNotificationAsync(object arg)
        {
            try
            {
                // select only products to be notified of
                List<ProductStockTrend> list = this.State.ProductStockTrends
                    .Where(x => x.Value.NotificationCounter > 0)
                    .Select(x => x.Value)
                    .ToList();

                if (list.Count > 0)
                {
                    Logger.Debug("RunNotificationAsync: notifying on {0} products", list.Count);

                    INotificationActor notificationActor = ActorProxy.Create<INotificationActor>(new ActorId(0));
                    await notificationActor.NotifyLowStockProducts(
                        list.Select(x => x.ToProductPrediction()).ToList()
                        );

                    // decrement the counter of notification attempts
                    list.ForEach(x => x.NotificationCounter--);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(this.RunNotificationAsync));
            }
        }
    }
}