using System;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using ExternalC2.Channels;
using ExternalC2Web.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExternalC2Web
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json")
                .AddJsonFile("socketsettings.json", true, true);

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.Configure<SocketSettings>(Configuration);
            services.AddSingleton<ChannelManager<SocketChannel>>();
            services.AddWebSocketHandlers();
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IServiceProvider serviceProvider)
        {
            app.UseWebSockets();
            app.MapWebSocketManager("/bws", serviceProvider.GetService<C2WebSocketHandler>());
            app.UseMvc();
        }
    }

    public static class StartupExtensions
    {
        public static IApplicationBuilder MapWebSocketManager(this IApplicationBuilder app, PathString path,
            WebSocketHandler handler)
        {
            return app.Map(path, a => a.UseMiddleware<WebSocketManagerMiddleware>(handler));
        }

        public static IServiceCollection AddWebSocketHandlers(this IServiceCollection services)
        {
            services.AddTransient<ChannelManager<WebSocket>>();

            Assembly.GetEntryAssembly().ExportedTypes
                .Where(type => type.GetTypeInfo().BaseType == typeof(WebSocketHandler))
                .ToList()
                .ForEach(type => services.AddSingleton(type));

            return services;
        }
    }
}