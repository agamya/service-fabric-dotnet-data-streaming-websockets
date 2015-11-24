// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Shared.Serializers
{
    using System.IO;
    using System.Threading.Tasks;
    using ProtoBuf;

    public class ProtobufWsSerializer : IWsSerializer
    {
        public Task<byte[]> SerializeAsync<T>(T value)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Serialize(ms, value);
                return Task.FromResult(ms.ToArray());
            }
        }

        public Task<T> DeserializeAsync<T>(byte[] serialized)
        {
            return this.DeserializeAsync<T>(serialized, 0, serialized.Length);
        }

        public Task<T> DeserializeAsync<T>(byte[] serialized, int offset, int length)
        {
            using (MemoryStream ms = new MemoryStream(serialized, offset, length))
            {
                return Task.FromResult(Serializer.Deserialize<T>(ms));
            }
        }
    }
}