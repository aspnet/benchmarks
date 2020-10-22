﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Crank.EventSources;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Proxy
{
    public class Program
    {
        private static HttpMessageInvoker _httpMessageInvoker;

        private static string _scheme;
        private static HostString _host;
        private static string _pathBase;
        private static QueryString _appendQuery;

        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "ASPNETCORE_")
                .AddCommandLine(args)
                .Build();

            // The url all requests will be forwarded to
            var baseUriArg = config["baseUri"];

            if (String.IsNullOrWhiteSpace(baseUriArg))
            {
                throw new ArgumentException("--baseUri is required");
            }

            var baseUri = new Uri(baseUriArg);

            // Cache base URI values
            _scheme = baseUri.Scheme;
            _host = new HostString(baseUri.Authority);
            _pathBase = baseUri.AbsolutePath;
            _appendQuery = new QueryString(baseUri.Query);

            Console.WriteLine($"Base URI: {baseUriArg}");

            BenchmarksEventSource.MeasureAspNetVersion();
            BenchmarksEventSource.MeasureNetCoreAppVersion();

            var builder = new WebHostBuilder()
                .ConfigureLogging(loggerFactory =>
                {
                    // Don't enable console logging if no specific level is defined (perf)

                    if (Enum.TryParse(config["LogLevel"], out LogLevel logLevel))
                    {
                        Console.WriteLine($"Console Logging enabled with level '{logLevel}'");
                        loggerFactory.AddConsole().SetMinimumLevel(logLevel);
                    }
                })
                .UseKestrel((context, kestrelOptions) =>
                {
                    kestrelOptions.ConfigureHttpsDefaults(httpsOptions =>
                    {
                        httpsOptions.ServerCertificate = new X509Certificate2(Path.Combine(context.HostingEnvironment.ContentRootPath, "testCert.pfx"), "testPassword");
                    });
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseConfiguration(config)
                ;

            InitializeHttpClient();

            builder = builder.Configure(app => app.Run(ProxyRequest));


            builder
                .Build()
                .Run();
        }

        private static void InitializeHttpClient()
        {
            var httpHandler = new SocketsHttpHandler();

            httpHandler.AllowAutoRedirect = false;
            httpHandler.UseProxy = false;
            httpHandler.AutomaticDecompression = System.Net.DecompressionMethods.None;
            // Accept any SSL certificate
            httpHandler.SslOptions.RemoteCertificateValidationCallback += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) => true;

            _httpMessageInvoker = new HttpMessageInvoker(httpHandler);
        }

        private static async Task ProxyRequest(HttpContext context)
        {
            var destinationUri = BuildDestinationUri(context);

            using var requestMessage = context.CreateProxyHttpRequest(destinationUri);
            requestMessage.VersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            requestMessage.Version = new Version(2, 0);

            using var responseMessage = await _httpMessageInvoker.SendAsync(requestMessage, context.RequestAborted);
            await context.CopyProxyHttpResponse(responseMessage);
        }

        private static Uri BuildDestinationUri(HttpContext context) => new Uri(UriHelper.BuildAbsolute(_scheme, _host, _pathBase, context.Request.Path, context.Request.QueryString.Add(_appendQuery)));
    }
}
