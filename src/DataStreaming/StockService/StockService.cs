// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockService
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Model;
    using Common.Shared;
    using Common.Shared.Serializers;
    using Common.Shared.Websockets;
    using global::StockService.Interfaces;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Data.Collections;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Remoting.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using StockTrendPredictionActor.Interfaces;

    public class StockService : StatefulService, IStockService, IWebSocketConnectionHandler
    {
        private const string ProductsCollectionName = "products";
        private const string OrdersCollectionName = "orders";
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(StockService));
        private TimeSpan processOrdersInterval;
        private IReliableDictionary<int, Product> productsCollection = null;
        private IReliableDictionary<string, ProductPurchase> purchaseLog = null;

        public StockService(StatefulServiceContext context)
            : base(context)
        {
        }

        public async Task<Product> GetProduct(int productId)
        {
            Logger.Debug("GetProduct: #{0}", productId);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<Product> item = await this.productsCollection.TryGetValueAsync(tx, productId);
                return item.HasValue ? item.Value : null;
            }
        }

        public async Task<int> PurchaseProduct(int productId, int quantity)
        {
            Logger.Debug("PurchaseProduct: product #{0} for {1} items", productId, quantity);

            try
            {
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    ConditionalValue<Product> item = await this.productsCollection.TryGetValueAsync(tx, productId, LockMode.Update);

                    Product product = item.HasValue ? item.Value : null;

                    if (product == null)
                    {
                        throw new ArgumentException("product not found", nameof(productId));
                    }
                    if (product.StockTotal < quantity)
                    {
                        return -1;
                    }

                    int reserved = Math.Min(product.StockTotal, quantity);
                    int targetStockReserved = product.StockReserved + reserved;
                    int targetStockTotal = product.StockTotal - reserved;

                    Logger.Debug(
                        "product name: {0}, stock total: {1}=>{2}, stock reserved: {3}=>{4}",
                        product.ProductName,
                        product.StockTotal,
                        targetStockTotal,
                        product.StockReserved,
                        targetStockReserved);

                    product.StockReserved = targetStockReserved;
                    product.StockTotal = targetStockTotal;

                    await this.productsCollection.SetAsync(tx, product.ProductId, product);

                    DateTime now = DateTime.UtcNow;
                    string orderKey = Guid.NewGuid().ToString();
                    await this.purchaseLog.AddAsync(
                        tx,
                        orderKey,
                        new ProductPurchase()
                        {
                            ProductId = product.ProductId,
                            ProductName = product.ProductName,
                            StockLeft = product.StockTotal,
                            Quantity = reserved,
                            PurchaseKey = orderKey,
                            Timestamp = now
                        });

                    // now commit transaction
                    await tx.CommitAsync();

                    return product.StockTotal;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(this.PurchaseProduct));
                throw;
            }
        }

        public async Task<bool> AddStock(int productId, int quantity)
        {
            Logger.Debug("AddStock: product #{0} for {1} items", productId, quantity);

            using (ITransaction tx = this.StateManager.CreateTransaction())
            {
                ConditionalValue<Product> item = await this.productsCollection.TryGetValueAsync(tx, productId);
                Product product = item.HasValue ? item.Value : null;

                if (product == null)
                {
                    throw new ArgumentException("product not found", nameof(productId));
                }

                int targetStockTotal = product.StockTotal + quantity;

                product.StockTotal = targetStockTotal;

                await this.productsCollection.SetAsync(tx, product.ProductId, product);
                await tx.CommitAsync();

                return true;
            }
        }

        /// <summary>
        /// IWebSocketListener.ProcessWsMessageAsync
        /// </summary>
        public async Task<byte[]> ProcessWsMessageAsync(byte[] wsrequest, CancellationToken cancellationToken)
        {
            Logger.Debug(nameof(this.ProcessWsMessageAsync));

            ProtobufWsSerializer mserializer = new ProtobufWsSerializer();

            WsRequestMessage mrequest = await mserializer.DeserializeAsync<WsRequestMessage>(wsrequest);

            switch (mrequest.Operation)
            {
                case WSOperations.AddItem:
                {
                    IWsSerializer pserializer = SerializerFactory.CreateSerializer();
                    PostProductModel payload = await pserializer.DeserializeAsync<PostProductModel>(mrequest.Value);
                    await this.PurchaseProduct(payload.ProductId, payload.Quantity);
                }
                    break;
            }

            WsResponseMessage mresponse = new WsResponseMessage
            {
                Result = WsResult.Success
            };

            return await mserializer.SerializeAsync(mresponse);
        }

        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return new[]
            {
                new ServiceReplicaListener(
                    context => this.CreateServiceRemotingListener<StockService>(context),
                    ServiceConst.ListenerRemoting
                    ),
                new ServiceReplicaListener(
                    context => new WebSocketListener("WsServiceEndpoint", "StockServiceWS", context, () => this),
                    ServiceConst.ListenerWebsocket
                    )
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            Logger.Debug(nameof(this.RunAsync));

            try
            {
                if (this.Partition.PartitionInfo.Kind != ServicePartitionKind.Int64Range)
                {
                    throw new ApplicationException("Partition kind is not Int64Range");
                }

                Int64RangePartitionInformation partitionInfo = (Int64RangePartitionInformation) this.Partition.PartitionInfo;
                int lowId = (int) partitionInfo.LowKey;
                int highId = (int) partitionInfo.HighKey;

                //bulk-import test data from CSV files
                Logger.Debug("attempting to bulk-import data");

                //get or create the "products" dictionary
                this.productsCollection =
                    await this.StateManager.GetOrAddAsync<IReliableDictionary<int, Product>>(ProductsCollectionName);
                //get or create the purchaseLog collection
                this.purchaseLog =
                    await this.StateManager.GetOrAddAsync<IReliableDictionary<string, ProductPurchase>>(OrdersCollectionName);

                if (!TimeSpan.TryParse(ConfigurationHelper.ReadValue("AppSettings", "ProcessOrdersInterval"), out this.processOrdersInterval))
                {
                    this.processOrdersInterval = TimeSpan.FromSeconds(3);
                }

                //check if data has been imported
                using (ITransaction tx = this.StateManager.CreateTransaction())
                {
                    if (await this.productsCollection.GetCountAsync(tx) <= 0)
                    {
                        Logger.Debug("Importing products with productId from {0} to {1}...", lowId, highId);

                        //bulk-import the data
                        BulkDataSource dataSource = new BulkDataSource();
                        foreach (Product p in dataSource.ReadProducts(lowId, highId))
                        {
                            await this.productsCollection.AddAsync(tx, p.ProductId, p);
                        }

                        await tx.CommitAsync();
                    }
                }

                // launch watcher thread
                await this.DispatchPurchaseLogAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(this.RunAsync));
                throw;
            }
        }

        /// <summary>
        /// Sends the current purchaseLog to StockTrendPredictionActor
        /// </summary>
        private async Task DispatchPurchaseLogAsync(CancellationToken cancellationToken)
        {
            string actorId = this.Partition.PartitionInfo.Id.ToString();

            while (true)
            {
                await Task.Delay(this.processOrdersInterval, cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                try
                {
                    using (ITransaction tx = this.StateManager.CreateTransaction())
                    {
                        long purchaseCount = await this.purchaseLog.GetCountAsync(tx);
                        if (purchaseCount <= 0)
                        {
                            continue;
                        }

                        // in real world you should use .Take() to selecte only limited number of orders
                        // and those in batches
                        List<ProductPurchase> orders = new List<ProductPurchase>((int) purchaseCount);

                        IAsyncEnumerable<KeyValuePair<string, ProductPurchase>> enumerable = await this.purchaseLog.CreateEnumerableAsync(tx);

                        await (enumerable.ForeachAsync(cancellationToken, item => { orders.Add(item.Value); }));


                        Logger.Debug("DispatchPurchaseLogAsync: {0} orders to process", orders.Count);

                        IStockTrendPredictionActor predictionActor = ConnectionFactory.CreateBatchedStockTrendPredictionActor(actorId);

                        try
                        {
                            // In real world, you should add retry logic in case actor is temporarily not available
                            await predictionActor.ProcessPurchasesAsync(orders);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "PredictionActor error");
                        }

                        // Clear up processed orders even if the actor is not availbale. 
                        // In real life you need to bound the orders log 
                        // even if the actor is not available 
                        // to avoid eating up memory and disk resources

                        foreach (ProductPurchase order in orders)
                        {
                            await this.purchaseLog.TryRemoveAsync(tx, order.PurchaseKey);
                        }

                        await tx.CommitAsync();
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, nameof(this.DispatchPurchaseLogAsync));
                    // do not throw out the exception, just keep iterating
                }
            }
        }
    }
}