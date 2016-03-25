// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockAggregatorService
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Common.Shared.Logging;
    using Microsoft.ServiceFabric.Services.Runtime;

    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                // Initializing ILogger
                LoggingSource.Initialize(ServiceEventSource.Current.Message);

                ServiceRuntime.RegisterServiceAsync(
                    "StockAggregatorServiceType",
                    context =>
                        new StockAggregatorService(context)).GetAwaiter().GetResult();

                ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(StockAggregatorService).Name);

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