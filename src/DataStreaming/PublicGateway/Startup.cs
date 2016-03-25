// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace PublicGateway
{
    using System.Web.Http;
    using Microsoft.AspNet.SignalR;
    using Microsoft.Owin;
    using Microsoft.Owin.Cors;
    using Microsoft.Owin.FileSystems;
    using Microsoft.Owin.StaticFiles;
    using Owin;

    public class Startup : IOwinAppBuilder
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            HttpConfiguration config = new HttpConfiguration();

            PhysicalFileSystem physicalFileSystem = new PhysicalFileSystem(@".\wwwroot");
            FileServerOptions fileOptions = new FileServerOptions();

            fileOptions.EnableDefaultFiles = true;
            fileOptions.RequestPath = PathString.Empty;
            fileOptions.FileSystem = physicalFileSystem;
            fileOptions.DefaultFilesOptions.DefaultFileNames = new[] {"Default.html"};
            fileOptions.StaticFileOptions.FileSystem = fileOptions.FileSystem = physicalFileSystem;
            fileOptions.StaticFileOptions.ServeUnknownFileTypes = true;

            FormatterConfig.ConfigureFormatters(config.Formatters);
            RouteConfig.RegisterRoutes(config);

            appBuilder.UseWebApi(config);
            appBuilder.UseFileServer(fileOptions);

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