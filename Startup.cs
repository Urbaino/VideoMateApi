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

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions{
                OnPrepareResponse = ctx => {
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
                    ctx.Context.Response.Headers.Append("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
                },
                //FileProvider = new PhysicalFileProvider("C:\\Users\\o_sra_000\\dev\\videomate\\storage\\hdd"),RequestPath = "/storage"
                FileProvider = new PhysicalFileProvider("G:\\videomate\\hdd"),RequestPath = "/storage"
                 /* "DataPath" : "C:\\Users\\o_sra_000\\dev\\videomate\\storage\\hdd",
                    "MemoryCardPath" : "C:\\Users\\o_sra_000\\dev\\videomate\\storage\\sd",*/
                
            });
            app.UseCors("AllowAll");
            app.UseMvc();
        }
    }
}
