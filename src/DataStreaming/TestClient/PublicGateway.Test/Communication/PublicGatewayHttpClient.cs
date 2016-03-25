// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Test
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Model;
    using Common.Shared.Serializers;
    using Newtonsoft.Json;

    public class PublicGatewayHttpClient
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(PublicGatewayHttpClient));
        private HttpClient httpClient;

        public PublicGatewayHttpClient()
        {
            this.httpClient = new HttpClient();
            this.httpClient.DefaultRequestHeaders.Clear();
            this.httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<T> ExecuteGetAsync<T>(string url)
        {
            try
            {
                using (HttpResponseMessage rsp = await this.httpClient.GetAsync(url))
                {
                    byte[] readBytes = await rsp.Content.ReadAsByteArrayAsync();

                    if (!rsp.IsSuccessStatusCode)
                    {
                        Logger.Warning("Error={0}", rsp.IsSuccessStatusCode);

                        throw new ApplicationException(string.Format("SendJSON Failed IsSuccessStatusCode={0}", rsp.IsSuccessStatusCode));
                    }

                    // Deserialize the JSON received from server
                    IWsSerializer serializer = new JsonNetWsSerializer();
                    return await serializer.DeserializeAsync<T>(readBytes);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(PublicGatewayHttpClient));
                throw;
            }
        }

        public async Task<string> ExecutePostAsync(string url, PostProductModel postProductModel)
        {
            string result = "";
            try
            {
                using (StringContent theContent = new StringContent(
                    JsonConvert.SerializeObject(postProductModel),
                    System.Text.Encoding.UTF8,
                    "application/json"))
                {
                    using (HttpResponseMessage rsp = await this.httpClient.PostAsync(url, theContent))
                    {
                        result = await rsp.Content.ReadAsStringAsync();

                        if (!rsp.IsSuccessStatusCode)
                        {
                            Logger.Warning("Error={0} {1}", rsp.IsSuccessStatusCode, result);

                            throw new ApplicationException(string.Format("SendJSON Failed IsSuccessStatusCode={0}", rsp.IsSuccessStatusCode));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                result = result + " " + ex.ToString();
                Logger.Error(ex, nameof(PublicGatewayHttpClient));
            }

            return result;
        }
    }
}