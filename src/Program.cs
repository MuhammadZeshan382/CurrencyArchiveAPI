using CurrencyArchiveAPI.Extensions;
using CurrencyArchiveAPI.Middleware;

namespace CurrencyArchiveAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddCurrencyServices();

            // Configure API versioning
            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;
                options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
                    new Asp.Versioning.UrlSegmentApiVersionReader()
                );
            }).AddMvc();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new()
                {
                    Title = "Currency Archive API",
                    Version = "v1",
                    Description = "High-performance REST API providing historical currency exchange rates from 1999-2025. " +
                                  "All data (~9,500 days, 170+ currencies) is loaded into memory for instant access. " +
                                  "Supports flexible base currency conversion and symbol filtering."
                });

                // Apply external API documentation from JSON file
                options.OperationFilter<CurrencyArchiveAPI.Documentation.ApiDocumentationFilter>();

                // Include XML comments for Swagger documentation
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            // Enable Swagger in all environments (development and production)
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency Archive API v1");
                options.RoutePrefix = string.Empty; // Set Swagger UI at root (/)
            });

            // Global exception handling (must be first in pipeline)
            app.UseGlobalExceptionHandler();

            app.UseHttpsRedirection();

            // Load currency data before processing requests
            app.UseCurrencyDataLoader();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
