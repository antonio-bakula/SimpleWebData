using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleWebData.Data;
using SimpleWebData.DTOs;
using SimpleWebData.Services;

namespace SimpleWebData.Endpoints
{
    public static class AuthEndpoints
    {
        public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth");

            // 1. POST /api/auth/login
            group.MapPost("/login", async ([FromBody] LoginRequestDto request, AppDbContext db, IConfiguration config) =>
            {
                // Napomena: U jednostavnom primjeru lozinka je ostavljena kao plain tekst (prema DataSeederu).
                // U stvarnoj produkciji ovdje bi koristili BCrypt ili PBKDF2 za validaciju hasha!
                var user = await db.Users.SingleOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);
                
                if (user == null)
                {
                    return Results.Unauthorized();
                }

                var accessToken = TokenService.GenerateAccessToken(user, config);
                var refreshToken = TokenService.GenerateRefreshToken();

                // Snimi refresh token za naredne /refresh zahtjeve (ističe za 7 dana)
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
                await db.SaveChangesAsync();

                return Results.Ok(new AuthTokensDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                });
            });

            // 2. POST /api/auth/refresh
            group.MapPost("/refresh", async ([FromBody] AuthTokensDto request, AppDbContext db, IConfiguration config) =>
            {
                // Pronađi korisnika direktno preko podudaranja refresh tokena
                var user = await db.Users.SingleOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);
                
                if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                {
                    return Results.Unauthorized(); // Token ne valja ili je istekao
                }

                // Generiranje novog para tokena (tzv. "Refresh Token Rotation")
                var newAccessToken = TokenService.GenerateAccessToken(user, config);
                var newRefreshToken = TokenService.GenerateRefreshToken();

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Produži valjanost nakon svake upotrebe
                await db.SaveChangesAsync();

                return Results.Ok(new AuthTokensDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = newRefreshToken
                });
            });
        }
    }
}
