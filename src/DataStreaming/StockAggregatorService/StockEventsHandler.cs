// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockAggregatorService
{
    using System;
    using System.Collections.Generic;
    using Common.Logging;
    using Common.Model;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using StockTrendPredictionActor.Interfaces;

    internal class StockEventsHandler : IStockEvents
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(StockEventsHandler));
        private readonly string productsCollectionName;
        private readonly IReliableStateManager stateManager;

        public StockEventsHandler(IReliableStateManager stateMgr, string productsCollectionName)
        {
            if (stateMgr == null)
            {
                throw new ArgumentNullException(nameof(this.stateManager));
            }
            this.stateManager = stateMgr;

            this.productsCollectionName = productsCollectionName;
        }

        public async void OnLowStockProducts(List<ProductStockPrediction> productPredictions)
        {
            Logger.Debug(nameof(this.OnLowStockProducts));

            IReliableDictionary<int, ProductStockPrediction> productsCollection =
                await this.stateManager.GetOrAddAsync<IReliableDictionary<int, ProductStockPrediction>>(this.productsCollectionName);

            using (ITransaction tx = this.stateManager.CreateTransaction())
            {
                foreach (ProductStockPrediction prediction in productPredictions)
                {
                    await productsCollection.SetAsync(tx, prediction.ProductId, prediction);
                }

                await tx.CommitAsync();
            }
        }
    }
}