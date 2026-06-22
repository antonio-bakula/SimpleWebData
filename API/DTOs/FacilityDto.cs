using SimpleWebData.Models;

namespace SimpleWebData.DTOs
{
    public class ReservationDto
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty; // 'Available', 'Pending', 'Booked'
    }

    public class FacilityDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public PhotoGalleryDto? PhotoGallery { get; set; }
        public List<ReservationDto> Reservations { get; set; } = new();
    }
}
