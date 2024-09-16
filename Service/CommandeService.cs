using API_Commande.Context;
using API_Commande.Models;
using System.Text.Json;

namespace API_Commande.Service
{
    public class CommandeService
    {
        private readonly IRabbitMQService _rabbitMQService;

        public CommandeService(IRabbitMQService rabbitMQService)
        {
            _rabbitMQService = rabbitMQService;
        }

        public async Task<List<Dictionary<string, object>>> GetProduitsByIds(List<int> produitIds)
        {
            var response = await _rabbitMQService.GetProduitsByIds(produitIds);
            return response;
        }
    }

}
