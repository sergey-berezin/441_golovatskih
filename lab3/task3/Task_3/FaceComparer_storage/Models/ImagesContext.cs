using Microsoft.EntityFrameworkCore;

namespace FaceComparer_storage.Models
{
    public class ImagesContext : DbContext
    {
        public DbSet<ImageItem> Images { get; set; }
        public DbSet<ImageDetails> ImagesDetails { get; set; }

        public ImagesContext() => Database.EnsureCreated();

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            builder.UseSqlite("Data Source=Database.db");
        }
    }
}
