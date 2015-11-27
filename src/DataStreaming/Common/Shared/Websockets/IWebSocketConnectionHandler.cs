// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Common.Shared.Websockets
{
    using System.Threading;
    using System.Threading.Tasks;

    public interface IWebSocketConnectionHandler
    {
        Task<byte[]> ProcessWsMessageAsync(byte[] wsrequest, CancellationToken cancellationToken);
    }
}