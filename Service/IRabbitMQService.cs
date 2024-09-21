
using API_Commande.Models;

public interface IRabbitMQService
{
    void SendMessage(string message);
    void CreateConsumer();
    void ReceiveMessage(string message);
    Task<List<Dictionary<string, object>>> GetProduitsByIds(List<Commande> commandes);
    void CreateConsumerCommandeID();
    void CreateConsumerCommandeIDProduits();
}
