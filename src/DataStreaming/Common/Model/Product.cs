// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Model
{
    public class Product
    {
        public Product()
        {
        }

        public Product(int productId)
        {
            this.ProductId = productId;
        }

        public int ProductId { get; set; }

        public int CategoryId { get; set; }

        public string ProductNumber { get; set; }

        public string ProductName { get; set; }

        public string ModelName { get; set; }

        public decimal StandardCost { get; set; }

        public decimal ListPrice { get; set; }

        /// <summary>
        /// Total amount of stock reserved but not purchased yet (in the basket)
        /// </summary>
        public int StockReserved { get; set; }

        /// <summary>
        /// Total amount of stock available for adding to the basket
        /// </summary>
        public int StockTotal { get; set; }

        /// <summary>
        /// Probability (0-1) coefficient that this item is running out of stock
        /// </summary>
        public float OutOfStockProbability { get; set; }
    }
}