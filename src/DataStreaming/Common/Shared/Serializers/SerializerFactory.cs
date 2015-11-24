// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Shared.Serializers
{
    using System;

    public static class SerializerFactory
    {
        // Set JsonNet as the default serializer
        private static Func<IWsSerializer> DefaultFactory = () => new ProtobufWsSerializer();

        public static void SetFactory<T>() where T : IWsSerializer, new()
        {
            DefaultFactory = () => new T();
        }

        public static IWsSerializer CreateSerializer()
        {
            return DefaultFactory();
        }
    }
}