using System;
using System.Collections.Generic;

namespace SimpleWebData.Models
{
    public class Page
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;

        // SEO meta podaci stranice (za <title> i <meta> tagove na javnom webu)
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Keywords { get; set; }

        public int WebSiteId { get; set; }
        public WebSite WebSite { get; set; } = null!;

        public ICollection<PageText> Texts { get; set; } = new List<PageText>();
    }
}
