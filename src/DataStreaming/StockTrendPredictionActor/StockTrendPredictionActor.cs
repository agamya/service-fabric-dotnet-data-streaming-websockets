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
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Actors.Runtime;

    [StatePersistence(StatePersistence.Persisted)]
    public class StockTrendPredictionActor : Actor, IStockTrendPredictionActor
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(StockTrendPredictionActor));
        private int notificationAttempts;
        private IActorTimer notificationTimer;
        private IAzureMlClient mlClient;

        public async Task ProcessPurchasesAsync(List<ProductPurchase> orders)
        {
            Logger.Debug("{0}: {1} orders", nameof(this.ProcessPurchasesAsync), orders?.Count);

            if (orders == null)
            {
                return;
            }

            //Dictionary<int, ProductStockTrend> ProductStockTrends

            try
            {
                Dictionary<int, int> productIds = new Dictionary<int, int>();

                DateTime now = DateTime.UtcNow;

                Dictionary<int, ProductStockTrend> productStockTrends =
                    await this.StateManager.GetStateAsync<Dictionary<int, ProductStockTrend>>("ProductStockTrends");

                foreach (ProductPurchase order in orders)
                {
                    // Add this product trend if we don't track it yet
                    if (!productStockTrends.ContainsKey(order.ProductId))
                    {
                        productStockTrends.Add(
                            order.ProductId,
                            new ProductStockTrend()
                            {
                                ProductId = order.ProductId,
                                ProductName = order.ProductName
                            });
                    }

                    ProductStockTrend trend = productStockTrends[order.ProductId];
                    trend.Reset(now.AddMonths(-1), DateTime.UtcNow);
                    trend.AddOrder(order, this.notificationAttempts);

                    productIds[order.ProductId] = 0;
                }

                await this.StateManager.SetStateAsync<Dictionary<int, ProductStockTrend>>("ProductStockTrends", productStockTrends);
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
                await this.StateManager.TryAddStateAsync<Dictionary<int, ProductStockTrend>>("ProductStockTrends", new Dictionary<int, ProductStockTrend>());

                TimeSpan startDelay = TimeSpan.Parse(ConfigurationHelper.ReadValue("AppSettings", "PredictionTimerStartDelay"));
                TimeSpan interval = TimeSpan.Parse(ConfigurationHelper.ReadValue("AppSettings", "PredictionTimerInterval"));

                if (!int.TryParse(ConfigurationHelper.ReadValue("AppSettings", "PredictionNotificationAttempts"), out this.notificationAttempts))
                {
                    this.notificationAttempts = 10;
                }

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

            Dictionary<int, ProductStockTrend> productTrends =
                await this.StateManager.GetStateAsync<Dictionary<int, ProductStockTrend>>("ProductStockTrends");

            AzureMlBatchResponse response = await this.mlClient.InvokeBatchAsync(new AzureMlBatchRequest(productIds.Select(x => productTrends[x])));

            foreach (AzureMlBatchResponse.ResponseLine r in response.Lines)
            {
                ProductStockTrend trend = productTrends[r.ProductId];
                trend.Probability = r.Probability;
                trend.Reorder = r.Reorder;
            }

            await this.StateManager.SetStateAsync<Dictionary<int, ProductStockTrend>>("ProductStockTrends", productTrends);
        }

        private async Task RunNotificationAsync(object arg)
        {
            try
            {
                Dictionary<int, ProductStockTrend> productTrends =
                    await this.StateManager.GetStateAsync<Dictionary<int, ProductStockTrend>>("ProductStockTrends");

                // select only products to be notified of
                IEnumerable<KeyValuePair<int, ProductStockTrend>> list = productTrends
                    .Where(x => x.Value.NotificationCounter > 0);

                int count = list.Count();
                if (count > 0)
                {
                    Logger.Debug("RunNotificationAsync: notifying on {0} products", count);

                    INotificationActor notificationActor = ActorProxy.Create<INotificationActor>(new ActorId(0));

                    await notificationActor.NotifyLowStockProducts(
                        list.Select(x => x.Value.ToProductPrediction()).ToList()
                        );

                    // decrement the counter of notification attempts
                    foreach (int key in list.Select(x => x.Key))
                    {
                        productTrends[key].NotificationCounter--;
                    }

                    await this.StateManager.SetStateAsync<Dictionary<int, ProductStockTrend>>("ProductStockTrends", productTrends);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(this.RunNotificationAsync));
            }
        }
    }
}