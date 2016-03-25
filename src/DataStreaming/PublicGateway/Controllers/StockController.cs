// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Controllers
{
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Common.Logging;
    using Common.Model;
    using Common.Shared;
    using StockService.Interfaces;

    public class StockController : ApiController
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(StockController));

        [HttpPost]
        public async Task<HttpResponseMessage> AddStock(PostStockModel stockUpdate)
        {
            Logger.Debug(nameof(this.AddStock));

            IStockService stockService = ConnectionFactory.CreateStockService(stockUpdate.ProductId);
            await stockService.AddStock(stockUpdate.ProductId, stockUpdate.Quantity);
            return this.Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        public Task<Product> Get(int id)
        {
            Logger.Debug(nameof(this.Get));

            IStockService stockService = ConnectionFactory.CreateStockService(id);

            return stockService.GetProduct(id);
        }

        [HttpGet]
        public Task<List<Product>> GetTopProductsOutOfStock()
        {
            Logger.Debug(nameof(this.GetTopProductsOutOfStock));

            return Task.FromResult<List<Product>>(null);
        }
    }
}