using API_Commande.Models;
using Microsoft.EntityFrameworkCore;

namespace API_Commande.Context
{
    public class CommandeContext : DbContext
    {
        public CommandeContext(DbContextOptions<CommandeContext> options) : base(options) { }

        public DbSet<Commande> Orders { get; set; }
    }
}
