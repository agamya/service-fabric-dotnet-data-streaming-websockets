// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Test
{
    using System;
    using System.IO;
    using System.Net.WebSockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;

    public class WebSocketManager : IDisposable
    {
        private const int MaxBufferSize = 10240;
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(WebSocketManager));
        private ClientWebSocket clientWebSocket = null;
        private byte[] receiveBytes = new byte[MaxBufferSize];

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
        }

        public async Task<bool> ConnectAsync(Uri serviceAddress)
        {
            this.clientWebSocket = new ClientWebSocket();

            using (CancellationTokenSource tcs = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                await this.clientWebSocket.ConnectAsync(serviceAddress, tcs.Token);
            }

            return true;
        }

        public async Task<byte[]> SendReceiveAsync(byte[] payload)
        {
            try
            {
                // Send request operation
                await this.clientWebSocket.SendAsync(new ArraySegment<byte>(payload), WebSocketMessageType.Binary, true, CancellationToken.None);

                WebSocketReceiveResult receiveResult =
                    await this.clientWebSocket.ReceiveAsync(new ArraySegment<byte>(this.receiveBytes), CancellationToken.None);

                using (MemoryStream ms = new MemoryStream())
                {
                    await ms.WriteAsync(this.receiveBytes, 0, receiveResult.Count);
                    return ms.ToArray();
                }
            }
            catch (WebSocketException exweb)
            {
                Logger.Error(exweb, "");
                this.CloseAsync().Wait();
            }

            return null;
        }

        public async Task CloseAsync()
        {
            try
            {
                if (this.clientWebSocket != null)
                {
                    if (this.clientWebSocket.State != WebSocketState.Closed)
                    {
                        await this.clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    }
                    this.clientWebSocket.Dispose();
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "");
            }
            finally
            {
                this.clientWebSocket = null;
            }
        }
    }
}