// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Model
{
    using ProtoBuf;

    [ProtoContract]
    public class MsgSpec
    {
        [ProtoMember(1)] public string Operation;

        [ProtoMember(2)] public long Key;

        [ProtoMember(3)] public byte[] Value;
    }
}