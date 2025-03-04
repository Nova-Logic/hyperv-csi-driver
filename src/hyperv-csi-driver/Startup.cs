﻿using System.Linq;
using HypervCsiDriver.Hosting;
using HypervCsiDriver.Infrastructure;
using HypervCsiDriver.Infrastructure.Hyperv.Node;
using HypervCsiDriver.Services;
using HypervCsiDriver.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HypervCsiDriver
{
    public sealed class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions<HypervCsiDriverOptions>()
                .Bind(Configuration.GetSection("Driver"))
                .PostConfigure(opt =>
                {
                    /*switch (opt.Type)
                    {
                        case HypervCsiDriverType.Controller:
                            //load hyperv host from kvp 
                            if (string.IsNullOrEmpty(opt.HostName))
                            {
                                var (_,value) = HypervUtils.ReadKvpPoolAsync()
                                    .FirstOrDefaultAsync(n => n.Name == "PhysicalHostNameFullyQualified")
                                    .Result;

                                if (!string.IsNullOrEmpty(value))
                                    opt.HostName = value;
                                if (string.IsNullOrEmpty(opt.UserName))
                                    opt.UserName = "Administrator"; //aka windows root
                            }
                            break;
                        
                    }*/
                    // Here we actually need to read the kvp pool not only on Controller but also on Node driver type
                    // since we need to know the hostname of the hyperv host to connect to
                    if (!string.IsNullOrEmpty(opt.HostName)) return;
                    var (_,value) = HypervUtils.ReadKvpPoolAsync()
                        .FirstOrDefaultAsync(n => n.Name == "PhysicalHostNameFullyQualified")
                        .Result;
                    if (!string.IsNullOrEmpty(value))
                        opt.HostName = value;
                    if (opt.Type == HypervCsiDriverType.Controller && string.IsNullOrEmpty(opt.UserName))
                        opt.UserName = "Administrator"; //aka windows root
                })
                .Validate(opt =>
                {
                    switch (opt.Type)
                    {
                        case HypervCsiDriverType.Controller:
                            return !string.IsNullOrEmpty(opt.HostName);
                        case HypervCsiDriverType.Node:
                            return !string.IsNullOrEmpty(opt.HostName);
                        default:
                            return false;
                    }
                });


            var driverType = Configuration.GetValue<HypervCsiDriverType>("Driver:Type");
            switch(driverType)
            {
                case HypervCsiDriverType.Controller:
                    services.AddSingleton<IHypervVolumeService, HypervVolumeService>();
                    break;
                case HypervCsiDriverType.Node:
                    services.AddSingleton<IHypervNodeService, LinuxNodeService>();
                    break;
            }

            services.AddGrpc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IOptions<HypervCsiDriverOptions> driverOptions)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGrpcService<HypervCsiIdentity>();
                
                switch(driverOptions.Value.Type)
                {
                    case HypervCsiDriverType.Controller:
                        endpoints.MapGrpcService<HypervCsiController>();
                        break;
                    case HypervCsiDriverType.Node:
                        endpoints.MapGrpcService<HypervCsiNode>();
                        break;
                }
                
                endpoints.MapGet("/", async context =>
                {
                    //that's why there is CsiClientProject
                    //or you can use postman/rider/some another IDE with gRPC plugin
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
            });
        }
    }
}
