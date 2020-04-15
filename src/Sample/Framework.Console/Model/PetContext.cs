using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framework.ConsoleDemo.Model
{
    public class PetContext : DbContext
    {
        public PetContext() : base("PetContext")
        {
        }

        public DbSet<PropType> PropTypes { get; set; }

    }
}
