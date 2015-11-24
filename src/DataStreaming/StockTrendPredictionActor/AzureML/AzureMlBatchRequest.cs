// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor.AzureML
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Now - 1 month
    /// </summary>
    public class AzureMlBatchRequest
    {
        public AzureMlBatchRequest(IEnumerable<ProductStockTrend> trends)
        {
            this.Trends = trends.ToList();
        }

        public List<ProductStockTrend> Trends { get; set; }
    }
}