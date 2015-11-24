// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Shared.Serializers
{
    using System.IO;
    using System.Runtime.Serialization;
    using System.Threading.Tasks;

    public class DataContractWsSerializer : IWsSerializer
    {
        public Task<byte[]> SerializeAsync<T>(T value)
        {
            DataContractSerializer dcs = new DataContractSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream())
            {
                dcs.WriteObject(ms, value);
                ms.Position = 0;
                return Task.FromResult(ms.ToArray());
            }
        }

        public Task<T> DeserializeAsync<T>(byte[] serialized)
        {
            return this.DeserializeAsync<T>(serialized, 0, serialized.Length);
        }

        public Task<T> DeserializeAsync<T>(byte[] serialized, int offset, int length)
        {
            DataContractSerializer dcs = new DataContractSerializer(typeof(T));
            using (MemoryStream ms = new MemoryStream(serialized, offset, length))
            {
                return Task.FromResult((T) dcs.ReadObject(ms));
            }
        }
    }
}