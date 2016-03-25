// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Model
{
    using ProtoBuf;

    [ProtoContract]
    public class WsResponseMessage
    {
        [ProtoMember(1)] public int Result;
        [ProtoMember(2)] public byte[] Value;
    }
}