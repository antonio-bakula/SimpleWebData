using SimpleWebData.Models;

namespace SimpleWebData.DTOs
{
    public class PageTextDto
    {
        public string Code { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }

    public class PageDto
    {
        public string Code { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Keywords { get; set; }
        public PhotoGalleryDto? PhotoGallery { get; set; }
        public List<PageTextDto> Texts { get; set; } = new();
    }
}
