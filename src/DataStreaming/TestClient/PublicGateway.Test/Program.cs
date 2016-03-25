// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Test
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Common.Model;
    using Common.Shared;

    public static class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                ThreadPool.SetMinThreads(10000, 500);
                ServicePointManager.DefaultConnectionLimit = int.MaxValue;
                ServicePointManager.UseNagleAlgorithm = false;
                ServicePointManager.Expect100Continue = false;

                if (args.Length == 0)
                {
                    throw new ArgumentException("Invalid number of input parameters");
                }

                else if (args[0].Equals("purchase"))
                {
                    LoadClientTest loadTest = new LoadClientTest();
                    IEnumerable<string> ids = args.Skip(1);
                    loadTest.ProductIdsToPurchase.AddRange(ids.Select(int.Parse));
                    loadTest.Purchase_MultipleProductsInParametersRandomly_Nothing();
                }
                else if (args[0].Equals("reorder"))
                {
                    int quantityPerProduct = 1;
                    int delayMs = -1;

                    if (args.Length > 1)
                    {
                        quantityPerProduct = Convert.ToInt32(args[1]);
                    }
                    if (args.Length > 2)
                    {
                        delayMs = Convert.ToInt32(args[2]);
                    }

                    LoadClientTest loadTest = new LoadClientTest();

                    // Re stock items on command line
                    if (args.Length > 3)
                    {
                        IEnumerable<string> ids = args.Skip(3);
                        loadTest.ProductIdsToOrder.AddRange(ids.Select(int.Parse));
                    }
                    else
                    {
                        // Re stock all items
                        BulkDataSource bulkDataSource = new BulkDataSource();
                        loadTest.ProductIdsToOrder.AddRange(bulkDataSource.ReadAllProducts().Select(p => p.ProductId));
                    }

                    loadTest.Order_MultipleProductsInParameters(quantityPerProduct, delayMs);
                }
                else if (args[0].Equals("stream"))
                {
                    LoadClientTest.TestProtocolType testProtocolType = LoadClientTest.TestProtocolType.WebSockets;
                    int threadCount = 1;
                    int iterationsPerThread = 1;
                    int delayMs = -1;
                    int maxProductsPerThread = -1;

                    // Read Parameters
                    if (args.Length > 1)
                    {
                        if (args[1].ToLower().Equals("http"))
                        {
                            testProtocolType = LoadClientTest.TestProtocolType.Http;
                        }
                        else if (args[1].ToLower().Equals("websockets") || args[1].ToLower().Equals("websocket"))
                        {
                            testProtocolType = LoadClientTest.TestProtocolType.WebSockets;
                        }
                        else
                        {
                            throw new ArgumentException("Invalid streaming protocol 'http|websockets'");
                        }
                    }
                    if (args.Length > 2)
                    {
                        threadCount = Convert.ToInt32(args[2]);
                    }
                    if (args.Length > 3)
                    {
                        iterationsPerThread = Convert.ToInt32(args[3]);
                    }
                    if (args.Length > 4)
                    {
                        maxProductsPerThread = Convert.ToInt32(args[4]);
                    }
                    if (args.Length > 5)
                    {
                        delayMs = Convert.ToInt32(args[5]);
                    }

                    // Print parameters
                    Console.WriteLine(
                        "Streaming: Protocol={0}\r\nThreadCount={1}\r\niterationsPerThread={2}\r\nMax Products per Thread={3}\r\nThread sleep={4}ms",
                        args[1],
                        threadCount,
                        iterationsPerThread,
                        maxProductsPerThread,
                        delayMs);


                    // Initialize and launch test...
                    LoadClientTest loadTest = new LoadClientTest();
                    BulkDataSource bulkDataSource = new BulkDataSource();
                    foreach (Product p in bulkDataSource.ReadAllProducts())
                    {
                        loadTest.ProductIdsToPurchase.Add(p.ProductId);
                    }
                    loadTest.Purchase_MultipleProductsInParametersRandomly_Stream(
                        testProtocolType,
                        threadCount,
                        iterationsPerThread,
                        maxProductsPerThread,
                        delayMs);
                }
                else
                {
                    throw new ArgumentException("Invalid input parameters");
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Please enter: purchase or reorder and a list of numbers");
                Console.WriteLine("To purchase: purchase 771 772 773 774");
                Console.WriteLine("To reorder: reorder (Items per Product) (Thread sleep in Milliseconds None=-1) [771 772 773 774 ...]");
                Console.WriteLine(
                    "To stream: stream http|websockets threadcount iterationsperthread [Max # Products per Thread (Default=-1 All)] [Thread sleep in Milliseconds (Default=-1 None)]");
            }

            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.ReadLine();
            }
        }
    }
}