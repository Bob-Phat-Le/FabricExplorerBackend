using FabricExplorerBackend.Extensions;
using FabricExplorerBackend.Mappers;
using FabricExplorerBackend.Mappers.Connection;
using FabricExplorerBackend.Persistences;
using FabricExplorerBackend.Repositories.Implements;
using FabricExplorerBackend.Repositories.Interfaces;
using FabricExplorerBackend.Securities;
using FabricExplorerBackend.Services.Implements;
using FabricExplorerBackend.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// My Dependency Injection

// -- Authentication
builder.Services
    .AddAuthentication()
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzuredAd"));

// -- Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// -- Datas
builder.Services.AddDbContext<FabricExplorerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FabricExplorerDatabase")));
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("RedisCache") + ",abortConnect=false"!));
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

// -- Mappers
builder.Services.AddScoped<IConnectionMapper, ConnectionMapper>();

// -- Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IFabricContextFactory, FabricContextFactory>();
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddScoped<IClientFactory, ClientFactory>();
builder.Services.AddScoped<ICacheService, CacheService>();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
