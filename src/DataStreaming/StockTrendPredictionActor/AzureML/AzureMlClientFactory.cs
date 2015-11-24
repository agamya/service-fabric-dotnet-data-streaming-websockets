// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor.AzureML
{
    using System;
    using Common.Logging;
    using Common.Shared;

    public static class AzureMlClientFactory
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(AzureMlClientFactory));

        public static IAzureMlClient CreateClient()
        {
            IAzureMlClient mlClient;

            string client = ConfigurationHelper.ReadValue("AzureML", "Client");
            if (client == "Azure")
            {
                string serviceUri = ConfigurationHelper.ReadValue("AzureML", "AzureMlApiUrl");
                string mlKey = ConfigurationHelper.ReadValue("AzureML", "AzureMlKey");
                serviceUri += "&details=true";
                mlClient = new AzureMlClient(new Uri(serviceUri), mlKey);

                Logger.Debug("created Azure ML client, url: {0}", serviceUri); //don't log the private key!
            }
            else
            {
                mlClient = new MockedAzureMlClient();
                Logger.Debug("created Mocked Azure ML client");
            }

            return mlClient;
        }
    }
}