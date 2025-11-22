
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
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new()
                {
                    Title = "Currency Archive API",
                    Version = "v1",
                    Description = "High-performance API for historical currency exchange rates (1999-2025)"
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Global exception handling
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
