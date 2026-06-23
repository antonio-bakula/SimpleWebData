using System.Collections.Generic;

namespace SimpleWebDataAdmin.Models
{
    public class AuthTokensDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class UserTokenData
    {
        public bool IsSuperUser { get; set; }
        public int WebSiteId { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public class WebSite
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public bool IsSuperUser { get; set; }
        public int WebSiteId { get; set; }
    }

    public class PhotoGallery
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int WebSiteId { get; set; }
        
        public List<Photo> Photos { get; set; } = new();
    }

    public class Photo
    {
        public int Id { get; set; }
        public string? AltText { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int PhotoGalleryId { get; set; }
    }

    public class Facility
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int WebSiteId { get; set; }
        public int? PhotoGalleryId { get; set; }
    }

    public class Page
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int WebSiteId { get; set; }
        public int? PhotoGalleryId { get; set; }
    }

    public enum ReservationStatus
    {
        Available,
        Pending,
        Booked
    }

    public class Reservation
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Available;
        public int FacilityId { get; set; }
    }
}