using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pomelo.EntityFrameworkCore.MySql;
using System.Reflection.Emit;
using WebApplication1.Models;
using EmailWebApi.Api.Models;

namespace EmailWebApi.Api.Data
{
    public class EmailDBContact : DbContext
    {
        public DbSet<Email> Emails { get; set; }

        public EmailDBContact(DbContextOptions<EmailDBContact> options) : base(options)
        {

        }

        /*protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySql("Server=localhost;Port=3306;Database=emaildb;Uid=root;Pwd=linh1102;",
                new MySqlServerVersion(new Version(8, 0, 41)));
        }*/

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Email>(entity =>
            {
                entity.Property(e => e.Sender).HasMaxLength(255);
                entity.Property(e => e.Recipient).HasMaxLength(255);
                entity.Property(e => e.Subject).HasMaxLength(255);

            }

            );
        }
    }
}