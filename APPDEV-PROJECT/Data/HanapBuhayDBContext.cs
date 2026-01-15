using Microsoft.EntityFrameworkCore; // we will use the functionality of the entity framework core to communicate with the database
using APPDEV_PROJECT.Models.Entities; // to access the Client model
namespace APPDEV_PROJECT.Data
{
    public class HanapBuhayDBContext: DbContext // dbcontext kasi we are making a database context class
    {
        // we are gpoing to make a constructor to pass the options to the base class

        public HanapBuhayDBContext(DbContextOptions<HanapBuhayDBContext> options) : base (options)
        {
            
        }
        public DbSet<Client> Clients { get; set; } // represent the clients table in the database
    }
}
