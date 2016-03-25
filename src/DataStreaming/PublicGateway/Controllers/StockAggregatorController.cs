// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Controllers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Common.Logging;
    using Common.Model;
    using Common.Shared;
    using StockAggregatorService.Interfaces;

    public class StockAggregatorController : ApiController
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(StockAggregatorController));

        [HttpPost]
        public async Task<IEnumerable<ProductStockPrediction>> GetProducts()
        {
            Logger.Debug(nameof(this.GetProducts));

            //Note assuming single partition for aggregator service so passing in product id
            IStockAggregatorService stockAggregatorService = ConnectionFactory.CreateStockAggregatorService(699);
            return await stockAggregatorService.GetAllProducts();
        }

        [HttpPost]
        public async Task<IEnumerable<ProductStockPrediction>> GetHighProbabilityProducts(float probability)
        {
            Logger.Debug(nameof(this.GetHighProbabilityProducts));

            //Note assuming single partition for aggregator service so passing in product id
            IStockAggregatorService stockAggregatorService = ConnectionFactory.CreateStockAggregatorService(699);
            return await stockAggregatorService.GetProducts(probability);
        }
    }
}