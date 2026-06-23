using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SimpleWebData.Data;
using SimpleWebData.Endpoints;
using System.Security.Claims;
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

// Super admin može raditi na bilo kojem web site-u: ako pošalje X-WebSite-Id header,
// pregazimo WebSiteId claim za taj zahtjev pa svi /api/admin endpointi rade na odabranom site-u.
// Obični admin nema IsSuperUser=True claim pa se header ignorira (ostaje zaključan na svoj site).
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        context.User.HasClaim("IsSuperUser", "True") &&
        context.Request.Headers.TryGetValue("X-WebSite-Id", out var raw) &&
        int.TryParse(raw, out var targetSiteId) &&
        context.User.Identity is ClaimsIdentity identity)
    {
        var existing = identity.FindFirst("WebSiteId");
        if (existing != null) identity.RemoveClaim(existing);
        identity.AddClaim(new Claim("WebSiteId", targetSiteId.ToString()));
    }

    await next();
});

// Registriranje novih Endpoint klasa
app.MapAuthEndpoints();
app.MapReadOnlyEndpoints();
app.MapUserAdminEndpoints();
app.MapSuperUserAdminEndpoints();

app.MapGet("/", () => "SimpleWebData API is running!");

app.Run();
