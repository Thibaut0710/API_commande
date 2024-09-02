public interface IRabbitMQService
{
    void SendMessage(string message);
    void CreateConsumer();
    void ReceiveMessage(string message);
    void CreateConsumerCommandeID();
    void CreateConsumerCommandeIDProduits();
    //Task<List<Produit>> GetProduitsByIds(List<int> produitIds);
}