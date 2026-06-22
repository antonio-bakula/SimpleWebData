using System;
using System.Collections.Generic;

namespace SimpleWebData.Models
{
    public class Page
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;

        public int WebSiteId { get; set; }
        public WebSite WebSite { get; set; } = null!;

        public int? PhotoGalleryId { get; set; }
        public PhotoGallery? PhotoGallery { get; set; }

        public ICollection<PageText> Texts { get; set; } = new List<PageText>();
    }
}
