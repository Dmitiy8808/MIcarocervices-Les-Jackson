using System.Security.Cryptography.X509Certificates;
using Microsoft.EntityFrameworkCore;
using PlatformService.Models;

namespace PlatformService.Data
{
    public static class PrepDb
    {
        public static void PrepPopulation(IApplicationBuilder app, bool isProduction)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetService<AppDbContext>();
                if (context == null)
                {
                    throw new InvalidOperationException("AppDbContext is not registered in the service provider.");
                }
                SeedData(context, isProduction);
            }
        }

        private static void SeedData(AppDbContext context, bool isProduction)
        {
            if(isProduction)
            {
                Console.WriteLine("--> Attempting to apply Migrations...");
                
                try
                {
                    context.Database.Migrate();
    
                }
                catch (Exception ex)
                {
                    
                    Console.WriteLine($"--> Could not run migrations: {ex.Message}");
                }
            }
            if (!context.Platforms.Any())
            {
                Console.WriteLine("--> Seeding Data...");

                context.Platforms.AddRange(
                    new Platform() {Name = "Dot net", Publisher = "Microsoft", Cost = "Free"},
                    new Platform() {Name = "Sql Server Express", Publisher = "Microsoft", Cost = "Free"},
                    new Platform() {Name = "Kubernetes", Publisher = "Cloud Native", Cost = "Free"}
                );

                context.SaveChanges();
            }
            else
            {
                Console.WriteLine("--> We already have data");
            }
        }
    }

}