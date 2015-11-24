// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Controllers
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Common.Logging;
    using Common.Model;
    using Common.Shared;
    using Common.Shared.Serializers;
    using Microsoft.ServiceFabric.Data;
    using StockService.Interfaces;

    /// <summary>
    /// This is the entry point for the clients of the PublicGateway
    /// It does stock reservation over web sockets
    /// </summary>
    public class ReserveStockWebSocketController
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger(nameof(ReserveStockWebSocketController));

        public ReserveStockWebSocketController(IReliableStateManager sm)
        {
        }

        public static async Task<MsgSpec> ProcessOperation(IReliableStateManager sm, MsgSpec mreq, CancellationToken cancellationToken)
        {
            MsgSpec mresp = new MsgSpec()
            {
                Operation = null,
                Key = MsgSpecKeys.Default,
                Value = null
            };

            // Process Operation for the Contact
            ReserveStockWebSocketController reserveStockWebSocketController = new ReserveStockWebSocketController(null);
            switch (mreq.Operation.ToLower())
            {
                case WSOperations.AddItem:
                    IWsSerializer serializer = SerializerFactory.CreateSerializer();
                    PostProductModel postProductModel = await serializer.DeserializeAsync<PostProductModel>(mreq.Value);
                    bool result = await reserveStockWebSocketController.ReserveProduct(postProductModel);
                    mresp.Value = Encoding.UTF8.GetBytes(result ? "Success" : "Failed");
                    break;
            }

            return mresp;
        }

        public async Task<bool> ReserveProduct(PostProductModel model)
        {
            Logger.Debug(nameof(this.ReserveProduct));
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            try
            {
                IStockService stockService = ConnectionFactory.CreateStockService(model.ProductId);

                // stockLeft == -1 means reserving stock failed
                int stockLeft = await stockService.PurchaseProduct(model.ProductId, model.Quantity);

                if (stockLeft == -1)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, nameof(this.ReserveProduct));
                return false;
            }
        }
    }
}