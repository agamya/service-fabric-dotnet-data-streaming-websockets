// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace StockTrendPredictionActor.AzureML
{
    using System.Collections.Generic;

    public class AzureMlBatchResponse
    {
        public AzureMlBatchResponse()
        {
            this.Lines = new List<ResponseLine>();
        }

        public List<ResponseLine> Lines { get; set; }

        public class ResponseLine
        {
            public int ProductId { get; set; }

            public bool Reorder { get; set; }

            public float Probability { get; set; }

            public bool DefinitelyReorder => this.Reorder && this.Probability > 50.0;
        }
    }
}