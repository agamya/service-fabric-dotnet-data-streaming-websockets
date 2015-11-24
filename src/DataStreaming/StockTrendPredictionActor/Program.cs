// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor
{
    using System;
    using System.Fabric;
    using System.Net;
    using System.Threading;
    using Common.Shared.Logging;
    using Microsoft.ServiceFabric.Actors;

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

                LoggingSource.Initialize(ActorEventSource.Current.Message);

                using (FabricRuntime fabricRuntime = FabricRuntime.Create())
                {
                    fabricRuntime.RegisterActor<StockTrendPredictionActor>();
                    fabricRuntime.RegisterActor<NotificationActor>();

                    Thread.Sleep(Timeout.Infinite);
                }
            }
            catch (Exception e)
            {
                ActorEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}