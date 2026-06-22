using System;

namespace SimpleWebData.Models
{
    public class Reservation
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public ReservationStatus Status { get; set; } = ReservationStatus.Available;

        public int FacilityId { get; set; }
        public Facility Facility { get; set; } = null!;
    }

    public enum ReservationStatus
    {
        Available,
        Pending,
        Booked
    }
}
