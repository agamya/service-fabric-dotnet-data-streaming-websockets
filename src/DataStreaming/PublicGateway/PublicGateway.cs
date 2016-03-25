// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway
{
    using System;
    using System.Collections.Generic;
    using System.Fabric;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Shared;
    using Common.Shared.Websockets;
    using Microsoft.ServiceFabric.Actors.Client;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;
    using Microsoft.ServiceFabric.Services.Runtime;
    using StockTrendPredictionActor.Interfaces;

    public class PublicGateway : StatelessService
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(PublicGateway));

        public PublicGateway(StatelessServiceContext context)
            : base(context)
        {
        }

        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return new[]
            {
                // Adding 2 or more listeners requires a unique name for each
                new ServiceInstanceListener(
                    initParams => new OwinCommunicationListener("", new Startup(), initParams),
                    ServiceConst.ListenerOwin),
                new ServiceInstanceListener(
                    initParams => new WebSocketListener(null, "PublicGatewayWS", initParams, () => new PublicGatewayWebSocketConnectionHandler()),
                    ServiceConst.ListenerWebsocket)
            };
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            Logger.Debug(nameof(this.RunAsync));

            try
            {
                INotificationActor notificationActor = ConnectionFactory.CreateNotificationActor();
                await notificationActor.SubscribeAsync(new StockEventsHandler());

                await base.RunAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(this.RunAsync));
                throw;
            }
        }
    }
}