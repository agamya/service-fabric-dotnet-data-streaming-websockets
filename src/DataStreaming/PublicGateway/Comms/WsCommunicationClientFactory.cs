// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Comms
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Microsoft.ServiceFabric.Services.Communication.Client;

    /// <summary>
    /// WebSockets communication client factory for StockService.
    /// </summary>
    public class WsCommunicationClientFactory : CommunicationClientFactoryBase<WsCommunicationClient>
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(WsCommunicationClientFactory));
        private static readonly TimeSpan MaxRetryBackoffIntervalOnNonTransientErrors = TimeSpan.FromSeconds(3);

        protected override bool ValidateClient(WsCommunicationClient client)
        {
            return client.ValidateClient();
        }

        protected override bool ValidateClient(string endpoint, WsCommunicationClient client)
        {
            return client.ValidateClient(endpoint);
        }

        protected override async Task<WsCommunicationClient> CreateClientAsync(
            string endpoint,
            CancellationToken cancellationToken
            )
        {
            Logger.Debug("CreateClientAsync: {0}", endpoint);

            if (string.IsNullOrEmpty(endpoint) || !endpoint.StartsWith("ws"))
            {
                throw new InvalidOperationException("The endpoint address is not valid. Please resolve again.");
            }

            string endpointAddress = endpoint;
            if (!endpointAddress.EndsWith("/"))
            {
                endpointAddress = endpointAddress + "/";
            }

            // Create a communication client. This doesn't establish a session with the server.
            WsCommunicationClient client = new WsCommunicationClient(endpointAddress);
            await client.ConnectAsync(cancellationToken);

            return client;
        }

        protected override void AbortClient(WsCommunicationClient client)
        {
            // Http communication doesn't maintain a communication channel, so nothing to abort.
            Logger.Debug("AbortClient");
        }
    }
}