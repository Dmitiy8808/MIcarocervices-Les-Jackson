using AutoMapper;
using CommandsService.Models;
using Grpc.Core;
using Grpc.Net.Client;
using PlatformService;

namespace CommandsService.SyncDataServices.Grpc
{
    public class PlatformDataClient : IPlatformDataClient
    {
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public PlatformDataClient(IConfiguration configuration, IMapper mapper)
        {
            _mapper = mapper;
            _configuration = configuration;

        }
        public IEnumerable<Platform> ReturnAllPlatforms()
        {
            var grpcPlatform = _configuration["GrpcPlatform"];

            if (grpcPlatform is null)
            {
                throw new InvalidOperationException("Grpc configuration is missing.");
            }
            Console.WriteLine($"--> Calling GRPC Service {grpcPlatform}");

            var channel = GrpcChannel.ForAddress(grpcPlatform);
            // var channel = GrpcChannel.ForAddress(grpcPlatform, new GrpcChannelOptions
            // {
            //     HttpHandler = new SocketsHttpHandler
            //     {
            //         EnableMultipleHttp2Connections = true
            //     },
            //     Credentials = ChannelCredentials.Insecure
            // });

            var client = new GrpcPlatform.GrpcPlatformClient(channel);

            var request = new GetAllRequest();

            try
            {
                var reply = client.GetAllPlatforms(request);

                return _mapper.Map<IEnumerable<Platform>>(reply.Platform);
            }
            catch (Exception ex)
            {

                Console.WriteLine($"--> Cuoldnot call GRPC Server {ex.Message}");
                return Enumerable.Empty<Platform>();
            }
        }
    }
}