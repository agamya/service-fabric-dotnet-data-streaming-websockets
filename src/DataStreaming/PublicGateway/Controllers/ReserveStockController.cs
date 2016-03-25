// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Controllers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Common.Logging;
    using Common.Model;
    using Common.Shared;
    using StockService.Interfaces;

    /// <summary>
    /// This is the entry point for the clients of the PublicGateway
    /// It does stock reservation over REST HTTP 
    /// </summary>
    public class ReserveStockController : ApiController
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(ReserveStockController));

        [HttpPost]
        public async Task<HttpResponseMessage> ReserveProduct(PostProductModel model)
        {
            Logger.Debug(nameof(this.ReserveProduct));
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            try
            {
                IStockService stockService = ConnectionFactory.CreateStockService(model.ProductId);

                // stockLeft == -1 means reserving stock failed
                int stockLeft = await stockService.PurchaseProduct(model.ProductId, model.Quantity);

                if (stockLeft == -1)
                {
                    return this.Request.CreateResponse<string>(HttpStatusCode.NoContent, string.Format("Failure: not enough stock left={0}", stockLeft));
                }

                return this.Request.CreateResponse<string>(HttpStatusCode.OK, string.Format("Success: stock left={0}", stockLeft));
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(this.ReserveProduct));
                return this.Request.CreateResponse<string>(HttpStatusCode.NoContent, ex.ToString());
            }
        }
    }
}