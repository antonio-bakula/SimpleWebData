using System;

namespace SimpleWebData.Models
{
    public class Photo
    {
        public int Id { get; set; }
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string? AltText { get; set; }
        public string FileName { get; set; } = string.Empty;

        public int PhotoGalleryId { get; set; }
        public PhotoGallery PhotoGallery { get; set; } = null!;
    }
}
