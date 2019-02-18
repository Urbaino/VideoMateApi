using System;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Http;

namespace VideoKategoriseringsApi
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container during development.
        public void ConfigureDevelopmentServices(IServiceCollection services)
        {
            services.Configure<KestrelServerOptions>(options =>
            {
                options.Listen(IPAddress.Any, 5000);
            });

            ConfigureServicesBase(services);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<KestrelServerOptions>(options =>
            {
                options.Listen(IPAddress.Any, 80);
            });

            ConfigureServicesBase(services);
        }

        public void ConfigureServicesBase(IServiceCollection services)
        {
            
             services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
            builder =>
            {
                builder
                .AllowAnyOrigin() 
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
            });
            });
            services.AddMvc();
            services.Configure<Settings>(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            String storageRootFolder = Configuration["DataPath"];
            if(storageRootFolder == null || storageRootFolder.Length == 0)
            {
                throw new Exception("Du har missat att ange DataPath i appsettings.json");
            }
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions{
                OnPrepareResponse = ctx => {
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
                },
                FileProvider = new PhysicalFileProvider(storageRootFolder),RequestPath = "/storage"        
            });
            app.UseCors("AllowAll");
            app.UseMvc();
        }
    }
}
