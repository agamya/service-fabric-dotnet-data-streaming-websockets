// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor.Interfaces
{
    using System.Collections.Generic;
    using Common.Model;
    using Microsoft.ServiceFabric.Actors;

    public interface IStockEvents : IActorEvents
    {
        void OnLowStockProducts(List<ProductStockPrediction> productStocks);
    }
}