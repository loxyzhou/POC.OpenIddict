using Microsoft.EntityFrameworkCore;

namespace APIAuthorizationServer.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder) { }
    }
}
