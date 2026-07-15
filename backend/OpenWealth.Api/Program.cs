using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenWealth.Api.Data;
using OpenWealth.Api.Endpoints;
using OpenWealth.Api.Models;
using OpenWealth.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=openwealth.db"));

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<SummaryService>();
builder.Services.AddScoped<AccrualService>();
builder.Services.AddHostedService<AccrualWorker>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = TokenService.Issuer,
            ValidAudience = TokenService.Issuer,
            IssuerSigningKey = TokenService.SigningKey(builder.Configuration),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// The Vite dev server proxies /api in development, but allow direct calls too.
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    DbInitializer.Initialize(scope.ServiceProvider.GetRequiredService<AppDbContext>());
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapIncomeEndpoints();
app.MapStudentLoanEndpoints();
app.MapPropertyEndpoints();
app.MapMortgageEndpoints();
app.MapSavingsEndpoints();
app.MapInvestmentEndpoints();
app.MapCustomAssetEndpoints();
app.MapCustomDebtEndpoints();
app.MapLedgerEndpoints();
app.MapGoalEndpoints();
app.MapSummaryEndpoints();
app.MapSettingsEndpoints();
app.MapShareEndpoints();
app.MapAutomationEndpoints();
app.MapHouseholdEndpoints();

// Serve the built React app (frontend/dist copied to wwwroot) in production.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapFallbackToFile("index.html");

app.Run();
