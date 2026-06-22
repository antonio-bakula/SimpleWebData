using System;

namespace SimpleWebData.Models
{
    public class PageText
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public int WebSiteId { get; set; }
        public WebSite WebSite { get; set; } = null!;

        public int PageId { get; set; }
        public Page Page { get; set; } = null!;
    }
}
