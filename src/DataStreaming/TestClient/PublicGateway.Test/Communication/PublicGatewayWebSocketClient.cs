// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Test
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Model;
    using Common.Shared;
    using Common.Shared.Serializers;

    public class PublicGatewayWebSocketClient : IDisposable
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(PublicGatewayWebSocketClient));
        private WebSocketManager websocketManager = null;

        public PublicGatewayWebSocketClient()
        {
        }

        public void Dispose()
        {
            try
            {
                this.CloseAsync().Wait();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (AggregateException ae)
            {
                ae.Handle(
                    ex =>
                    {
                        Logger.Error(ex, "");
                        return true;
                    });
            }
            finally
            {
                this.websocketManager = null;
            }
        }

        public async Task<bool> ConnectAsync(string serviceName)
        {
            Uri serviceAddress = new Uri(new Uri(serviceName), ServiceConst.DataApiWebsockets);

            return await this.ConnectAsync(serviceAddress);
        }

        public async Task<bool> ConnectAsync(Uri serviceAddress)
        {
            if (this.websocketManager == null)
            {
                this.websocketManager = new WebSocketManager();
            }

            return await this.websocketManager.ConnectAsync(serviceAddress);
        }

        public async Task CloseAsync()
        {
            try
            {
                if (this.websocketManager != null)
                {
                    await this.websocketManager.CloseAsync();
                    this.websocketManager.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "");
            }
            finally
            {
                this.websocketManager = null;
            }
        }

        /// <summary>
        /// Re-uses the open websocket connection (assumes one is already created/connected)
        /// </summary>
        public async Task<WsResponseMessage> SendReceiveAsync(WsRequestMessage msgspec, CancellationToken cancellationToken)
        {
            if (this.websocketManager == null)
            {
                throw new ApplicationException("SendReceiveAsync requires an open websocket client");
            }

            // Serialize Msg payload
            IWsSerializer mserializer = new ProtobufWsSerializer();

            byte[] request = await mserializer.SerializeAsync(msgspec);

            byte[] response = await this.websocketManager.SendReceiveAsync(request);

            return await mserializer.DeserializeAsync<WsResponseMessage>(response);
        }
    }
}