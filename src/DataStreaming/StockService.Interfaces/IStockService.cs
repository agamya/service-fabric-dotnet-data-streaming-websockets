// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockService.Interfaces
{
    using System.Threading.Tasks;
    using Common.Model;
    using Microsoft.ServiceFabric.Services.Remoting;

    /// <summary>
    /// Primary communication interface between to the Stock Service
    /// </summary>
    public interface IStockService : IService
    {
        /// <summary>
        /// Get a single product by ID
        /// </summary>
        Task<Product> GetProduct(int productId);

        /// <summary>
        /// Reserve stock
        /// </summary>
        Task<int> PurchaseProduct(int productId, int quantity);

        /// <summary>
        /// Adds additional Stock for the specified product ID 
        /// </summary>
        Task<bool> AddStock(int productId, int quantity);
    }
}