using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using DotNetEnv;
using System.IO;

namespace ContosoHotels
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Load environment variables from .env file if it exists
            var envFile = Path.Combine(Directory.GetCurrentDirectory(), ".env");
            if (File.Exists(envFile))
            {
                Env.Load(envFile);
                System.Console.WriteLine($"Loaded environment variables from {envFile}");
            }
            else
            {
                System.Console.WriteLine($"No .env file found at {envFile}");
            }

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
