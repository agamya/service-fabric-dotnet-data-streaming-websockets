// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Test
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Model;
    using Common.Shared;
    using Common.Shared.Serializers;

    public class LoadClientTest
    {
        public enum TestProtocolType
        {
            WebSockets = 0,
            Http = 1
        };

        private const int ReportStatusMs = 5000; // Report status every X seconds
        public readonly List<int> ProductIdsToPurchase = new List<int>();
        public readonly List<int> ProductIdsToOrder = new List<int>();
        public int PurchaseTimes = 1000;
        private PublicGatewayHttpClient client;

        public LoadClientTest()
        {
            this.client = new PublicGatewayHttpClient();
        }

        public void Purchase_MultipleProductsInParametersRandomly_Nothing()
        {
            int quantity = 0;
            Random rnd = new Random(DateTime.UtcNow.Millisecond);

            while (quantity++ < this.PurchaseTimes)
            {
                int delayMs = rnd.Next(0, 10)*1000;

                foreach (int productId in this.ProductIdsToPurchase)
                {
                    int immediateQuantity = rnd.Next(1, 4);

                    Console.WriteLine("purchasing {0} items on product {1}...", immediateQuantity, productId);

                    this.client.ExecutePostAsync(
                        ConnectionFactory.ReserveStockApiController,
                        new PostProductModel
                        {
                            // generating new user id for every iteration
                            ProductId = productId,
                            Quantity = immediateQuantity
                        }).Wait();
                }

                Console.WriteLine("sleeping for {0:N0}ms...", delayMs);
                Thread.Sleep(delayMs);
            }

            Console.WriteLine("Completed");
        }

        public void Purchase_MultipleProductsInParametersRandomly_Stream(
            TestProtocolType testProtocolType, int taskCount,
            int iterations, int maxProductsPerThread, int delayMs)
        {
            Task<int>[] tasks = new Task<int>[taskCount];
            int[] taskArgs = new int[taskCount];

            Stopwatch sw = Stopwatch.StartNew();
            Console.WriteLine("Starting Product Purchase Streaming {0} tasks {1:N0} iterations each...", taskCount, iterations);
            for (int i = 0; i < tasks.Length; i++)
            {
                taskArgs[i] = i;
                if (testProtocolType == TestProtocolType.Http)
                {
                    tasks[i] = this.DoWorkAsyncHttp(iterations, taskArgs[i], tasks.Length, maxProductsPerThread, delayMs);
                }
                else
                {
                    tasks[i] = this.DoWorkAsyncWebSocket(iterations, taskArgs[i], tasks.Length, maxProductsPerThread, delayMs);
                }
            }
            Task.WaitAll(tasks);
            sw.Stop();

            int iterationCounter = tasks.Sum(t => t.Result);

            Console.WriteLine("Streaming done! {0:N0} total iterations", iterationCounter);
            Console.WriteLine("Total Iterations " + OutputOperationsMs(iterationCounter, sw.ElapsedMilliseconds));
        }

        public async Task<int> DoWorkAsyncWebSocket(int iterationsMax, int threadId, int threadCount, int maxProductsPerThread, int delayMs)
        {
            int iterations = 0;
            int failedCounter = 0;

            try
            {
                Random rnd = new Random((threadId + 1)*DateTime.UtcNow.Millisecond);

                Stopwatch sw = Stopwatch.StartNew();
                long executionTime = sw.ElapsedMilliseconds;

                // Partition the Product Ids per thread
                int productListOffsetMin = (this.ProductIdsToPurchase.Count/threadCount)*threadId;
                int productListOffsetMax = (this.ProductIdsToPurchase.Count/threadCount)*(threadId + 1) - 1;
                // Use Max Products Per Thread?
                if (maxProductsPerThread > 0)
                {
                    maxProductsPerThread--;
                    if (productListOffsetMin + maxProductsPerThread < productListOffsetMax)
                    {
                        productListOffsetMax = productListOffsetMin + maxProductsPerThread;
                    }
                }
                //Console.WriteLine("{0} to {1} to {2}", threadId, productListOffsetMin, productListOffsetMax);

                int productId = 0;
                int immediateQuantity = 0;

                // Websocket client to public gateway
                using (PublicGatewayWebSocketClient websocketClient = new PublicGatewayWebSocketClient())
                {
                    await websocketClient.ConnectAsync(ConnectionFactory.WebSocketServerName);
                    while (iterations < iterationsMax)
                    {
                        productId = this.ProductIdsToPurchase[rnd.Next(productListOffsetMin, productListOffsetMax)];
                        immediateQuantity = rnd.Next(1, 4);

                        // Report status every X seconds
                        if ((sw.ElapsedMilliseconds - executionTime) > ReportStatusMs)
                        {
                            Console.WriteLine(OutputReport(threadId, immediateQuantity, productId, delayMs, failedCounter, iterations, sw.ElapsedMilliseconds));
                            executionTime = sw.ElapsedMilliseconds;
                        }

                        // Serialize PostDataModel
                        PostProductModel postProductModel = new PostProductModel
                        {
                            // generating new user id for every iteration
                            ProductId = productId,
                            Quantity = immediateQuantity
                        };

                        IWsSerializer serializer = SerializerFactory.CreateSerializer();
                        byte[] payload = await serializer.SerializeAsync(postProductModel);

                        // Websocket send & receive message spec
                        WsRequestMessage mreq = new WsRequestMessage
                        {
                            PartitionKey = productId,
                            Operation = WSOperations.AddItem,
                            Value = payload
                        };

                        WsResponseMessage mresp = await websocketClient.SendReceiveAsync(mreq, CancellationToken.None);
                        if (mresp.Result == WsResult.Error)
                        {
                            Console.WriteLine("Error: {0}", Encoding.UTF8.GetString(mresp.Value));
                        }

                        if (mresp.Result != WsResult.Success)
                        {
                            failedCounter++;
                        }

                        iterations++;

                        if (delayMs > -1 && iterations < iterationsMax)
                        {
                            await Task.Delay(delayMs);
                        }
                    }
                    Console.WriteLine(
                        "Completed: " + OutputReport(threadId, immediateQuantity, productId, delayMs, failedCounter, iterations - 1, sw.ElapsedMilliseconds));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return iterations;
        }

        public async Task<int> DoWorkAsyncHttp(int iterationsMax, int threadId, int threadCount, int maxProductsPerThread, int delayMs)
        {
            int iterations = 0;
            int failedCounter = 0;

            try
            {
                Random rnd = new Random((threadId + 1)*DateTime.UtcNow.Millisecond);

                Stopwatch sw = Stopwatch.StartNew();
                long executionTime = sw.ElapsedMilliseconds;

                // Partition the Product Ids per thread
                int productListOffsetMin = (this.ProductIdsToPurchase.Count/threadCount)*threadId;
                int productListOffsetMax = (this.ProductIdsToPurchase.Count/threadCount)*(threadId + 1) - 1;
                // Use Max Products Per Thread?
                if (maxProductsPerThread > 0)
                {
                    maxProductsPerThread--;
                    if (productListOffsetMin + maxProductsPerThread < productListOffsetMax)
                    {
                        productListOffsetMax = productListOffsetMin + maxProductsPerThread;
                    }
                }
                //Console.WriteLine("{0} to {1} to {2}", threadId, productListOffsetMin, productListOffsetMax);

                int productId = 0;
                int immediateQuantity = 0;

                while (iterations < iterationsMax)
                {
                    productId = this.ProductIdsToPurchase[rnd.Next(productListOffsetMin, productListOffsetMax)];
                    immediateQuantity = rnd.Next(1, 4);

                    // Report status every X seconds
                    if ((sw.ElapsedMilliseconds - executionTime) > ReportStatusMs)
                    {
                        Console.WriteLine(OutputReport(threadId, immediateQuantity, productId, delayMs, failedCounter, iterations, sw.ElapsedMilliseconds));
                        executionTime = sw.ElapsedMilliseconds;
                    }

                    PostProductModel postProductModel = new PostProductModel
                    {
                        // generating new user id for every iteration
                        ProductId = productId,
                        Quantity = immediateQuantity
                    };

                    string result = await this.client.ExecutePostAsync(ConnectionFactory.ReserveStockApiController, postProductModel);
                    if (!result.ToLower().Contains("success"))
                    {
                        failedCounter++;
                    }

                    iterations++;

                    if (delayMs > -1 && iterations < iterationsMax)
                    {
                        await Task.Delay(delayMs);
                    }
                }
                Console.WriteLine(
                    "Completed: " + OutputReport(threadId, immediateQuantity, productId, delayMs, failedCounter, iterations - 1, sw.ElapsedMilliseconds));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return iterations;
        }

        public void Order_MultipleProductsInParameters(int quantity, int delayMs)
        {
            Random rnd = new Random(DateTime.UtcNow.Millisecond);

            foreach (int productId in this.ProductIdsToOrder)
            {
                Console.WriteLine("Ordering {0} items for product {1}...", quantity, productId);
                this.client.ExecutePostAsync(
                    ConnectionFactory.StockApiController,
                    new PostProductModel
                    {
                        ProductId = productId,
                        Quantity = quantity
                    }).Wait();

                if (delayMs > -1)
                {
                    Console.WriteLine("sleeping for {0}ms...", delayMs);
                    Thread.Sleep(delayMs);
                }
            }
            Console.WriteLine("Done oredering items for products.");
        }

        #region Helpers

        public static string OutputReport(
            int threadId, int immediateQuantity, int productId,
            int delayMs, int failedCounter, int iterations, long elapsedMilliseconds)
        {
            return string.Format(
                "{0}) order {1} of product {2}, {3}{4}{5}",
                threadId,
                immediateQuantity,
                productId,
                (delayMs > -1 ? string.Format("Sleep {0}ms, ", delayMs) : ""),
                OutputOperationsMs(iterations + 1, elapsedMilliseconds),
                (failedCounter > 0 ? string.Format(" Failures={0:N0} ", failedCounter) : ""));
        }

        public static string OutputOperationsMs(int iterations, long ms)
        {
            return string.Format(
                "{0:N0} ops, {1:N0}ms, {2:N2} ops/sec, {3:N2}ms/op",
                iterations,
                ms,
                Convert.ToDouble(iterations)/ms*1000,
                (iterations > 0 ? ms/Convert.ToDouble(iterations) : 0));
        }

        #endregion
    }
}