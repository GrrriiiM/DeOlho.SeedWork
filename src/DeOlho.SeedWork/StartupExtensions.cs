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

        public static IServiceCollection AddSeedWork(
            this IServiceCollection services, 
            DeOlhoDbContextConfiguration deOlhoDbContextConfiguration)
        {
            services.AddSingleton(deOlhoDbContextConfiguration);

            services.AddDbContext<DeOlhoDbContext>();

            //services.AddScoped<IMapper, Mapper>();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = Assembly.GetEntryAssembly().GetName().Name, Version = $"v{Assembly.GetEntryAssembly().GetName().Version.Major}" });
            });

            services.AddHttpClient("deolho").AddRetryPolicy();

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

            return app;
        }

    }
}