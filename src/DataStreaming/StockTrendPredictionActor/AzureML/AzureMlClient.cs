// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor.AzureML
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Common.Logging;
    using Newtonsoft.Json.Linq;

    public class AzureMlClient : IAzureMlClient
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(AzureMlClient));
        private readonly Uri serviceUri;
        private readonly string apiKey;

        public AzureMlClient(Uri serviceUri, string apiKey)
        {
            if (serviceUri == null)
            {
                throw new ArgumentNullException(nameof(serviceUri));
            }
            if (apiKey == null)
            {
                throw new ArgumentNullException(nameof(apiKey));
            }

            this.serviceUri = serviceUri;
            this.apiKey = apiKey;
        }

        public async Task<AzureMlBatchResponse> InvokeBatchAsync(AzureMlBatchRequest request)
        {
            using (HttpClient client = new HttpClient())
            {
                var scoreRequest = new
                {
                    Inputs = new Dictionary<string, StringTable>
                    {
                        {
                            "input1",
                            new StringTable
                            {
                                ColumnNames =
                                    new[]
                                    {
                                        "ProductId",
                                        "ConsumptionRate",
                                        "AverageTimeBetweenPurchases",
                                        "AveragePurchaseSize",
                                        "Reorder",
                                        "StockLevel",
                                        "TotalOrders"
                                    },
                                Values = request.Trends.Select(
                                    trend => new[]
                                    {
                                        trend.ProductId.ToString(),
                                        trend.AvgPurchasesPerDay.ToString(),
                                        trend.AvgTimeBetweenPurchases.ToString(),
                                        trend.AvgQuantityPerOrder.ToString(),
                                        "True",
                                        trend.LastStockCount.ToString(),
                                        trend.TotalPurchases.ToString()
                                    })
                                    .ToArray()
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>
                    {
                    }
                };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.apiKey);
                client.BaseAddress = this.serviceUri;

                HttpResponseMessage response = await client.PostAsJsonAsync("", scoreRequest);

                if (response.IsSuccessStatusCode)
                {
                    string resultString = await response.Content.ReadAsStringAsync();
                    JObject resultObject = JObject.Parse(resultString);

                    JToken arrValues = resultObject["Results"]["output1"]["value"]["Values"];
                    return new AzureMlBatchResponse()
                    {
                        Lines = arrValues.Select(
                            values => new AzureMlBatchResponse.ResponseLine()
                            {
                                ProductId = values[0].Value<int>(),
                                Reorder = values[7].Value<bool>(),
                                Probability = values[8].Value<float>()
                            }).ToList()
                    };
                }
                else
                {
                    Logger.Debug($"The request failed with status code: {response.StatusCode}");

                    // Print the headers - they include the requert ID and the timestamp, which are useful for debugging the failure
                    Logger.Debug(response.Headers.ToString());

                    string responseContent = await response.Content.ReadAsStringAsync();
                    Logger.Debug(responseContent);
                    throw new InvalidOperationException(responseContent);
                }
            }
        }

        private class StringTable
        {
            public string[] ColumnNames { get; set; }

            public string[][] Values { get; set; }
        }
    }
}