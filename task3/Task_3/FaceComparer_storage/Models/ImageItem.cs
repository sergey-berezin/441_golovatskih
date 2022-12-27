using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace FaceComparer_storage.Models
{
    public class ImageItem
    {
        [Key]
        public int Id { get; set; }
        public string Hash { get; set; }
        public byte[] Embeddings { get; set; }
        public ImageDetails Details { get; set; }
    }

    public class ImageDetails
    {
        [Key]
        [ForeignKey(nameof(ImageItem))]
        public int Id { get; set; }
        public byte[] Data { get; set; }
        public string FullFileName { get; set; }
    }
}
