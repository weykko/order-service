using System.Text.Json;
using System.Text.Json.Serialization;
using OrderService.Application;
using OrderService.Infrastructure;
using OrderService.Presentation.Middleware;

namespace OrderService.Presentation;

/// <summary>
/// Конфигурация DI-контейнера и HTTP-конвейера системы заказов.
/// </summary>
public class Startup
{
    private const string CorsPolicyName = "AllowFrontend";

    // Origin'ы фронтенда по умолчанию (локальная разработка). Дополнительные
    // origin'ы задаются через конфиг "Cors:AllowedOrigins" (например, адрес сервера).
    private static readonly string[] DefaultFrontendOrigins =
    {
        "http://localhost:3000",
        "http://localhost:5173"
    };

    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        var allowedOrigins = _configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var origins = allowedOrigins is { Length: > 0 } ? allowedOrigins : DefaultFrontendOrigins;

        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
            policy.WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()));

        services.AddApplication();
        services.AddInfrastructure(_configuration);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (env.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseRouting();
        app.UseCors(CorsPolicyName);
        app.UseAuthorization();

        app.UseEndpoints(endpoints => endpoints.MapControllers());
    }
}
