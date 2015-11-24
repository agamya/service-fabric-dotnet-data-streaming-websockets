// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor
{
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    [DataContract]
    public class StockTrendPredictionActorState
    {
        [DataMember]
        public Dictionary<int, ProductStockTrend> ProductStockTrends;
    }
}