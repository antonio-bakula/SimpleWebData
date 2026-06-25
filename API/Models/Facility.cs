using System;
using System.Collections.Generic;

namespace SimpleWebData.Models
{
    public class Facility
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public int WebSiteId { get; set; }
        public WebSite WebSite { get; set; } = null!;

        public ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
    }
}
