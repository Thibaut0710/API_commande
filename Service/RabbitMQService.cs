using API_Commande.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

public class RabbitMQService : IRabbitMQService
{
    private readonly string _hostName;
    private readonly string _queueName;
    private readonly string _userName;
    private readonly string _password;
    private readonly IServiceScopeFactory _scopeFactory;
    private static ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingMessages = new ConcurrentDictionary<string, TaskCompletionSource<string>>();
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMQService(IConfiguration configuration, IServiceScopeFactory scopeFactory)
    {
        _hostName = configuration["RabbitMQ:HostName"];
        _queueName = configuration["RabbitMQ:QueueName"];
        _userName = configuration["RabbitMQ:UserName"];
        _password = configuration["RabbitMQ:Password"];
        _scopeFactory = scopeFactory;

        var factory = new ConnectionFactory() { HostName = _hostName, UserName = _userName, Password = _password };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
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

    public void CreateConsumerCommandeID()
    {
        Console.WriteLine("CreateConsumerCommandeID");
        Task.Run(() =>
        {
            var factory = new ConnectionFactory() { HostName = _hostName, UserName = _userName, Password = _password };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "channelCommmandeClient", durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                Console.WriteLine(" [*] Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    // Log the received message
                    Console.WriteLine($" [x] Received {ea.DeliveryTag}");
                    byte[] body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"Message: {message}");

                    try
                    {
                        // Parse the received message
                        using (JsonDocument doc = JsonDocument.Parse(message))
                        {
                            JsonElement root = doc.RootElement;

                            if (root.TryGetProperty("clientId", out JsonElement clientIdElement) &&
                                root.TryGetProperty("commandeId", out JsonElement commandeIdElement))
                            {
                                int clientId = clientIdElement.GetInt32();
                                int commandeId = commandeIdElement.GetInt32();
                                Console.WriteLine($"clientId: {clientId}, commandeId: {commandeId}");

                                // Process the request asynchronously
                                string jsonResponse = await ProcessCommandeAsync(clientId, commandeId);

                                // Send the response to the reply queue
                                SendReply(channel, ea, jsonResponse);
                            }
                            else
                            {
                                Console.WriteLine("Propriétés 'clientId' et/ou 'commandeId' non trouvées.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors du traitement de la requête: {ex.Message}");
                    }

                    // Acknowledge the message after processing
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                channel.BasicConsume(queue: "channelCommmandeClient",
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


    public void CreateConsumerCommandeIDProduits()
    {
        Console.WriteLine("channelCommandeClientProduit");
        Task.Run(() =>
        {
            var factory = new ConnectionFactory() { HostName = _hostName, UserName = _userName, Password = _password };

            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: "channelCommandeClientProduit", durable: true, exclusive: false, autoDelete: false, arguments: null);
                channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                Console.WriteLine(" [*] Waiting for messages.");

                var consumer = new EventingBasicConsumer(channel);
                consumer.Received += async (model, ea) =>
                {
                    // Log the received message
                    Console.WriteLine($" [x] Received {ea.DeliveryTag}");
                    byte[] body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    Console.WriteLine($"Message: {message}");

                    try
                    {
                        // Parse the received message
                        using (JsonDocument doc = JsonDocument.Parse(message))
                        {
                            JsonElement root = doc.RootElement;

                            if (root.TryGetProperty("clientId", out JsonElement clientIdElement) &&
                                root.TryGetProperty("commandeId", out JsonElement commandeIdElement))
                            {
                                int clientId = clientIdElement.GetInt32();
                                int commandeId = commandeIdElement.GetInt32();
                                Console.WriteLine($"clientId: {clientId}, commandeId: {commandeId}");

                                // Process the request asynchronously
                                string jsonOrders = await ProcessCommandeAsync(clientId, commandeId);
                                var response = await SendMessageAndWaitForResponseAsync(jsonOrders, "produitInCommande", "commandeProduitReply");
                                // Send the response to the reply queue
                                SendReply(channel, ea, response);
                            }
                            else
                            {
                                Console.WriteLine("Propriétés 'clientId' et/ou 'commandeId' non trouvées.");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors du traitement de la requête: {ex.Message}");
                    }

                    // Acknowledge the message after processing
                    channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                };

                channel.BasicConsume(queue: "channelCommandeClientProduit",
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

    // Method to process the request and fetch the necessary data
    private async Task<string> ProcessCommandeAsync(int clientId, int commandeId)
    {
        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CommandeContext>();

            // Filtrer les commandes en fonction du clientId et du commandeId
            var orders = await context.Orders
                                      .Where(order => order.ClientID == clientId && order.Id == commandeId)
                                      .ToListAsync();

            if (!orders.Any()) // Vérifie si la liste est vide
            {
                return "Cette commande n'existe pas.";
            }

            return JsonSerializer.Serialize(orders);
        }
    }


    public async Task<List<Dictionary<string, object>>> GetProduitsByIds(List<int> produitIds)
    {
        Console.WriteLine("------------------------------------------3------------------------------------------------------------------------------------");
        try
        {
            var jsonString = JsonSerializer.Serialize(produitIds);
            Console.WriteLine(jsonString);
            var response = await SendMessageAndWaitForResponseAsync(jsonString, "produitInCommande", "channel_commande_produit_details");
            Console.WriteLine("RESPONSE", response);

            var produits = JsonSerializer.Deserialize<List<Dictionary<string, object>>>(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
            return produits;
        }
        catch (Exception ex)
        {
            // Gérer les erreurs éventuelles
            throw new Exception("Erreur lors de la récupération des produits", ex);
        }
    }

    // Method to send the reply after processing
    private void SendReply(IModel channel, BasicDeliverEventArgs ea, string jsonResponse)
    {
        // Create reply properties with the original CorrelationId
        var replyProperties = channel.CreateBasicProperties();
        replyProperties.CorrelationId = ea.BasicProperties.CorrelationId;
        Console.WriteLine(ea.BasicProperties.ReplyTo);
        // Publish the reply to the specified reply queue
        channel.BasicPublish(exchange: string.Empty,
                             routingKey: ea.BasicProperties.ReplyTo, // La queue de réponse est spécifiée dans le message original
                             basicProperties: replyProperties,
                             body: Encoding.UTF8.GetBytes(jsonResponse));

        Console.WriteLine($" [x] Replied with processed data for CorrelationId: {ea.BasicProperties.CorrelationId}");
    }

    public async Task<string> SendMessageAndWaitForResponseAsync(string message, string CommandQueueName, string ReplyQueueName)
    {
        Console.WriteLine(message);
        _channel.QueueDeclare(queue: ReplyQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
        await Task.Delay(100);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += OnResponseReceived;
        _channel.BasicConsume(consumer: consumer, queue: ReplyQueueName, autoAck: true);
        Console.WriteLine(ReplyQueueName);


        var correlationId = Guid.NewGuid().ToString();
        var tcs = new TaskCompletionSource<string>();

        _pendingMessages[correlationId] = tcs;

        var properties = _channel.CreateBasicProperties();
        properties.CorrelationId = correlationId;
        properties.ReplyTo = ReplyQueueName;

        var body = Encoding.UTF8.GetBytes(message);
        Console.WriteLine(CommandQueueName);
        _channel.BasicPublish(exchange: string.Empty,
                             routingKey: CommandQueueName, // Utilisation de la queue de commande en dur
                             basicProperties: properties,
                             body: body);

        Console.WriteLine($" [x] Sent {message} with CorrelationId {correlationId}");

        // Attendre de manière asynchrone que la réponse arrive
        return await tcs.Task;
    }
    private void OnResponseReceived(object sender, BasicDeliverEventArgs ea)
    {
        var correlationId = ea.BasicProperties?.CorrelationId;

        if (string.IsNullOrEmpty(correlationId))
        {
            Console.WriteLine("Received a message without a CorrelationId or with a null CorrelationId.");
            return;
        }

        var response = Encoding.UTF8.GetString(ea.Body.ToArray());

        Console.WriteLine($" [x] Received {response} with CorrelationId {correlationId}");

        // Vérifier si le message attendu est dans la liste
        if (_pendingMessages.TryRemove(correlationId, out var tcs))
        {
            if (tcs != null)
            {
                tcs.SetResult(response); // Renvoie la réponse à la méthode appelante
            }
            else
            {
                Console.WriteLine($"TaskCompletionSource for CorrelationId {correlationId} is null.");
            }
        }
        else
        {
            Console.WriteLine($"No pending message found for CorrelationId {correlationId}.");
        }
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }

    public void ReceiveMessage(string message)
    {
        // Cette méthode est appelée pour traiter les messages reçus de RabbitMQ
        Console.WriteLine($"Received message from RabbitMQ: {message}");
        // Vous pouvez ajouter ici le traitement nécessaire pour le message reçu
    }
}
