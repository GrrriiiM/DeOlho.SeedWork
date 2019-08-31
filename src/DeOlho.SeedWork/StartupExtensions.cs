using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Polly;
using Polly.Extensions.Http;
using DeOlho.SeedWork.Infrastructure.Data;
using Swashbuckle.AspNetCore.Swagger;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using AutoMapper;
using DeOlho.SeedWork.Domain.Abstractions;
using DeOlho.EventBus;
using DeOlho.EventBus.RabbitMQ.DependencyInjection;
using MediatR;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using HealthChecks.UI.Client;

namespace DeOlho.SeedWork
{
    public static class StartupExtensions
    {
        public static IApplicationBuilder UseMigrate(this IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope())
            {
                using (var deOlhoDbContext = serviceScope.ServiceProvider.GetService<DeOlhoDbContext>())
                {
                    deOlhoDbContext.Database.Migrate();
                }
            }

            return app;
        }

        public static IHttpClientBuilder AddRetryPolicy(this IHttpClientBuilder value)
        {
            return value.AddPolicyHandler((_) =>
                HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .OrResult(msg => (int)msg.StatusCode == 429) //System.Net.HttpStatusCode.TooManyRequests)
                    .WaitAndRetryAsync(new TimeSpan[] {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10)
                    }));
        }

        public static IServiceCollection AddSeedWork<TStartup>(
            this IServiceCollection services, 
            IConfiguration configuration,
            SeedWorkConfiguration seedWorkConfiguration = null)
        {
            var startupAssembly = typeof(TStartup).Assembly;

            seedWorkConfiguration = seedWorkConfiguration ?? new SeedWorkConfiguration();

            services.AddMediatR(startupAssembly);

            var deOlhoDbContextConfiguration = new DeOlhoDbContextConfiguration(
                configuration.GetConnectionString(seedWorkConfiguration.DeOlhoContextConnectionString),
                startupAssembly);

            services.AddSingleton(deOlhoDbContextConfiguration);

            var eventBusConfig = new EventBusRabbitMQDependencyInjectionConfiguration();
            eventBusConfig.Configuration(configuration.GetSection(seedWorkConfiguration.EventBusSectionName));
            eventBusConfig.SubscribeMediatorConsumers(startupAssembly);

            services.AddEventBusRabbitMQ(eventBusConfig);

            services.AddDbContext<DeOlhoDbContext>();

            services.AddScoped<IUnitOfWork>(sp => sp.GetService<DeOlhoDbContext>());

            //services.AddScoped<IMapper, Mapper>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = Assembly.GetEntryAssembly().GetName().Name, Version = $"v{Assembly.GetEntryAssembly().GetName().Version.Major}" });
            });

            services.AddHttpClient("deolho").AddRetryPolicy();

            services.AddHealthChecks()
                .AddMySql(deOlhoDbContextConfiguration.ConnectionString, "DeOlho Database")
                .AddRabbitMQ(string.Format("amqp://{0}:{1}@{2}:{3}/{4}",
                    eventBusConfig.UserName,
                    eventBusConfig.Password,
                    eventBusConfig.HostName,
                    eventBusConfig.Port,
                    eventBusConfig.VirtualHost),
                    name: "DeOlho Message Queue");

            return services;
        }

        public static IApplicationBuilder UseSeedWork(this IApplicationBuilder app)
        {
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint($"/swagger/v{Assembly.GetEntryAssembly().GetName().Version.Major}/swagger.json", Assembly.GetEntryAssembly().GetName().Name);
                c.RoutePrefix = string.Empty;
            });

            app.UseMigrate();

            app.UseHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter =  UIResponseWriter.WriteHealthCheckUIResponse
            });

            return app;
        }

    }
}