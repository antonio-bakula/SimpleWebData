using System;
using System.Collections.Generic;

namespace SimpleWebData.Models
{
    public class WebSite
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<PhotoGallery> PhotoGalleries { get; set; } = new List<PhotoGallery>();
        public ICollection<Facility> Facilities { get; set; } = new List<Facility>();
        public ICollection<Page> Pages { get; set; } = new List<Page>();
        public ICollection<PageText> PageTexts { get; set; } = new List<PageText>();
    }
}
