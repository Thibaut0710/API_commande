public interface IRabbitMQService
{
    void SendMessage(string message);
    void CreateConsumer();
    void ReceiveMessage(string message);
}