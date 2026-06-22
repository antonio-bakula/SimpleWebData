using Microsoft.EntityFrameworkCore;
using SimpleWebData.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleWebData.Data
{
    public static class DataSeeder
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var context = new AppDbContext(serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());
            
            // Osigurava da je baza kreirana i u najnovijoj migraciji
            await context.Database.MigrateAsync();

            // Prekini ako u bazi već postoje korisnici - znači da je seeding ranije odrađen
            if (await context.Users.AnyAsync())
                return;

            // 1. Kreiranje osnovnog testnog WebSite-a
            var website = new WebSite
            {
                Code = "demo-site",
                Description = "Testni Demo WebSite"
            };
            context.WebSites.Add(website);
            await context.SaveChangesAsync(); // Spremanje da dobijemo website.Id

            // 2. Kreiranje super usera i obicnog usera (vlasnika stranice)
            var superAdmin = new User
            {
                Username = "superadmin",
                Password = "123", // NAPOMENA: Za test ostavljamo plain text 
                FirstName = "Super",
                LastName = "Admin",
                IsSuperUser = true,
                WebSiteId = website.Id
            };

            var userAdmin = new User
            {
                Username = "admin",
                Password = "123",
                FirstName = "Web",
                LastName = "Vlasnik",
                IsSuperUser = false,
                WebSiteId = website.Id
            };
            context.Users.AddRange(superAdmin, userAdmin);

            // 3. Kreiranje fotogalerija
            var gallery1 = new PhotoGallery { Code = "gal-apartman", Name = "Apartman Sunce", WebSiteId = website.Id };
            var gallery2 = new PhotoGallery { Code = "gal-okolina", Name = "Okolica", WebSiteId = website.Id };
            context.PhotoGalleries.AddRange(gallery1, gallery2);
            await context.SaveChangesAsync();

            // 4. Objekti za najam i rezervacije
            var fac1 = new Facility { Code = "ap1", Name = "Apartman Sunce (A1)", WebSiteId = website.Id, PhotoGalleryId = gallery1.Id };
            var fac2 = new Facility { Code = "ap2", Name = "Soba More (S1)", WebSiteId = website.Id };
            context.Facilities.AddRange(fac1, fac2);
            await context.SaveChangesAsync();

            context.Reservations.AddRange(
                new Reservation { Date = DateTime.Today, Status = ReservationStatus.Booked, FacilityId = fac1.Id },
                new Reservation { Date = DateTime.Today.AddDays(1), Status = ReservationStatus.Booked, FacilityId = fac1.Id },
                new Reservation { Date = DateTime.Today.AddDays(2), Status = ReservationStatus.Pending, FacilityId = fac1.Id },
                new Reservation { Date = DateTime.Today.AddDays(1), Status = ReservationStatus.Available, FacilityId = fac2.Id }
            );

            // 5. Stranice i tekstovi
            var pageHome = new Page { Code = "home", WebSiteId = website.Id, PhotoGalleryId = gallery2.Id };
            var pageContact = new Page { Code = "contact", WebSiteId = website.Id };
            context.Pages.AddRange(pageHome, pageContact);
            await context.SaveChangesAsync();

            context.PageTexts.AddRange(
                new PageText { Code = "title", Content = "Dobrodošli u naše privatne smještaje", WebSiteId = website.Id, PageId = pageHome.Id },
                new PageText { Code = "subtitle", Content = "Uživajte u bezbrižnom odmoru pokraj mora", WebSiteId = website.Id, PageId = pageHome.Id },
                new PageText { Code = "address", Content = "Zagrebačka 10, 10000 Zagreb", WebSiteId = website.Id, PageId = pageContact.Id },
                new PageText { Code = "email", Content = "info@nasi-apartmani.hr", WebSiteId = website.Id, PageId = pageContact.Id }
            );

            await context.SaveChangesAsync();
            
            Console.WriteLine("Baza je uspješno inicijalizirana i popunjena!");
        }
    }
}