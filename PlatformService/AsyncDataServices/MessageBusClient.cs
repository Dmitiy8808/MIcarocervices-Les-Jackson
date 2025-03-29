using System.Text;
using System.Text.Json;
using PlatformService.Dtos;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace PlatformService.AsyncDataServices
{
    public class MessageBusClient : IMessageBusClient
    {
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;

        public MessageBusClient(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task InitAsync()
        {
            var host = _configuration["RabbitMQHost"];
            var portStr = _configuration["RabbitMQPort"];

            if (host is null || portStr is null)
            {
                throw new InvalidOperationException("RabbitMQ configuration is missing.");
            }

            var factory = new ConnectionFactory()
            {
                HostName = host,
                Port = int.Parse(portStr)
            };

            try
            {
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
                await _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout);
                _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;

                Console.WriteLine("--> Connected to MessageBus");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> Could not connect to the message bus: {ex.Message}");
            }
        }

        private Task RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("--> RubbitMQ Connection Shutdown");
            return Task.CompletedTask;
        }

        public async Task PublishNewPlatform(PlatformPublishedDto platformPublishedDto)
        {
            var message = JsonSerializer.Serialize(platformPublishedDto);

            if (_connection != null && _connection.IsOpen)
            {
                Console.WriteLine("--> RubbitMQ Connection open, sendig message...");
                await SendMessage(message);
            }
            else
            {
                Console.WriteLine("--> RubbitMQ Connection closed, not sendig");
            }
        }

        private async Task SendMessage(string message)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                Console.WriteLine("--> Cannot send message: channel is not initialized or closed");
                return;
            }

            var body = Encoding.UTF8.GetBytes(message);

            var props = new BasicProperties();
            await _channel.BasicPublishAsync(exchange: "trigger",
                                    routingKey: "",
                                    mandatory: false,
                                    basicProperties: props,
                                    body: body);

            Console.WriteLine($"--> We have sent {message}");
        }

        public async Task Dispose()
        {
            Console.WriteLine("Message Bus Disposed");

            if (_channel != null && _channel.IsOpen)
            {
                await _channel.CloseAsync();
            }

            if (_connection != null && _connection.IsOpen)
            {
                await _connection.CloseAsync();
            }
        }
    }

}