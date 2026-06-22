namespace SimpleWebDataAdmin.Models
{
    public class PageText
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int WebSiteId { get; set; }
        public int PageId { get; set; }
    }
}
