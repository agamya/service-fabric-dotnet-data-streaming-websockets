// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway
{
    using System.Web.Http;

    public static class RouteConfig
    {
        public static void RegisterRoutes(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "StockAggregatorApiGetProducts",
                routeTemplate: "stockaggregator/GetProducts",
                defaults: new {controller = "StockAggregator", action = "GetProducts"}
                );

            config.Routes.MapHttpRoute(
                name: "StockAggregatorApiGetHighProbabilityProducts",
                routeTemplate: "stockaggregator/GetHighProbabilityProducts/{probability}",
                defaults: new {controller = "StockAggregator", action = "GetHighProbabilityProducts", probability = RouteParameter.Optional}
                );

            config.Routes.MapHttpRoute(
                name: "StockAggregator",
                routeTemplate: "stockaggregator",
                defaults: new {controller = "StockAggregator", action = "Index"},
                constraints: new {}
                );

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new {id = RouteParameter.Optional}
                );
        }
    }
}