using CorporateApp.Application.Mappings;
using CorporateApp.Infrastructure;
using CorporateApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Serilog;
using Hangfire;                              // ← EKLE
using Hangfire.SqlServer;                    // ← EKLE
using CorporateApp.Infrastructure.Filters;    // ← EKLE
using CorporateApp.Application.Interfaces;    // ← EKLE
using CorporateApp.Infrastructure.Services.DiaServices;  // ← Bu satırı ekleyin
using CorporateApp.Application.Services;
using CorporateApp.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add Infrastructure Layer (Repository, DbContext, Services)
builder.Services.AddInfrastructure(builder.Configuration);

// Add AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("UserOnly", policy => policy.RequireRole("User"));
});
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});



builder.Services.AddHttpClient("DiaApi", client =>
{
    var baseUrl = builder.Configuration["DiaApi:BaseUrl"] ?? "https://fayev.ws.dia.com.tr/";

    // Sonunda / olduğundan emin ol
    if (!baseUrl.EndsWith("/"))
        baseUrl += "/";

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    // Content-Type header'ını BURAYA EKLEMEYİN!
    client.Timeout = TimeSpan.FromSeconds(30);
});


builder.Services.Configure<DiaApiConfiguration>(
    builder.Configuration.GetSection("DiaApi")
);
// HttpClient configuration
builder.Services.AddHttpClient("DiaApi", (serviceProvider, client) =>
{
    var configuration = serviceProvider.GetRequiredService<IOptions<DiaApiConfiguration>>();
    var baseUrl = configuration.Value.BaseUrl;

    if (!baseUrl.EndsWith("/"))
        baseUrl += "/";

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(configuration.Value.Timeout);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// Service registration - MEVCUT KODU DEĞİŞTİRİN
builder.Services.AddScoped<IDiaLoginService, DiaLoginService>();
builder.Services.AddScoped<IPersonelSyncService, PersonelSyncService>();

// Register DIA services 
builder.Services.AddScoped<IPersonelSyncService, PersonelSyncService>();
builder.Services.AddScoped<IPersonelSyncJobService, PersonelSyncJobService>();

// Hangfire Configuration
builder.Services.AddHangfire(configuration => configuration
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(builder.Configuration.GetConnectionString("HangfireConnection"), new SqlServerStorageOptions
    {
        CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
        SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
        QueuePollInterval = TimeSpan.Zero,
        UseRecommendedIsolationLevel = true,
        DisableGlobalLocks = true
    }));

// Hangfire Server
builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = builder.Configuration.GetValue<int>("Hangfire:WorkerCount");
    options.Queues = builder.Configuration.GetSection("Hangfire:Queues").Get<string[]>();
});






var app = builder.Build();
// After app.Build()
using (var scope = app.Services.CreateScope())
{
    var jobService = scope.ServiceProvider.GetRequiredService<IPersonelSyncJobService>();
    jobService.ConfigureRecurringJobs();
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CorporateApp API V1");
        c.RoutePrefix = "swagger";
    });
}
// Hangfire Dashboard - UseAuthentication'dan ÖNCE olmalı
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter(builder.Configuration) },
    DashboardTitle = "CorporateApp - Background Jobs"
});

// Middleware order is important!
app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/", () => Results.Ok(new
{
    message = "CorporateApp API is running",
    status = "Healthy",
    timestamp = DateTime.UtcNow,
    swagger = "/swagger"
}));

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    timestamp = DateTime.UtcNow
}));

// Database Migration
using (var scope = app.Services.CreateScope())
{
    try
    {
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<AppDbContext>();

        // Check if database exists and can connect
        if (context.Database.CanConnect())
        {
            Log.Information("Database connection successful");

            // Apply pending migrations
            if (context.Database.GetPendingMigrations().Any())
            {
                Log.Information("Applying database migrations...");
                context.Database.Migrate();
                Log.Information("Database migrations completed successfully");
            }
            else
            {
                Log.Information("Database is up to date");
            }
        }
        else
        {
            Log.Warning("Cannot connect to database. Creating new database...");
            context.Database.Migrate();
            Log.Information("Database created and migrations applied successfully");
        }
    }
    catch (Exception ex)
    {
        Log.Error(ex, "An error occurred during database migration");
        // Don't throw - let the application start even if migration fails
    }
}




Log.Information("Starting CorporateApp API...");
Log.Information($"Environment: {app.Environment.EnvironmentName}");
Log.Information($"URLs: {string.Join(", ", builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey)?.Split(';') ?? new[] { "http://localhost:5000" })}");


using (var scope = app.Services.CreateScope())
{
    var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

    // Her gün saat 02:00'de çalışacak
    recurringJobManager.AddOrUpdate<IErpSyncJobService>(
        "erp-daily-sync",
        service => service.SyncUsersFromErpAsync(),
        "0 2 * * *", // CRON expression
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")
            // Queue özelliği kaldırıldı
        });

    // Her 30 dakikada bir çalışacak
    recurringJobManager.AddOrUpdate<IErpSyncJobService>(
        "erp-incremental-sync",
        service => service.IncrementalSyncAsync(),
        "*/30 * * * *",
        new RecurringJobOptions
        {
            TimeZone = TimeZoneInfo.Local  // veya TimeZoneInfo.Utc
            // Queue özelliği kaldırıldı
        });
}


// app.MapGet("/test-login", async (IDiaLoginService diaLoginService, ILogger<Program> logger) =>
// {
//     try
//     {
//         var result = await diaLoginService.LoginAsync();

//         logger.LogInformation($"Login test - Success: {result.Success}, SessionId: {result.SessionId}");

//         return Results.Ok(new
//         {
//             success = result.Success,
//             sessionId = result.SessionId,
//             message = result.Message
//         });
//     }
//     catch (Exception ex)
//     {
//         logger.LogError(ex, "Login test failed");
//         return Results.Problem(ex.Message);
//     }
// });


app.Run();