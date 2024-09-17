
public interface IRabbitMQService
{
    void SendMessage(string message);
    void CreateConsumer();
    void ReceiveMessage(string message);
    Task<List<Dictionary<string, object>>> GetProduitsByIds(List<int> produitIds);
    void CreateConsumerCommandeID();
    void CreateConsumerCommandeIDProduits();
}
