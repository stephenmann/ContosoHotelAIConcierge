using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using ContosoHotels.Data;
using ContosoHotels.Services;
using ContosoHotels.Configuration;
using Microsoft.SemanticKernel;
using System;
using System.ComponentModel.DataAnnotations;

namespace ContosoHotels
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configure DbContext with appropriate connection string
            services.AddDbContext<ContosoHotelsContext>(options =>
            {
                var connectionString = Configuration.GetConnectionString("DefaultConnection");
                
                // If in development and SQL_PASSWORD environment variable exists, use it to replace placeholder
                if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
                {
                    var sqlPassword = Environment.GetEnvironmentVariable("SQL_PASSWORD");
                    if (!string.IsNullOrEmpty(sqlPassword))
                    {
                        connectionString = connectionString.Replace("${SQL_PASSWORD}", sqlPassword);
                    }
                }
                
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
            });

            // Configure AI Concierge settings
            var aiConfig = Configuration.GetSection(AIConciergeConfiguration.SectionName)
                .Get<AIConciergeConfiguration>();
            
            if (aiConfig != null)
            {
                services.Configure<AIConciergeConfiguration>(
                    Configuration.GetSection(AIConciergeConfiguration.SectionName));
                
                // Helper method to resolve environment variables
                string ResolveEnvironmentVariable(string value)
                {
                    if (value.StartsWith("${") && value.EndsWith("}"))
                    {
                        var envVarName = value.Substring(2, value.Length - 3);
                        return Environment.GetEnvironmentVariable(envVarName);
                    }
                    return value;
                }
                
                // Try Azure AI first (preferred), then fallback to OpenAI
                bool configuredSuccessfully = false;
                
                // Attempt Azure AI configuration
                var azureApiKey = ResolveEnvironmentVariable(aiConfig.Azure.ApiKey);
                var azureEndpoint = ResolveEnvironmentVariable(aiConfig.Azure.Endpoint);
                
                if (!string.IsNullOrEmpty(azureApiKey) && !string.IsNullOrEmpty(azureEndpoint))
                {
                    try
                    {
                        // Configure Semantic Kernel with Azure OpenAI
                        services.AddScoped<Kernel>(serviceProvider =>
                        {
                            var builder = Kernel.CreateBuilder();
                            builder.AddAzureOpenAIChatCompletion(
                                deploymentName: aiConfig.Azure.ChatDeploymentName,
                                endpoint: azureEndpoint,
                                apiKey: azureApiKey);
                            return builder.Build();
                        });
                        
                        configuredSuccessfully = true;
                        Console.WriteLine("AI Concierge configured with Azure AI Foundry endpoints.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Failed to configure Azure AI: {ex.Message}. Attempting OpenAI fallback...");
                    }
                }
                
                // Fallback to OpenAI if Azure configuration failed
                if (!configuredSuccessfully)
                {
                    var openAiApiKey = ResolveEnvironmentVariable(aiConfig.OpenAI.ApiKey);
                    
                    if (!string.IsNullOrEmpty(openAiApiKey))
                    {
                        try
                        {
                            // Validate configuration
                            var validationContext = new ValidationContext(aiConfig);
                            Validator.ValidateObject(aiConfig, validationContext, validateAllProperties: true);
                            
                            // Configure Semantic Kernel with OpenAI
                            services.AddScoped<Kernel>(serviceProvider =>
                            {
                                var builder = Kernel.CreateBuilder();
                                builder.AddOpenAIChatCompletion(
                                    modelId: aiConfig.OpenAI.ChatModel,
                                    apiKey: openAiApiKey);
                                return builder.Build();
                            });
                            
                            configuredSuccessfully = true;
                            Console.WriteLine("AI Concierge configured with OpenAI endpoints.");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Warning: Failed to configure OpenAI: {ex.Message}. AI features will be disabled.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Warning: No API keys found for Azure AI or OpenAI. AI features will be disabled.");
                    }
                }
            }

            // Add SignalR for real-time chat
            services.AddSignalR();

            // Register AI Concierge Service
            services.AddScoped<AIConciergeService>();

            services.AddScoped<DataSeedingService>();

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DataSeedingService seedingService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                
                // Seed the database in development
                seedingService.SeedDataAsync().Wait();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                
                // Add SignalR hub for chat functionality
                endpoints.MapHub<ChatHub>("/chathub");
            });
        }
    }
}
