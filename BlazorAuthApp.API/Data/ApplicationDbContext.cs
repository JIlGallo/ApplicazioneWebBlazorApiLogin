using BlazorAuthApp.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BlazorAuthApp.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<ImageEntity> ImageEntities { get; set; }
    }
}

