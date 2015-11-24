// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway
{
    using System;
    using System.Fabric;
    using System.Fabric.Description;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Model;
    using Common.Shared.Serializers;
    using Microsoft.ServiceFabric.Data;
    using Microsoft.ServiceFabric.Services.Communication.Runtime;

    public class WebSocketListener : ICommunicationListener
    {
        private const int MaxBufferSize = 102400;
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(WebSocketListener));
        public readonly string __webSocketRoot;

        private readonly string _appRoot;
        private readonly IReliableStateManager _stateManager;

        private readonly ServiceInitializationParameters _serviceInitializationParameters;

        private Task _mainLoop;
        private string _listeningAddress;
        private string _publishAddress;
        // Web Socket listener
        private WebSocketApp _webSocketApp;

        private Func<IReliableStateManager, MsgSpec, CancellationToken, Task<MsgSpec>> _appAction;

        public WebSocketListener(
            IReliableStateManager stateManager,
            string appRoot,
            string webSocketRoot,
            ServiceInitializationParameters serviceInitializationParameters,
            Func<IReliableStateManager, MsgSpec, CancellationToken, Task<MsgSpec>> appAction
            )
        {
            this._stateManager = stateManager;
            this._appRoot = appRoot;
            this.__webSocketRoot = webSocketRoot;
            this._appAction = appAction;
            this._serviceInitializationParameters = serviceInitializationParameters;
        }

        public async Task<string> OpenAsync(CancellationToken cancellationToken)
        {
            Logger.Debug(nameof(this.OpenAsync));

            try
            {
                EndpointResourceDescription serviceEndpoint = this._serviceInitializationParameters.CodePackageActivationContext.GetEndpoint("ServiceEndpoint");
                int port = serviceEndpoint.Port;

                this._listeningAddress = string.Format(
                    CultureInfo.InvariantCulture,
                    "http://+:{0}/{1}",
                    port,
                    string.IsNullOrWhiteSpace(this._appRoot)
                        ? string.Empty
                        : this._appRoot.TrimEnd('/') + '/');

                this._publishAddress = this._listeningAddress.Replace("+", FabricRuntime.GetNodeContext().IPAddressOrFQDN);

                // Publish "ws" address!
                this._publishAddress = this._publishAddress.Replace("http", "ws");

                Logger.Info("Starting websocket listener on {0}", this._listeningAddress);
                this._webSocketApp = new WebSocketApp(this._listeningAddress);
                this._webSocketApp.Init();

                this._mainLoop = this._webSocketApp.StartAsync(this.ProcessConnectionAsync);

                return await Task.FromResult(this._publishAddress);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(this.OpenAsync));
                throw;
            }
        }

        public Task CloseAsync(CancellationToken cancellationToken)
        {
            this.StopAll();
            return Task.FromResult(true);
        }

        public void Abort()
        {
            this.StopAll();
        }

        /// <summary>
        /// Stops, cancels, and disposes everything.
        /// </summary>
        private void StopAll()
        {
            Logger.Debug(nameof(this.StopAll));

            try
            {
                this._webSocketApp.Dispose();
                if (this._mainLoop != null)
                {
                    // allow a few seconds to complete the main loop
                    if (!this._mainLoop.Wait(TimeSpan.FromSeconds(3)))
                        Logger.Warning("MainLoop did not complete within allotted time");

                    this._mainLoop.Dispose();
                    this._mainLoop = null;
                }

                this._listeningAddress = string.Empty;
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private async Task<bool> ProcessConnectionAsync(CancellationToken cancellationToken, HttpListenerContext httpContext)
        {
            Logger.Debug("ProcessConnectionAsync");

            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await httpContext.AcceptWebSocketAsync(null);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "AcceptWebSocketAsync");

                // The upgrade process failed somehow. For simplicity lets assume it was a failure on the part of the server and indicate this using 500.
                httpContext.Response.StatusCode = 500;
                httpContext.Response.Close();
                return false;
            }

            WebSocket webSocket = webSocketContext.WebSocket;
            MemoryStream ms = new MemoryStream();
            try
            {
                byte[] receiveBuffer = null;
                int payloadbytes = 0;

                // While the WebSocket connection remains open run a simple loop that receives data and sends it back.
                while (webSocket.State == WebSocketState.Open)
                {
                    try
                    {
                        if (receiveBuffer == null)
                            receiveBuffer = new byte[MaxBufferSize];

                        WebSocketReceiveResult receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), cancellationToken);
                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                        {
                            Logger.Debug("ProcessConnectionAsync: closing websocket");
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cancellationToken);
                            continue;
                        }

                        if (receiveResult.EndOfMessage)
                        {
                            await ms.WriteAsync(receiveBuffer, (int) ms.Position, receiveResult.Count, cancellationToken);
                            ms.Dispose();
                            ms = new MemoryStream();
                            payloadbytes += receiveResult.Count;
                        }
                        else
                        {
                            await ms.WriteAsync(receiveBuffer, (int) ms.Position, receiveResult.Count, cancellationToken);
                            payloadbytes += receiveResult.Count;
                            continue;
                        }

                        IWsSerializer mserializer = new ProtobufWsSerializer();
                        MsgSpec mresp;
                        try
                        {
                            // Derialize websocket request
                            MsgSpec mreq = await mserializer.DeserializeAsync<MsgSpec>(receiveBuffer, 0, payloadbytes);

                            // dispatch to App provided function with requested payload
                            mresp = await this._appAction(this._stateManager, mreq, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            // catch any error in the appAction and notify the client
                            mresp = new MsgSpec()
                            {
                                Key = MsgSpecKeys.Error,
                                Value = Encoding.UTF8.GetBytes(ex.Message)
                            };
                        }

                        payloadbytes = 0;

                        // Send Result back to client
                        await
                            webSocket.SendAsync(
                                new ArraySegment<byte>(await mserializer.SerializeAsync(mresp)),
                                WebSocketMessageType.Binary,
                                true,
                                cancellationToken);
                    }
                    catch (WebSocketException ex)
                    {
                        Logger.Error(ex, "ProcessConnectionAsync: WebSocketException={0}", webSocket.State);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "ProcessConnectionAsync");
                return false;
            }
        }
    }
}