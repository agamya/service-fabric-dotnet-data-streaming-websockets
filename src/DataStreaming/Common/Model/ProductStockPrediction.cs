// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Model
{
    using System.Runtime.Serialization;

    [DataContract]
    public class ProductStockPrediction
    {
        [DataMember] public bool Reorder;

        [DataMember]
        public int ProductId { get; set; }

        [DataMember]
        public string ProductName { get; set; }

        [DataMember]
        public int StockLeft { get; set; }

        [DataMember]
        public float Probability { get; set; }
    }
}