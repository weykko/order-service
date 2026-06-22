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

    private static readonly string[] FrontendOrigins =
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

        services.AddCors(options => options.AddPolicy(CorsPolicyName, policy =>
            policy.WithOrigins(FrontendOrigins)
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
