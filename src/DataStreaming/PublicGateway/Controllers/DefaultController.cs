// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway.Controllers
{
    using System.Net.Http;
    using System.Web.Http;

    public class DefaultController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage Index()
        {
            return this.View("PublicGateway.wwwroot.Default.html", "text/html");
        }
    }
}