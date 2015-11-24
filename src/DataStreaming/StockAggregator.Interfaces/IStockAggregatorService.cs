// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockAggregatorService.Interfaces
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Common.Model;
    using Microsoft.ServiceFabric.Services.Remoting;

    /// <summary>
    /// Primary communication interface between to the Stock Aggregator Service
    /// </summary>
    public interface IStockAggregatorService : IService
    {
        /// <summary>
        /// Gets all products that should be reordered
        /// </summary>
        Task<IEnumerable<ProductStockPrediction>> GetProducts(float probability);

        Task<IEnumerable<ProductStockPrediction>> GetAllProducts();
    }
}