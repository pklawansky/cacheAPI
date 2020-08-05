using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheAPI.BL;
using CacheAPI.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
//using Utf8Json.Resolvers;

namespace CacheAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            //services.AddControllersWithViews();

            services.AddMemoryCache((m) =>
            {
                
            });

            services.Configure<IISServerOptions>(options =>
            {
                options.AutomaticAuthentication = false;
            });

            services.AddWebSockets((options) =>
            {

            });

            var configurationBL = new ConfigurationBL(Configuration);
            GlobalSettings.WebSocketPort = configurationBL.WebSocketPort;
            GlobalSettings.SemaphoreInitial = configurationBL.SemaphoreInitial;
            GlobalSettings.DefaultCacheExpirationSeconds = configurationBL.DefaultCacheExpirationSeconds;
            GlobalSettings.AutoPopulateEndpoints = configurationBL.AutoPopulateEndpoints;
            GlobalSettings.PersistCacheToFile = configurationBL.PersistCacheToFile;
            GlobalSettings.PersistentDataFileName = configurationBL.PersistentDataFileName;

            var t = new System.Threading.Thread(() =>
            {
                SynchronousSocketListener.StartListening();
            })
            { IsBackground = true };
            t.Start();

            //services.AddMvc(option =>
            // {
            //     option.OutputFormatters.Clear();
            //     option.OutputFormatters.Add(new Utf8JsonOutputFormatter(StandardResolver.Default));
            //     option.InputFormatters.Clear();
            //     option.InputFormatters.Add(new Utf8JsonInputFormatter());
            // });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }


            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
