// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Comms
{
    using System;
    using System.Net;
    using System.Net.Http;
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
                throw new InvalidOperationException("The endpoint address is not valid. Please resolve again.");

            string endpointAddress = endpoint;
            if (!endpointAddress.EndsWith("/"))
                endpointAddress = endpointAddress + "/";

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

        protected override bool OnHandleException(Exception e, out ExceptionHandlingResult result)
        {
            Logger.Debug("OnHandleException {0}", e);

            if (e is TimeoutException)
                return this.CreateExceptionHandlingResult(false, out result);

            if (e is ProtocolViolationException)
                return this.CreateExceptionHandlingResult(false, out result);

            if (e is HttpRequestException)
            {
                HttpRequestException he = (HttpRequestException) e;

                // this should not happen, but let's do a sanity check
                if (null == he.InnerException)
                    return this.CreateExceptionHandlingResult(false, out result);

                e = he.InnerException;
            }

            if (e is WebException)
            {
                WebException we = (WebException) e;
                HttpWebResponse errorResponse = we.Response as HttpWebResponse;

                if (we.Status == WebExceptionStatus.ProtocolError)
                {
                    if (errorResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        // This could either mean we requested an endpoint that does not exist in the service API (a user error)
                        // or the address that was resolved by fabric client is stale (transient runtime error) in which we should re-resolve.
                        return this.CreateExceptionHandlingResult(false, out result);
                    }

                    if (errorResponse.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        // The address is correct, but the server processing failed.
                        // This could be due to conflicts when writing the word to the dictionary.
                        // Retry the operation without re-resolving the address.
                        return this.CreateExceptionHandlingResult(true, out result);
                    }
                }

                if (we.Status == WebExceptionStatus.Timeout ||
                    we.Status == WebExceptionStatus.RequestCanceled ||
                    we.Status == WebExceptionStatus.ConnectionClosed ||
                    we.Status == WebExceptionStatus.ConnectFailure)
                {
                    return this.CreateExceptionHandlingResult(false, out result);
                }
            }

            return this.CreateExceptionHandlingResult(false, out result);
        }

        private bool CreateExceptionHandlingResult(bool isTransient, out ExceptionHandlingResult result)
        {
            result = new ExceptionHandlingRetryResult()
            {
                IsTransient = isTransient,
                RetryDelay = TimeSpan.FromMilliseconds(MaxRetryBackoffIntervalOnNonTransientErrors.TotalMilliseconds),
            };

            return true;
        }
    }
}