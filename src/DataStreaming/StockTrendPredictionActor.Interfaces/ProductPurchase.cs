// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor.Interfaces
{
    using System;
    using System.Runtime.Serialization;

    [DataContract]
    public class ProductPurchase
    {
        [DataMember]
        public string PurchaseKey { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }

        [DataMember]
        public int ProductId { get; set; }

        [DataMember]
        public string ProductName { get; set; }

        [DataMember]
        public int StockLeft { get; set; }

        [DataMember]
        public int Quantity { get; set; }
    }
}