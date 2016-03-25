// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Comms
{
    using System;
    using System.Fabric;
    using System.IO;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Microsoft.ServiceFabric.Services.Communication.Client;

    /// <summary>
    /// Communication client that wraps the logic for talking to the StockService service.
    /// Created by communication client factory.
    /// </summary>
    public class WsCommunicationClient : ICommunicationClient
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(WsCommunicationClient));
        private ClientWebSocket clientWebSocket = null;

        public WsCommunicationClient(string baseAddress)
        {
            Logger.Debug("ctor: {0}", baseAddress);

            this.clientWebSocket = new ClientWebSocket();

            this.BaseAddress = baseAddress;
        }

        /// <summary>
        /// Base address of the client
        /// </summary>
        public string BaseAddress { get; }

        /// <summary>
        /// The resolved service partition which contains the resolved service endpoints.
        /// </summary>
        public ResolvedServicePartition ResolvedServicePartition { get; set; }

        public string ListenerName { get; set; }

        public ResolvedServiceEndpoint Endpoint { get; set; }

        public async Task<byte[]> SendReceiveAsync(byte[] payload)
        {
            byte[] receiveBytes = new byte[10240];

            // Send request operation
            await this.clientWebSocket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Binary, true, CancellationToken.None);

            WebSocketReceiveResult receiveResult =
                await this.clientWebSocket.ReceiveAsync(new ArraySegment<byte>(receiveBytes), CancellationToken.None);

            using (MemoryStream ms = new MemoryStream())
            {
                await ms.WriteAsync(receiveBytes, 0, receiveResult.Count);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        internal bool ValidateClient()
        {
            if (this.clientWebSocket == null)
            {
                return false;
            }

            if (this.clientWebSocket.State != WebSocketState.Open && this.clientWebSocket.State != WebSocketState.Connecting)
            {
                this.clientWebSocket.Dispose();
                this.clientWebSocket = null;
                return false;
            }

            return true;
        }

        internal bool ValidateClient(string endpoint)
        {
            if (this.BaseAddress == endpoint)
            {
                return true;
            }

            this.clientWebSocket.Dispose();
            this.clientWebSocket = null;
            return false;
        }

        internal async Task ConnectAsync(CancellationToken cancellationToken)
        {
            await this.clientWebSocket.ConnectAsync(new Uri(this.BaseAddress), cancellationToken);
        }
    }
}