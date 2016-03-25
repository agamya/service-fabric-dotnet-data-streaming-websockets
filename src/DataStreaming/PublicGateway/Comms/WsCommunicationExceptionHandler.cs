// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Comms
{
    using System;
    using System.Net;
    using System.Net.Http;
    using Common.Logging;
    using Microsoft.ServiceFabric.Services.Communication.Client;

    public class WsCommunicationExceptionHandler : IExceptionHandler
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(WsCommunicationClientFactory));

        public bool TryHandleException(ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, out ExceptionHandlingResult result)
        {
            Exception e = exceptionInformation.Exception;

            Logger.Debug("OnHandleException {0}", e);

            if (e is TimeoutException)
            {
                result = new ExceptionHandlingRetryResult(e, false, retrySettings, retrySettings.DefaultMaxRetryCount);
                return true;
            }

            if (e is ProtocolViolationException)
            {
                result = new ExceptionHandlingRetryResult(e, false, retrySettings, retrySettings.DefaultMaxRetryCount);
                return true;
            }

            if (e is HttpRequestException)
            {
                HttpRequestException he = (HttpRequestException) e;

                // this should not happen, but let's do a sanity check
                if (null == he.InnerException)
                {
                    result = new ExceptionHandlingRetryResult(e, false, retrySettings, retrySettings.DefaultMaxRetryCount);
                    return true;
                }

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
                        result = new ExceptionHandlingRetryResult(e, false, retrySettings, retrySettings.DefaultMaxRetryCount);
                        return true;
                    }

                    if (errorResponse.StatusCode == HttpStatusCode.InternalServerError)
                    {
                        // The address is correct, but the server processing failed.
                        // This could be due to conflicts when writing the word to the dictionary.
                        // Retry the operation without re-resolving the address.
                        result = new ExceptionHandlingRetryResult(e, true, retrySettings, retrySettings.DefaultMaxRetryCount);
                        return true;
                    }
                }

                if (we.Status == WebExceptionStatus.Timeout ||
                    we.Status == WebExceptionStatus.RequestCanceled ||
                    we.Status == WebExceptionStatus.ConnectionClosed ||
                    we.Status == WebExceptionStatus.ConnectFailure)
                {
                    result = new ExceptionHandlingRetryResult(e, false, retrySettings, retrySettings.DefaultMaxRetryCount);
                    return true;
                }
            }

            result = new ExceptionHandlingRetryResult(e, false, retrySettings, retrySettings.DefaultMaxRetryCount);
            return true;
        }
    }
}