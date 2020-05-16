using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PokerBackend.Hubs;
using PokerBackend.Services;

namespace PokerBackend
{
    public class Startup
    {
        private const string MyCorsPolicy = "MyCorsPolicy";
        
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options =>
            {
                options.RespectBrowserAcceptHeader = true;
            });

            services.AddCors(options =>
            {
                options.AddPolicy(name: MyCorsPolicy,
                                  builder =>
                                  {
                                      builder
                                        .WithOrigins(Configuration.GetValue<string>("AllowedOrigin"))
                                        .AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .AllowCredentials();
                                  });
            });

            services.AddSignalR().AddAzureSignalR();
            services.AddSingleton(new PokerService());
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

            app.UseCors(MyCorsPolicy);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<PokerHub>("/poker-hub");
            });
        }
    }
}
