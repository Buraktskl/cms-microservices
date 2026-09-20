using ContentService.API.Middleware;
using ContentService.Infrastructure;
using ContentService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Docker/Kubernetes secrets mounted under /run/secrets are loaded as configuration keys:
// a file named ConnectionStrings__UserDb becomes ConnectionStrings:UserDb, so the same
// binary reads env vars in compose and secret files in Swarm/K8s without a code change.
builder.Configuration.AddKeyPerFile("/run/secrets", optional: true);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Service", "ContentService")
    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj} {Properties:j}{NewLine}{Exception}")
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(builder.Configuration["Elasticsearch:Uri"] ?? "http://elasticsearch:9200"))
    {
        AutoRegisterTemplate = true,
        IndexFormat = "cms-logs-{0:yyyy.MM}",
        FailureCallback = (logEvent, ex) => Console.Error.WriteLine($"Elasticsearch sink error: {ex.Message}")
    })
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Content Service API",
        Version = "v1",
        Description = "CMS Content Management Microservice — CQRS, Redis Cache, RabbitMQ Consumer"
    });
});

builder.Services.AddInfrastructure(builder.Configuration);

// OpenTelemetry — distributed tracing across all services via Jaeger
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("ContentService"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter(o =>
        {
            o.Endpoint = new Uri(builder.Configuration["Jaeger:OtlpEndpoint"] ?? "http://jaeger:4317");
            o.Protocol = OtlpExportProtocol.Grpc;
        }));

builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("ContentDb")!);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seederLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await db.Database.MigrateAsync();
    await ContentService.Infrastructure.Persistence.DataSeeder.SeedAsync(db, seederLogger);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<IdempotencyMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Content Service API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllers();
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program { }
