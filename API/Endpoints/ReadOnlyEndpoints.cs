using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SimpleWebData.Data;
using SimpleWebData.DTOs;
using System.Security.Claims;
using System.Text.Json;

namespace SimpleWebData.Endpoints
{
    public static class ReadOnlyEndpoints
    {
        public static void MapReadOnlyEndpoints(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/read");

            // Filter koji presreće svaki zahtjev na "Read" rutama i obavlja Custom provjeru origin/referer domena
            group.AddEndpointFilter(async (invocationContext, next) =>
            {
                var httpContext = invocationContext.HttpContext;
                
                // Propustanje /images/ rute bez strogog Authorization i domain headera jer <img> html element nema te headere
                if (httpContext.Request.Path.Value != null && httpContext.Request.Path.Value.StartsWith("/api/read/images/"))
                {
                    return await next(invocationContext);
                }

                var user = httpContext.User;
                var allowedDomainsClaim = user.Claims.FirstOrDefault(c => c.Type == "AllowedDomains")?.Value;

                if (string.IsNullOrEmpty(allowedDomainsClaim))
                {
                    return Results.Forbid(); 
                }

                var allowedDomains = JsonSerializer.Deserialize<string[]>(allowedDomainsClaim);
                var origin = httpContext.Request.Headers.Origin.FirstOrDefault();
                var referer = httpContext.Request.Headers.Referer.FirstOrDefault();

                bool isValidOrigin = false;
                if (allowedDomains != null && allowedDomains.Length > 0)
                {
                    foreach(var domain in allowedDomains)
                    {
                        // Ako stavimo "*" znak propušta sve (bitno npr za lako testiranje i lokalno)
                        if (domain == "*" || 
                           (!string.IsNullOrEmpty(origin) && origin.Contains(domain, StringComparison.OrdinalIgnoreCase)) ||
                           (!string.IsNullOrEmpty(referer) && referer.Contains(domain, StringComparison.OrdinalIgnoreCase)))
                        {
                            isValidOrigin = true;
                            break;
                        }
                    }
                }

                if (!isValidOrigin)
                {
                    return Results.StatusCode(StatusCodes.Status403Forbidden);
                }

                return await next(invocationContext);
            });

            // Primjenjujemo Auth samo na zahtjeve koji nisu deklarirani eksplicitno sa AllowAnonymous()
            group.RequireAuthorization();

            group.MapGet("/pages/{code}", async (string code, AppDbContext db, ClaimsPrincipal user) =>
            {
                var websiteIdStr = user.Claims.FirstOrDefault(c => c.Type == "WebSiteId")?.Value;
                if (!int.TryParse(websiteIdStr, out int webSiteId)) return Results.Unauthorized();

                var page = await db.Pages
                    .Include(p => p.Texts)
                    .Include(p => p.PhotoGallery)
                        .ThenInclude(pg => pg!.Photos)
                    .FirstOrDefaultAsync(p => p.WebSiteId == webSiteId && p.Code == code);

                if (page == null) return Results.NotFound();

                var dto = new PageDto
                {
                    Code = page.Code,
                    Texts = page.Texts.Select(t => new PageTextDto { Code = t.Code, Content = t.Content }).ToList(),
                    PhotoGallery = page.PhotoGallery != null ? new PhotoGalleryDto
                    {
                        Code = page.PhotoGallery.Code,
                        Name = page.PhotoGallery.Name,
                        Description = page.PhotoGallery.Description,
                        Photos = page.PhotoGallery.Photos.Select(ph => new PhotoDto
                        {
                            AltText = ph.AltText,
                            FileName = ph.FileName,
                            ImageUrl = $"/api/read/images/{page.PhotoGallery.Code}/{ph.FileName}"
                        }).ToList()
                    } : null
                };

                return Results.Ok(dto);
            });

            group.MapGet("/photogalleries/{code}", async (string code, AppDbContext db, ClaimsPrincipal user) =>
            {
                var websiteIdStr = user.Claims.FirstOrDefault(c => c.Type == "WebSiteId")?.Value;
                if (!int.TryParse(websiteIdStr, out int webSiteId)) return Results.Unauthorized();

                var pg = await db.PhotoGalleries
                    .Include(p => p.Photos)
                    .FirstOrDefaultAsync(p => p.WebSiteId == webSiteId && p.Code == code);

                if (pg == null) return Results.NotFound();

                var dto = new PhotoGalleryDto
                {
                    Code = pg.Code,
                    Name = pg.Name,
                    Description = pg.Description,
                    Photos = pg.Photos.Select(ph => new PhotoDto
                    {
                        AltText = ph.AltText,
                        FileName = ph.FileName,
                        ImageUrl = $"/api/read/images/{pg.Code}/{ph.FileName}"
                    }).ToList()
                };

                return Results.Ok(dto);
            });

            group.MapGet("/facilities/{code}/reservations", async (string code, AppDbContext db, ClaimsPrincipal user, [FromQuery] DateTime? from, [FromQuery] DateTime? to) =>
            {
                var websiteIdStr = user.Claims.FirstOrDefault(c => c.Type == "WebSiteId")?.Value;
                if (!int.TryParse(websiteIdStr, out int webSiteId)) return Results.Unauthorized();

                var facility = await db.Facilities
                    .Include(f => f.Reservations)
                    .Include(f => f.PhotoGallery)
                        .ThenInclude(pg => pg!.Photos)
                    .FirstOrDefaultAsync(f => f.WebSiteId == webSiteId && f.Code == code);

                if (facility == null) return Results.NotFound();

                var resQuery = facility.Reservations.AsEnumerable();
                if (from.HasValue) resQuery = resQuery.Where(r => r.Date >= from.Value);
                if (to.HasValue) resQuery = resQuery.Where(r => r.Date <= to.Value);

                var dto = new FacilityDto
                {
                    Code = facility.Code,
                    Name = facility.Name,
                    Description = facility.Description,
                    PhotoGallery = facility.PhotoGallery != null ? new PhotoGalleryDto
                    {
                        Code = facility.PhotoGallery.Code,
                        Name = facility.PhotoGallery.Name,
                        Description = facility.PhotoGallery.Description,
                        Photos = facility.PhotoGallery.Photos.Select(ph => new PhotoDto
                        {
                            AltText = ph.AltText,
                            FileName = ph.FileName,
                            ImageUrl = $"/api/read/images/{facility.PhotoGallery.Code}/{ph.FileName}"
                        }).ToList()
                    } : null,
                    Reservations = resQuery.Select(r => new ReservationDto
                    {
                        Date = r.Date,
                        Status = r.Status.ToString()
                    }).ToList()
                };

                return Results.Ok(dto);
            });

            // Metoda za serviranje raw bloba - Nije pod globalnim RequiresAuthorization filterom
            group.MapGet("/images/{galleryCode}/{fileName}", async (string galleryCode, string fileName, AppDbContext db) =>
            {
                var photo = await db.Photos
                    .Include(p => p.PhotoGallery)
                    .FirstOrDefaultAsync(p => p.PhotoGallery.Code == galleryCode && p.FileName == fileName);

                if (photo == null || photo.ImageData == null || photo.ImageData.Length == 0)
                {
                    return Results.NotFound();
                }

                // Odredi ekstenziju kako bi ispravno isporučio header Content-Type
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var mimeType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".svg" => "image/svg+xml",
                    _ => "application/octet-stream"
                };

                return Results.File(photo.ImageData, mimeType);
            }).AllowAnonymous();
        }
    }
}
