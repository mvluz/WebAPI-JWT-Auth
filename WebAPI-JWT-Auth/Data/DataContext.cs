using Microsoft.EntityFrameworkCore;
using WebAPI_JWT_Auth.Data.Repositoty;

namespace WebAPI_JWT_Auth.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {

        }

        public DbSet<User> TbUser { get; set; }

    }
}
