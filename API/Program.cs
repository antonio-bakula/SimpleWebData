using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SimpleWebData.Data;
using SimpleWebData.Endpoints;
using System.Text;

using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Konfiguriranje HTTP JSON opcija zbog EF referenci (Cycles)
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Dodavanje DbContexta sa SQLite podrškom
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Konfiguracija CORS-a 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Postavljanje JWT Autentikacije
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is missing!");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SuperUserOnly", policy => policy.RequireClaim("IsSuperUser", "True"));
    options.AddPolicy("UserAdminOnly", policy => policy.RequireClaim("WebSiteId"));
});

var app = builder.Build();

// Pozivanje Seedera prije nego aplikacija krene primati requestove
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DataSeeder.InitializeAsync(services);
}

app.UseHttpsRedirection();

// Primjena CORS politike i Auth middleware-a
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// Registriranje novih Endpoint klasa
app.MapAuthEndpoints();
app.MapReadOnlyEndpoints();
app.MapUserAdminEndpoints();
app.MapSuperUserAdminEndpoints();

app.MapGet("/", () => "SimpleWebData API is running!");

app.Run();
