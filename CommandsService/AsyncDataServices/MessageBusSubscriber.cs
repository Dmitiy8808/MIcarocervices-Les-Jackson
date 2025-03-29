using System.Text;
using CommandsService.EventProcessing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CommandsService.AsyncDataServices
{
    public class MessageBusSubscriber : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly IEventProcessor _eventProcessor;
        private IConnection? _connection;
        private IChannel? _channel;
        private string? _queueName;

        public MessageBusSubscriber(IConfiguration configuration, IEventProcessor eventProcessor)
        {
            _eventProcessor = eventProcessor;
            _configuration = configuration;

        }

        private async Task InitializeRabbitMQ()
        {
            var host = _configuration["RabbitMQHost"];
            var portStr = _configuration["RabbitMQPort"];

            if (host is null || portStr is null)
            {
                throw new InvalidOperationException("RabbitMQ configuration is missing.");
            }
            var factory = new ConnectionFactory() { HostName = host, Port = int.Parse(portStr) };

            _connection = await factory.CreateConnectionAsync();
            _channel = await _connection.CreateChannelAsync();

            // Создаём Exchange
            await _channel.ExchangeDeclareAsync(exchange: "trigger", type: ExchangeType.Fanout);

            // Объявляем очередь (без имени — сервер сам задаст имя)
            var queueDeclareResult = await _channel.QueueDeclareAsync(
                queue: "",
                durable: false,
                exclusive: true,
                autoDelete: true
            );

            // Сохраняем имя очереди
            _queueName = queueDeclareResult.QueueName;

            // Привязываем очередь к exchange
            await _channel.QueueBindAsync(
                queue: _queueName,
                exchange: "trigger",
                routingKey: ""
            );

            Console.WriteLine("--> Listening on the Message Bus...");

            _connection.ConnectionShutdownAsync += RabbitMQ_ConnectionShutdown;

        }

        private Task RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e)
        {
            Console.WriteLine("--> RubbitMQ Connection Shutdown");
            return Task.CompletedTask;
        }

        protected override async Task<Task> ExecuteAsync(CancellationToken stoppingToken)
        {
            await InitializeRabbitMQ();

            stoppingToken.ThrowIfCancellationRequested();

            if (_channel != null && _queueName != null)
            {
                var consumer = new AsyncEventingBasicConsumer(_channel);

                consumer.ReceivedAsync += async (model, ea) =>
                {
                    Console.WriteLine("--> Event Received!");

                    var body = ea.Body.ToArray();
                    var notificationMessage = Encoding.UTF8.GetString(body);

                    // Обработка события через твой IEventProcessor
                    _eventProcessor.ProcessEvent(notificationMessage);

                    await Task.CompletedTask; // для совместимости с async consumer
                };

                await _channel.BasicConsumeAsync(
                        queue: _queueName,
                        autoAck: true,
                        consumer: consumer,
                        cancellationToken: stoppingToken
                    );
            }

            return Task.CompletedTask;
        }

        public async ValueTask DisposeAsync()
        {
            Console.WriteLine("Message Bus Disposed");

            if (_channel?.IsOpen == true)
                await _channel.CloseAsync();

            if (_connection?.IsOpen == true)
                await _connection.CloseAsync();
        }
    }
}