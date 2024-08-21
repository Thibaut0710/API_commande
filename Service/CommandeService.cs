using API_Commande.Context;
using API_Commande.Models;
using API_produit.Models;
using System.Text.Json;

namespace API_Commande.Service
{
    public class CommandeService
    {
        private readonly HttpClient _httpClient;

        public CommandeService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Produit>> GetProduitsByIds(List<int> produitIds)
        {
            try
            {
                var produitIdsString = string.Join("&", produitIds.Select(id => $"produitsId={id}"));
                var response = await _httpClient.GetAsync($"Produit/produitInCommande?{produitIdsString}");

                response.EnsureSuccessStatusCode();

                // Lire le contenu JSON
                var content = await response.Content.ReadAsStringAsync();
                var produits = JsonSerializer.Deserialize<List<Produit>>(content, new JsonSerializerOptions
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
    }

}
