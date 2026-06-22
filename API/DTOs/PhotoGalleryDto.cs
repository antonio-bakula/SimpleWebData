namespace SimpleWebData.DTOs
{
    public class PhotoDto
    {
        public string? AltText { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty; // Konstruirat će se na backendu
    }

    public class PhotoGalleryDto
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<PhotoDto> Photos { get; set; } = new();
    }
}
