using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleWebData.Data;
using SimpleWebData.Models;

namespace SimpleWebData.Endpoints
{
    public static class SuperUserAdminEndpoints
    {
        public static void MapSuperUserAdminEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/superadmin");
            
            // Limitiramo ovu grupu ruta isključivo na korisnike koji imaju Claim "IsSuperUser" = "True"
            group.RequireAuthorization("SuperUserOnly");

            // --- WebSites ---
            group.MapGet("/websites", async (AppDbContext db) =>
                Results.Ok(await db.WebSites.ToListAsync()));

            group.MapPost("/websites", async ([FromBody] WebSite site, AppDbContext db) =>
            {
                db.WebSites.Add(site);
                await db.SaveChangesAsync();
                return Results.Created($"/api/superadmin/websites/{site.Id}", site);
            });

            group.MapPut("/websites/{id}", async (int id, [FromBody] WebSite input, AppDbContext db) =>
            {
                var existing = await db.WebSites.FindAsync(id);
                if (existing == null) return Results.NotFound();

                existing.Code = input.Code;
                existing.Description = input.Description;
                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            group.MapDelete("/websites/{id}", async (int id, AppDbContext db) =>
            {
                var existing = await db.WebSites.FindAsync(id);
                if (existing == null) return Results.NotFound();

                db.WebSites.Remove(existing);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            // --- Users ---
            group.MapGet("/users", async (AppDbContext db) =>
            {
                // Izbjegavanje slanja passworda i refresh tokena natrag do frontenda (anonimiziranje selekcije)
                var users = await db.Users.Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.IsSuperUser,
                    u.WebSiteId
                }).ToListAsync();

                return Results.Ok(users);
            });

            group.MapPost("/users", async ([FromBody] User input, AppDbContext db) =>
            {
                // Provjera postojanja WebSite-a kojemu se User asocira
                var siteExists = await db.WebSites.AnyAsync(w => w.Id == input.WebSiteId);
                if (!siteExists) return Results.BadRequest("Invalid WebSiteId.");

                db.Users.Add(input);
                await db.SaveChangesAsync();
                
                var userResponse = new { input.Id, input.Username, input.WebSiteId };
                return Results.Created($"/api/superadmin/users/{input.Id}", userResponse);
            });

            group.MapPut("/users/{id}", async (int id, [FromBody] User input, AppDbContext db) =>
            {
                var existing = await db.Users.FindAsync(id);
                if (existing == null) return Results.NotFound();

                var siteExists = await db.WebSites.AnyAsync(w => w.Id == input.WebSiteId);
                if (!siteExists) return Results.BadRequest("Invalid WebSiteId.");

                existing.Username = input.Username;
                
                // Azuriraj password samo ako je poslan novi (iz sigurnosnih i prakticnih razloga kod edita)
                if (!string.IsNullOrEmpty(input.Password))
                {
                    existing.Password = input.Password;
                }
                
                existing.FirstName = input.FirstName;
                existing.LastName = input.LastName;
                existing.Email = input.Email;
                existing.IsSuperUser = input.IsSuperUser;
                existing.WebSiteId = input.WebSiteId;

                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            group.MapDelete("/users/{id}", async (int id, AppDbContext db) =>
            {
                var existing = await db.Users.FindAsync(id);
                if (existing == null) return Results.NotFound();

                db.Users.Remove(existing);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });
        }
    }
}
