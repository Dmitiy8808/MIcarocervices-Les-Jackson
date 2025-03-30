using CommandsService.Models;
using CommandsService.SyncDataServices.Grpc;

namespace CommandsService.Data
{
    public class PrepDb
    {
        public static void PrepPopulation(IApplicationBuilder applicationBuilder)
        {
            using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
            {
                var grpcClient = serviceScope.ServiceProvider.GetService<IPlatformDataClient>();

                if (grpcClient is null)
                {
                    Console.WriteLine("IPlatformDataClient is not registered in the service container.");
                    return;
                }

                var platforms = grpcClient.ReturnAllPlatforms();

                var commandRepo = serviceScope.ServiceProvider.GetService<ICommandRepo>();

                if (commandRepo is null)
                {
                    Console.WriteLine("ICommandRepo is not registered in the service container.");
                    return; 
                }

                SeedData(commandRepo, platforms);
            }
        }

        private static void SeedData(ICommandRepo repo, IEnumerable<Platform> platforms)
        {
            Console.WriteLine("--> Seeding new platforms...");

            foreach (var platform in platforms)
            {
                if(!repo.ExternalPlatformExists(platform.ExternalId))
                {
                    repo.CreatePlatform(platform);
                }

                repo.SaveChanges();
            }
        }
    }
}