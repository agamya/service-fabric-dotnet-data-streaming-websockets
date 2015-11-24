// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway
{
    using System;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;

    public class WebSocketApp : IDisposable
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(WebSocketApp));
        private static byte[] _uncaughtHttpBytes = Encoding.Default.GetBytes("Uncaught error in main processing loop!");

        private string _address;
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationToken _cancellationToken;
        private HttpListener _httpListener;

        public WebSocketApp(string address)
        {
            this._address = address;
            this._cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            Logger.Debug("Dispose");

            try
            {
                if (this._cancellationTokenSource != null && !this._cancellationTokenSource.IsCancellationRequested)
                    this._cancellationTokenSource.Cancel();

                if (this._httpListener != null && this._httpListener.IsListening)
                {
                    this._httpListener.Stop();
                    this._httpListener.Close();
                }

                if (this._cancellationTokenSource != null && !this._cancellationTokenSource.IsCancellationRequested)
                    this._cancellationTokenSource.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (AggregateException ae)
            {
                ae.Handle(
                    ex =>
                    {
                        Logger.Error(ex, nameof(this.Dispose));
                        return true;
                    });
            }
        }

        public void Init()
        {
            if (!this._address.EndsWith("/"))
            {
                this._address += "/";
            }

            this._httpListener = new HttpListener();
            this._httpListener.Prefixes.Add(this._address);
            this._cancellationTokenSource = new CancellationTokenSource();
            this._cancellationToken = this._cancellationTokenSource.Token;
            this._httpListener.Start();
        }

        public async Task StartAsync(
            Func<CancellationToken, HttpListenerContext, Task<bool>> processActionAsync
            )
        {
            while (this._httpListener.IsListening)
            {
                HttpListenerContext context = null;
                try
                {
                    context = await this._httpListener.GetContextAsync();
                    Logger.Debug("GetContextAsync complete");
                }
                catch (Exception ex)
                {
                    // check if the exception is caused due to cancellation
                    if (this._cancellationToken.IsCancellationRequested)
                        return;

                    Logger.Error(ex, "Error in GetContextAsync");
                    continue;
                }

                if (this._cancellationToken.IsCancellationRequested)
                    return;

                // a new connection is established, dispatch to the callback function
                this.DispatchConnectedContext(context, processActionAsync);
            }
        }


        private void DispatchConnectedContext(
            HttpListenerContext context,
            Func<CancellationToken, HttpListenerContext, Task<bool>> processActionAsync
            )
        {
            // do not await on processAction since we don't want to block on waiting for more connections
            processActionAsync(this._cancellationToken, context)
                .ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted)
                        {
                            Logger.Error(t.Exception, "processAction did not handle their exceptions");
                            try
                            {
                                context.Response.ContentLength64 = _uncaughtHttpBytes.Length;
                                context.Response.StatusCode = 500;
                                context.Response.OutputStream.Write(_uncaughtHttpBytes, 0, _uncaughtHttpBytes.Length);
                                context.Response.OutputStream.Close();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Couldn't write the 500 for misbehaving user");
                            }
                        }
                    },
                    this._cancellationToken);
        }
    }
}