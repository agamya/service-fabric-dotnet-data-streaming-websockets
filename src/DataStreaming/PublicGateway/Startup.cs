// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway
{
    using System.Web.Http;
    using Microsoft.AspNet.SignalR;
    using Microsoft.Owin.Cors;
    using Owin;

    public class Startup : IOwinAppBuilder
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();

            FormatterConfig.ConfigureFormatters(config.Formatters);
            RouteConfig.RegisterRoutes(config);

            appBuilder.UseWebApi(config);

            //CORS & SignalR
            appBuilder.UseCors(CorsOptions.AllowAll);
            HubConfiguration configR = new HubConfiguration();
            configR.EnableDetailedErrors = true;
            appBuilder.MapSignalR(configR);

            GlobalHost.DependencyResolver.Register(typeof(IUserIdProvider), () => new SignalRUserIdProvider());

            config.EnsureInitialized();
        }
    }
}