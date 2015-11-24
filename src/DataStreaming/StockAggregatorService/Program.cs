// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockAggregatorService
{
    using System;
    using System.Diagnostics;
    using System.Fabric;
    using System.Threading;
    using Common.Shared.Logging;

    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                // Initializing ILogger
                LoggingSource.Initialize(ServiceEventSource.Current.Message);

                using (FabricRuntime fabricRuntime = FabricRuntime.Create())
                {
                    // This is the name of the ServiceType that is registered with FabricRuntime. 
                    // This name must match the name defined in the ServiceManifest. If you change
                    // this name, please change the name of the ServiceType in the ServiceManifest.
                    fabricRuntime.RegisterServiceType("StockAggregatorServiceType", typeof(StockAggregatorService));

                    ServiceEventSource.Current.ServiceTypeRegistered(Process.GetCurrentProcess().Id, typeof(StockAggregatorService).Name);

                    Thread.Sleep(Timeout.Infinite);
                }
            }
            catch (Exception e)
            {
                ServiceEventSource.Current.ServiceHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}