using FabricExplorerBackend.Api.Extensions;
using FabricExplorerBackend.Api.Startup;
using FabricExplorerBackend.Api.TestAuthenticationHandler;
using Microsoft.AspNetCore.Authentication;
using Npgsql;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// My Dependency Injection

// -- Authentication
//builder.Services
//    .AddAuthentication()
//    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzuredAd"));

// -- Test Authentication | This section is only for testing the active connection endpoint by retrieve the user id from the HttpContext.User
builder.Services
    .AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
        "Test",
        options => { }
    );

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

builder.Services.AddStartupDependencyInjection(builder.Configuration);
builder.Services.AddGlobalExceptionHandler();

// Temporary added to check request-response timing
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService("FabricExplorerBackend"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddNpgsql()
        .AddRedisInstrumentation()
        .AddOtlpExporter(o => o.Endpoint = new Uri("http://jaeger:4317")));

var app = builder.Build();
await app.MigrateDatabaseAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("MyAllowSpecificOrigin");

//app.UseRequestResponseLogging();
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
