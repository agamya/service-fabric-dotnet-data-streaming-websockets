// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway
{
    using Microsoft.AspNet.SignalR;

    internal class SignalRUserIdProvider : IUserIdProvider
    {
        public string GetUserId(IRequest request)
        {
            return "user1";
        }
    }
}