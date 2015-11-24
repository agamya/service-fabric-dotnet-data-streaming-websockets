// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Shared.Serializers
{
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    public class JsonNetWsSerializer : IWsSerializer
    {
        public Task<byte[]> SerializeAsync<T>(T value)
        {
            return Task.FromResult(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)));
        }

        public Task<T> DeserializeAsync<T>(byte[] serialized)
        {
            return this.DeserializeAsync<T>(serialized, 0, serialized.Length);
        }

        public Task<T> DeserializeAsync<T>(byte[] serialized, int offset, int length)
        {
            return Task.FromResult(JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(serialized, offset, length)));
        }
    }
}