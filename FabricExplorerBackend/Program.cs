using FabricExplorerBackend.Extensions;
using FabricExplorerBackend.Features.Cache;
using FabricExplorerBackend.Features.Connection;
using FabricExplorerBackend.Features.Fabric.Client;
using FabricExplorerBackend.Features.Fabric.Context;
using FabricExplorerBackend.Features.Fabric.Lakehouse;
using FabricExplorerBackend.Features.Fabric.Operation;
using FabricExplorerBackend.Features.Mappers.FabricOperation;
using FabricExplorerBackend.Features.Token;
using FabricExplorerBackend.Infrastructures.Persistences;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Implements;
using FabricExplorerBackend.Infrastructures.Persistences.Repositories.Interfaces;
using FabricExplorerBackend.Infrastructures.Securities;
using FabricExplorerBackend.Mappers;
using FabricExplorerBackend.Mappers.FabricOperation;
using FabricExplorerBackend.TestAuthenticationHandler;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

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

// -- Test Cors
builder.Services.AddCors(options =>
{
    options.AddPolicy("MyAllowSpecificOrigin", policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5500")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// -- Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -- Datas
builder.Services.AddDbContext<FabricExplorerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FabricExplorerDatabase")));
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisCache")!));
builder.Services.AddHttpContextAccessor();
builder.Services
    .AddDataProtection()
    .SetApplicationName("FabricExplorerBackend")
    .PersistKeysToFileSystem(
        new DirectoryInfo(
            Path.Combine(
                builder.Environment.ContentRootPath,
                "DataProtectionKeys")));
builder.Services.AddSingleton<IDataProtector>(sp =>
    sp.GetRequiredService<IDataProtectionProvider>()
      .CreateProtector("FabricExplorer.Secrets"));

// -- Repositories
builder.Services.AddSingleton<ISecretProtector, SecretProtector>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IConnectionRepository, ConnectionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IFabricOperationRepository, FabricOperationRepository>();

// -- Mappers
builder.Services.AddScoped<IConnectionMapper, ConnectionMapper>();
builder.Services.AddScoped<IFabricOperationMapper, FabricOperationMapper>();

// -- Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IFabricContextFactory, FabricContextFactory>();
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IClientFactory, ClientFactory>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<ILakehouseService, LakehouseService>();
builder.Services.AddScoped<IFabricOperationService, FabricOperationService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
