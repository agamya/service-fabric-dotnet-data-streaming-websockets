// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockAggregatorService
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Model;
    using Common.Shared;
    using global::StockAggregatorService.Interfaces;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using StockTrendPredictionActor.Interfaces;

    public class StockAggregatorService : StatefulService, IStockAggregatorService
    {
        private const string ProductsCollectionName = "aggregatedproducts";
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(StockAggregatorService));

        public StockAggregatorService(StatefulServiceContext context)
            : base(context)
        {
        }

        public async Task<IEnumerable<ProductStockPrediction>> GetAllProducts()
        {
            Logger.Debug(nameof(this.GetAllProducts));

            IReliableDictionary<int, ProductStockPrediction> productsCollection =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<int, ProductStockPrediction>>(ProductsCollectionName);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                return (await productsCollection.CreateEnumerableAsync(tx)).ToEnumerable()
                    .OrderByDescending(p => p.Value.Probability)
                    .Select(p => p.Value);
            }
        }

        public async Task<IEnumerable<ProductStockPrediction>> GetProducts(float probability)
        {
            Logger.Debug(nameof(this.GetProducts));

            IReliableDictionary<int, ProductStockPrediction> productsCollection =
                await this.StateManager.GetOrAddAsync<IReliableDictionary<int, ProductStockPrediction>>(ProductsCollectionName);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                return (await productsCollection.CreateEnumerableAsync(tx)).ToEnumerable()
                    .Where(p => p.Value.Probability >= probability)
                    .OrderByDescending(p => p.Value.Probability)
                    .Select(p => p.Value);
            }
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(
                    initParams =>
                        this.CreateServiceRemotingListener<StockAggregatorService>(initParams))
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            Logger.Debug(nameof(this.RunAsync));
            try
            {
                INotificationActor notificationActor = ConnectionFactory.CreateNotificationActor();
                await notificationActor.SubscribeAsync(new StockEventsHandler(this.StateManager, ProductsCollectionName));

                await base.RunAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(this.RunAsync));
                throw;
            }
        }
    }
}