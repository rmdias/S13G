using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using S13G.Application.Common.Interfaces;
using FluentValidation.AspNetCore;
using S13G.Infrastructure.Persistence;
using S13G.Infrastructure.Xml;
using S13G.Infrastructure.Messaging;
using S13G.Infrastructure.Configuration;
using S13G.Infrastructure.HealthChecks;
using S13G.Application.Documents.IngestDocument;
using S13G.Application.Documents.Commands;
using S13G.Application.Documents.Queries;
using FluentValidation;
using MediatR;
using Npgsql;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// explicitly configure Kestrel to listen on both HTTP and HTTPS localhost ports
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5000); // http
    options.ListenLocalhost(5001, listenOptions =>
    {
        listenOptions.UseHttps(); // requires a dev certificate
    });
});

// configuration
builder.Services.AddControllers()
    .AddNewtonsoftJson(); // optional for XML payloads

// use FluentValidation for automatic request validation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo { Title = "S13G API", Version = "v1" });
    options.EnableAnnotations();
    // Include XML comments for documentation if available
    try
    {
        var xmlFile = System.IO.Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".xml");
        if (System.IO.File.Exists(xmlFile))
        {
            options.IncludeXmlComments(xmlFile);
        }
    }
    catch (Exception ex)
    {
        System.Console.WriteLine($"Warning: Failed to load XML comments for Swagger: {ex.Message}");
        // Continue without XML comments if they fail to load
    }
});

// register MediatR
builder.Services.AddMediatR(typeof(Program).Assembly, typeof(IngestDocumentCommand).Assembly);

// register validators
builder.Services.AddValidatorsFromAssemblyContaining<S13G.Api.Validators.UpdateDocumentRequestValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<IngestDocumentValidator>();

// persistence
// configure connection string with optional environment overrides
{
    var configConn = builder.Configuration.GetConnectionString("DefaultConnection");
    var npgsqlBuilder = new Npgsql.NpgsqlConnectionStringBuilder(configConn);
    // prefer DB_USER env var, fall back to current OS user (Homebrew default)
    var user = Environment.GetEnvironmentVariable("DB_USER");
    if (string.IsNullOrWhiteSpace(user))
        user = Environment.UserName;
    npgsqlBuilder.Username = user;
    var pass = Environment.GetEnvironmentVariable("DB_PASS");
    if (!string.IsNullOrWhiteSpace(pass))
        npgsqlBuilder.Password = pass;

    var finalConn = npgsqlBuilder.ConnectionString;
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(finalConn));
}

// application services
builder.Services.AddScoped<IFiscalDocumentRepository, FiscalDocumentRepository>();
builder.Services.AddScoped<IDocumentSummaryRepository, DocumentSummaryRepository>();
builder.Services.AddScoped<IXmlDocumentParser, XmlDocumentParser>();
builder.Services.AddSingleton<S13G.Application.Common.Interfaces.IXmlSchemaValidator, S13G.Infrastructure.Xml.XmlSchemaValidator>();

// RabbitMQ configuration
builder.Services.Configure<S13G.Infrastructure.Configuration.RabbitMqOptions>(
    builder.Configuration.GetSection("RabbitMq"));
builder.Services.AddSingleton<RabbitMqPublisher>();
builder.Services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<RabbitMqPublisher>());
builder.Services.AddHostedService<RabbitMqConsumer>();

// health checks
{
    var configConn = builder.Configuration.GetConnectionString("DefaultConnection");
    var npgsqlBuilder = new NpgsqlConnectionStringBuilder(configConn);
    var user = Environment.GetEnvironmentVariable("DB_USER");
    if (string.IsNullOrWhiteSpace(user)) user = Environment.UserName;
    npgsqlBuilder.Username = user;
    var pass = Environment.GetEnvironmentVariable("DB_PASS");
    if (!string.IsNullOrWhiteSpace(pass)) npgsqlBuilder.Password = pass;

    builder.Services.AddHealthChecks()
        .AddCheck("postgresql", new PostgreSqlHealthCheck(npgsqlBuilder.ConnectionString), tags: new[] { "db" })
        .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: new[] { "messaging" });
}

var app = builder.Build();

// Try to apply migrations on startup, but don't fail if database is unavailable
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Database migration failed. The application will continue but database features may not work. Please ensure PostgreSQL is running and accessible.");
}

// always enable Swagger for local testing; environment check removed
app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
    endpoints.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
            var result = JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                services = report.Entries.ToDictionary(
                    e => e.Key,
                    e => new
                    {
                        status = e.Value.Status.ToString(),
                        error = e.Value.Exception?.Message
                    })
            });
            await context.Response.WriteAsync(result);
        }
    });
});

app.Run();
