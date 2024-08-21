using API_Commande.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

public class RabbitMQService : IRabbitMQService
{
    private readonly string _hostName;
    private readonly string _queueName;
    private readonly string _userName;
    private readonly string _password;
    private readonly IServiceScopeFactory _scopeFactory;

    public RabbitMQService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _hostName = configuration["RabbitMQ:HostName"];
        _queueName = configuration["RabbitMQ:QueueName"];
        _userName = configuration["RabbitMQ:UserName"];
        _password = configuration["RabbitMQ:Password"];
        _scopeFactory = scopeFactory;
    }

    public void SendMessage(string message)
    {
        var factory = new ConnectionFactory() { HostName = _hostName, UserName = _userName, Password = _password };

        using (var connection = factory.CreateConnection())
        using (var channel = connection.CreateModel())
        {
            channel.QueueDeclare(queue: "Channel_Client", durable: true, exclusive: false, autoDelete: false, arguments: null);

            var body = Encoding.UTF8.GetBytes(message);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = false;

            channel.BasicPublish(exchange: string.Empty,
                                 routingKey: "Channel_Client",
                                 basicProperties: properties,
                                 body: body);
            Console.WriteLine($" [x] Sent {message}");
        }
    }

    public void CreateConsumer()
    {
        Console.WriteLine("Create Consumer");
        Task.Run(() =>
        {
            var factory = new ConnectionFactory() { HostName = _hostName, UserName = _userName, Password = _password };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "Channel_Commande", durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                Console.WriteLine(" [*] Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    byte[] body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    using (JsonDocument doc = JsonDocument.Parse(message))
                    {
                        JsonElement root = doc.RootElement;

                        if (root.TryGetProperty("Id", out JsonElement idElement))
                        {
                            int id = idElement.GetInt32();
                            Console.WriteLine($"Id: {id}");

                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var context = scope.ServiceProvider.GetRequiredService<CommandeContext>();
                                var orders = await context.Orders.Where(order => order.ClientID == id).ToListAsync();
                                string json = JsonSerializer.Serialize(orders);

                                // Créer les propriétés du message de réponse avec le CorrelationId d'origine
                                var replyProperties = channel.CreateBasicProperties();
                                replyProperties.CorrelationId = ea.BasicProperties.CorrelationId; // Utilisation du CorrelationId d'origine

                                // Envoyer la réponse à la queue de réponse (Channel_Client)
                                channel.BasicPublish(exchange: string.Empty,
                                                     routingKey: ea.BasicProperties.ReplyTo, // La queue de réponse est spécifiée dans le message original
                                                     basicProperties: replyProperties,
                                                     body: Encoding.UTF8.GetBytes(json));

                                Console.WriteLine($"Processed {orders.Count} orders for client ID: {id}");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Propriété 'Id' non trouvée.");
                        }
                    }

                    Console.WriteLine($" [x] Received {message}");

                    // Acknowledge the message
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                channel.BasicConsume(queue: "Channel_Commande",
                                     autoAck: false,
                                     consumer: consumer);

                // Keep the thread alive while consuming messages
                while (true)
                {
                    // Add any additional logic needed to keep the consumer alive,
                    // such as waiting for cancellation tokens or other termination signals.
                }
            }
        });
    }

    public void ReceiveMessage(string message)
    {
        // Cette méthode est appelée pour traiter les messages reçus de RabbitMQ
        Console.WriteLine($"Received message from RabbitMQ: {message}");
        // Vous pouvez ajouter ici le traitement nécessaire pour le message reçu
    }
}
