// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockService
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Threading;
    using Common.Shared.Logging;
    using Microsoft.ServiceFabric.Services.Runtime;

    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                ThreadPool.SetMinThreads(10000, 500);
                ServicePointManager.DefaultConnectionLimit = int.MaxValue;
                ServicePointManager.UseNagleAlgorithm = false;
                ServicePointManager.Expect100Continue = false;

                LoggingSource.Initialize(ServiceEventSource.Current.Message);

                ServiceRuntime.RegisterServiceAsync(
                    "StockServiceType",
                    context =>
                        new StockService(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(StockService).Name);

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}