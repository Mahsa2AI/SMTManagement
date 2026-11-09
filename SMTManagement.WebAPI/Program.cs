using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.OpenApi;
using Serilog;
using SMTManagement.Contracts.Interfaces.SqliteAdapter;
using SMTManagement.Services;
using SMTManagement.SqliteAdapter;
using SMTManagement.SqliteAdapter.Repositories;
using SMTManagement.WebAPI.Helpers;
using SMTManagement.WebAPI.Middleware;
using SMTManagement.WebAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext();
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/", "UiCookie");
    options.Conventions.AllowAnonymousToPage("/Login");
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ManagementApiClient>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
        options.JsonSerializerOptions.DefaultIgnoreCondition =  System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SMT Management API",
        Version = "v1",
        Description = "Production Management API with JWT authentication"
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "API Token",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Enter your API token."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement{{ new OpenApiSecuritySchemeReference("Bearer", document), new List<string>()}});
});

// Database
builder.Services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
builder.Services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();

// Repositories
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IBoardRepository, BoardRepository>();
builder.Services.AddScoped<IComponentRepository, ComponentRepository>();

// Services
builder.Services.AddScoped<ProductionService, ProductionService>();
builder.Services.AddSingleton<UserAuthenticationService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Bearer";
        options.DefaultChallengeScheme = "Bearer";
    })
    .AddScheme<AuthenticationSchemeOptions, CustomTokenAuthenticationHandler>("Bearer", options => { })
    .AddCookie("UiCookie", options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UiCookie", policy =>
    {
        policy.AddAuthenticationSchemes("UiCookie");
        policy.RequireAuthenticatedUser();
    });
});

try
{
    Log.Information("Starting SMT Management API");

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI();

    var initializer = app.Services.GetRequiredService<IDatabaseInitializer>();

    await initializer.InitializeAsync();
    await app.Services.GetRequiredService<UserAuthenticationService>().EnsureInitialUserAsync();

    Log.Information("Database initialized successfully");

    if (!app.Environment.IsEnvironment("Container"))
        app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapRazorPages();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "SMT Management API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
