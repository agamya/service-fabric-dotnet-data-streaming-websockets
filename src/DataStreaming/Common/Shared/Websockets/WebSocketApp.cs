// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Shared.Websockets
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

        private static readonly byte[] UncaughtHttpBytes =
            Encoding.Default.GetBytes("Uncaught error in main processing loop!");

        private string address;
        private CancellationToken cancellationToken;
        private CancellationTokenSource cancellationTokenSource;
        private HttpListener httpListener;

        public WebSocketApp(string address)
        {
            this.address = address;
            this.cancellationTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            Logger.Debug("Dispose");

            try
            {
                if (this.cancellationTokenSource != null && !this.cancellationTokenSource.IsCancellationRequested)
                {
                    this.cancellationTokenSource.Cancel();
                }

                if (this.httpListener != null && this.httpListener.IsListening)
                {
                    this.httpListener.Stop();
                    this.httpListener.Close();
                }

                if (this.cancellationTokenSource != null && !this.cancellationTokenSource.IsCancellationRequested)
                {
                    this.cancellationTokenSource.Dispose();
                }
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
            if (!this.address.EndsWith("/"))
            {
                this.address += "/";
            }

            this.httpListener = new HttpListener();
            this.httpListener.Prefixes.Add(this.address);
            this.cancellationTokenSource = new CancellationTokenSource();
            this.cancellationToken = this.cancellationTokenSource.Token;
            this.httpListener.Start();
        }

        public async Task StartAsync(
            Func<CancellationToken, HttpListenerContext, Task<bool>> processActionAsync
            )
        {
            while (this.httpListener.IsListening)
            {
                HttpListenerContext context = null;
                try
                {
                    context = await this.httpListener.GetContextAsync();
                    Logger.Debug("GetContextAsync complete");
                }
                catch (Exception ex)
                {
                    // check if the exception is caused due to cancellation
                    if (this.cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    Logger.Error(ex, "Error in GetContextAsync");
                    continue;
                }

                if (this.cancellationToken.IsCancellationRequested)
                {
                    return;
                }

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
            processActionAsync(this.cancellationToken, context)
                .ContinueWith(
                    t =>
                    {
                        if (t.IsFaulted)
                        {
                            Logger.Error(t.Exception, "processAction did not handle their exceptions");
                            try
                            {
                                context.Response.ContentLength64 = UncaughtHttpBytes.Length;
                                context.Response.StatusCode = 500;
                                context.Response.OutputStream.Write(UncaughtHttpBytes, 0, UncaughtHttpBytes.Length);
                                context.Response.OutputStream.Close();
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Couldn't write the 500 for misbehaving user");
                            }
                        }
                    },
                    this.cancellationToken);
        }
    }
}