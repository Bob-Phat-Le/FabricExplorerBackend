using FabricExplorerBackend.Extensions;
using FabricExplorerBackend.Infrastructures.Persistences;
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

// -- Http
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();

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

builder.Services.AddRepositories();
builder.Services.AddServices();
builder.Services.AddMappers();

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
