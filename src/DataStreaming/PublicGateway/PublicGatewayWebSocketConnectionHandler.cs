// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway
{
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Model;
    using Common.Shared;
    using Common.Shared.Serializers;
    using Common.Shared.Websockets;
    using global::PublicGateway.Comms;
    using Microsoft.ServiceFabric.Services.Client;
    using Microsoft.ServiceFabric.Services.Communication.Client;

    public class PublicGatewayWebSocketConnectionHandler : IWebSocketConnectionHandler
    {
        private readonly ICommunicationClientFactory<WsCommunicationClient> clientFactory
            = new WsCommunicationClientFactory();

        public async Task<byte[]> ProcessWsMessageAsync(
            byte[] wsrequest, CancellationToken cancellationToken
            )
        {
            IWsSerializer mserializer = new ProtobufWsSerializer();
            WsRequestMessage mrequest = await mserializer.DeserializeAsync<WsRequestMessage>(wsrequest);

            ServicePartitionClient<WsCommunicationClient> serviceClient =
                new ServicePartitionClient<WsCommunicationClient>(
                    this.clientFactory,
                    ConnectionFactory.StockServiceUri,
                    partitionKey: new ServicePartitionKey(mrequest.PartitionKey),
                    listenerName: ServiceConst.ListenerWebsocket);

            return await serviceClient.InvokeWithRetryAsync(
                async client => await client.SendReceiveAsync(wsrequest),
                cancellationToken
                );
        }
    }
}