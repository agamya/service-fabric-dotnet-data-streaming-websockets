// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor.AzureML
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class MockedAzureMlClient : IAzureMlClient
    {
        private readonly Random random;

        public MockedAzureMlClient()
        {
            this.random = new Random();
        }

        public Task<AzureMlBatchResponse> InvokeBatchAsync(AzureMlBatchRequest request)
        {
            return Task.FromResult(
                new AzureMlBatchResponse()
                {
                    Lines = request.Trends
                        .Select(
                            x => new AzureMlBatchResponse.ResponseLine()
                            {
                                ProductId = x.ProductId,
                                Reorder = x.LastStockCount < 150,
                                Probability = (float) this.random.NextDouble()
                            })
                        .ToList()
                });
        }
    }
}