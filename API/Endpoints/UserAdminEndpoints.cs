using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleWebData.Data;
using SimpleWebData.Models;
using SimpleWebData.Services;
using System.Security.Claims;

namespace SimpleWebData.Endpoints
{
    public static class UserAdminEndpoints
    {
        public static void MapUserAdminEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/admin");

            // Primjenjujemo Policy koji smo ranije definirali u Program.cs
            group.RequireAuthorization("UserAdminOnly");

            // --- API Keys ---
            group.MapPost("/apikey", ([FromBody] string[] domains, ClaimsPrincipal user, IConfiguration config) =>
            {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var token = TokenService.GenerateReadOnlyApiKey(webSiteId, domains, config);
                return Results.Ok(new { ApiKey = token });
            });

            // --- PhotoGalleries ---
            group.MapGet("/photogalleries", async (AppDbContext db, ClaimsPrincipal user) =>
            {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var galleries = await db.PhotoGalleries.Include(p => p.Photos).Where(p => p.WebSiteId == webSiteId).ToListAsync();
                return Results.Ok(galleries);
            });

            group.MapPost("/photogalleries", async ([FromBody] PhotoGallery input, AppDbContext db, ClaimsPrincipal user) =>
            {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                input.WebSiteId = webSiteId;
                db.PhotoGalleries.Add(input);
                await db.SaveChangesAsync();
                return Results.Created($"/api/admin/photogalleries/{input.Id}", input);
            });

            group.MapPut("/photogalleries/{id}", async (int id, [FromBody] PhotoGallery input, AppDbContext db, ClaimsPrincipal user) =>
            {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var existing = await db.PhotoGalleries.FirstOrDefaultAsync(p => p.Id == id && p.WebSiteId == webSiteId);
                if (existing == null) return Results.NotFound();

                existing.Code = input.Code;
                existing.Name = input.Name;
                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            group.MapDelete("/photogalleries/{id}", async (int id, AppDbContext db, ClaimsPrincipal user) =>
            {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var existing = await db.PhotoGalleries.FirstOrDefaultAsync(p => p.Id == id && p.WebSiteId == webSiteId);
                if (existing == null) return Results.NotFound();

                db.PhotoGalleries.Remove(existing);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            group.MapPost("/photogalleries/{id}/photos", async (int id, [FromForm] string? altText, IFormFile file, AppDbContext db, ClaimsPrincipal user) =>
            {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var gallery = await db.PhotoGalleries.FirstOrDefaultAsync(p => p.Id == id && p.WebSiteId == webSiteId);
                if (gallery == null) return Results.NotFound();

                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                
                var photo = new Photo
                {
                    FileName = file.FileName,
                    AltText = altText,
                    ImageData = memoryStream.ToArray(),
                    PhotoGalleryId = id
                };

                db.Photos.Add(photo);
                await db.SaveChangesAsync();
                return Results.Created($"/api/admin/photos/{photo.Id}", new { photo.Id, photo.FileName, photo.AltText });
            });

            group.MapDelete("/photos/{id}", async (int id, AppDbContext db, ClaimsPrincipal user) =>
            {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                // Provjera da slika pripada galeriji koja pripada korisnikovom sajtu
                var photo = await db.Photos.Include(p => p.PhotoGallery).FirstOrDefaultAsync(p => p.Id == id && p.PhotoGallery.WebSiteId == webSiteId);
                if (photo == null) return Results.NotFound();

                db.Photos.Remove(photo);
                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            group.MapPut("/photos/{id}", async (int id, [FromForm] string? altText, IFormFile? file, AppDbContext db, ClaimsPrincipal user) =>
            {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var photo = await db.Photos.Include(p => p.PhotoGallery).FirstOrDefaultAsync(p => p.Id == id && p.PhotoGallery.WebSiteId == webSiteId);
                if (photo == null) return Results.NotFound();

                photo.AltText = altText;
                
                if (file != null)
                {
                    photo.FileName = file.FileName;
                    using var memoryStream = new MemoryStream();
                    await file.CopyToAsync(memoryStream);
                    photo.ImageData = memoryStream.ToArray();
                }

                await db.SaveChangesAsync();
                return Results.NoContent();
            });

            // --- Facilities & Reservations ---
            group.MapGet("/facilities", async (AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                return Results.Ok(await db.Facilities.Where(f => f.WebSiteId == webSiteId).ToListAsync());
            });

            group.MapPost("/facilities", async ([FromBody] Facility f, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                f.WebSiteId = webSiteId;
                db.Facilities.Add(f);
                await db.SaveChangesAsync();
                return Results.Ok(f);
            });

            group.MapPut("/facilities/{id}", async (int id, [FromBody] Facility input, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var f = await db.Facilities.FirstOrDefaultAsync(x => x.Id == id && x.WebSiteId == webSiteId);
                if (f == null) return Results.NotFound();
                f.Code = input.Code; f.Name = input.Name; f.Description = input.Description; f.PhotoGalleryId = input.PhotoGalleryId;
                await db.SaveChangesAsync(); return Results.Ok(f);
            });

            group.MapDelete("/facilities/{id}", async (int id, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var f = await db.Facilities.FirstOrDefaultAsync(x => x.Id == id && x.WebSiteId == webSiteId);
                if (f == null) return Results.NotFound();
                db.Facilities.Remove(f); await db.SaveChangesAsync(); return Results.NoContent();
            });

            group.MapGet("/facilities/{id}/reservations", async (int id, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var f = await db.Facilities.Include(x => x.Reservations).FirstOrDefaultAsync(x => x.Id == id && x.WebSiteId == webSiteId);
                return f != null ? Results.Ok(f.Reservations) : Results.NotFound();
            });

            group.MapPost("/facilities/{id}/reservations", async (int id, [FromBody] Reservation r, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var f = await db.Facilities.FirstOrDefaultAsync(x => x.Id == id && x.WebSiteId == webSiteId);
                if (f == null) return Results.NotFound();
                r.FacilityId = id; db.Reservations.Add(r); await db.SaveChangesAsync(); return Results.Ok(r);
            });

            group.MapPut("/reservations/{id}", async (int id, [FromBody] Reservation input, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var r = await db.Reservations.Include(x => x.Facility).FirstOrDefaultAsync(x => x.Id == id && x.Facility.WebSiteId == webSiteId);
                if (r == null) return Results.NotFound();
                r.Date = input.Date; r.Status = input.Status;
                await db.SaveChangesAsync(); return Results.Ok(r);
            });

            group.MapDelete("/reservations/{id}", async (int id, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var r = await db.Reservations.Include(x => x.Facility).FirstOrDefaultAsync(x => x.Id == id && x.Facility.WebSiteId == webSiteId);
                if (r == null) return Results.NotFound();
                db.Reservations.Remove(r); await db.SaveChangesAsync(); return Results.NoContent();
            });

            // --- Pages ---
            group.MapGet("/pages", async (AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                return Results.Ok(await db.Pages.Where(p => p.WebSiteId == webSiteId).ToListAsync());
            });

            group.MapPost("/pages", async ([FromBody] Page pg, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                pg.WebSiteId = webSiteId; db.Pages.Add(pg); await db.SaveChangesAsync(); return Results.Ok(pg);
            });

            group.MapPut("/pages/{id}", async (int id, [FromBody] Page input, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var pg = await db.Pages.FirstOrDefaultAsync(x => x.Id == id && x.WebSiteId == webSiteId);
                if (pg == null) return Results.NotFound();
                pg.Code = input.Code; pg.PhotoGalleryId = input.PhotoGalleryId;
                await db.SaveChangesAsync(); return Results.Ok(pg);
            });

            group.MapDelete("/pages/{id}", async (int id, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var pg = await db.Pages.FirstOrDefaultAsync(x => x.Id == id && x.WebSiteId == webSiteId);
                if (pg == null) return Results.NotFound();
                db.Pages.Remove(pg); await db.SaveChangesAsync(); return Results.NoContent();
            });

            group.MapGet("/pages/{id}/texts", async (int id, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var pg = await db.Pages.Include(x => x.Texts).FirstOrDefaultAsync(x => x.Id == id && x.WebSiteId == webSiteId);
                return pg != null ? Results.Ok(pg.Texts) : Results.NotFound();
            });

            group.MapPost("/pages/{id}/texts", async (int id, [FromBody] PageText t, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var pg = await db.Pages.FirstOrDefaultAsync(x => x.Id == id && x.WebSiteId == webSiteId);
                if (pg == null) return Results.NotFound();
                t.PageId = id; t.WebSiteId = webSiteId;
                db.PageTexts.Add(t); await db.SaveChangesAsync(); return Results.Ok(t);
            });

            group.MapPut("/pagetexts/{id}", async (int id, [FromBody] PageText input, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var t = await db.PageTexts.FirstOrDefaultAsync(x => x.Id == id && x.WebSiteId == webSiteId);
                if (t == null) return Results.NotFound();
                t.Code = input.Code; t.Content = input.Content;
                await db.SaveChangesAsync(); return Results.Ok(t);
            });

            group.MapDelete("/pagetexts/{id}", async (int id, AppDbContext db, ClaimsPrincipal user) => {
                int webSiteId = int.Parse(user.Claims.First(c => c.Type == "WebSiteId").Value);
                var t = await db.PageTexts.FirstOrDefaultAsync(x => x.Id == id && x.WebSiteId == webSiteId);
                if (t == null) return Results.NotFound();
                db.PageTexts.Remove(t); await db.SaveChangesAsync(); return Results.NoContent();
            });
        }
    }
}