// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Model
{
    using ProtoBuf;

    [ProtoContract]
    public class PostProductModel
    {
        [ProtoMember(1)]
        public int ProductId { get; set; }

        [ProtoMember(2)]
        public int Quantity { get; set; }
    }
}