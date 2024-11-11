using CodeBridgeTestTask.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace CodeBridgeTestTask.Contexts
{
    public class DogContext : DbContext
    {
        public DogContext(DbContextOptions<DogContext> options) : base(options) { }

        public DbSet<Dog> Dogs { get; set; }

    }
}
