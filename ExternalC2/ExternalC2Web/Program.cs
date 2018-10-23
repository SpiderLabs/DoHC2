using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace ExternalC2Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("Usage: dotnet run --url http://*:80/");
                return;
            }
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args)
        {
            return WebHost.CreateDefaultBuilder(args)
                .UseUrls(args[1])
                .UseStartup<Startup>()
                .Build();
        }
    }
}