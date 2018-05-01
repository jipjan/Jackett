﻿using Autofac;
using Autofac.Extensions.DependencyInjection;
using Jackett.Common.Models.Config;
using Jackett.Common.Plumbing;
using Jackett.Common.Services.Interfaces;
using Jackett.Server.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json.Serialization;
using System;
using System.Text;

namespace Jackett.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .AddJsonOptions(options => options.SerializerSettings.ContractResolver = new DefaultContractResolver()); //Web app uses Pascal Case JSON

            RuntimeSettings runtimeSettings = new RuntimeSettings();
            Configuration.GetSection("RuntimeSettings").Bind(runtimeSettings);

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var builder = new ContainerBuilder();

            Initialisation.SetupLogging(runtimeSettings, builder);

            builder.Populate(services);
            builder.RegisterModule(new JackettModule(runtimeSettings));
            builder.RegisterType<SecuityService>().As<ISecuityService>();
            builder.RegisterType<ServerService>().As<IServerService>();
            builder.RegisterType<ProtectionService>().As<IProtectionService>();

            IContainer container = builder.Build();
            Initialisation.ApplicationContainer = container;

            return new AutofacServiceProvider(container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            Initialisation.Initialize();

            app.UseDeveloperExceptionPage();

            app.UseFileServer(new FileServerOptions
            {
                FileProvider = new PhysicalFileProvider(Initialisation.ConfigService.GetContentFolder()),
                RequestPath = "",
                EnableDefaultFiles = true,
                EnableDirectoryBrowsing = false
            });

            app.UseMvc();
        }
    }
}
