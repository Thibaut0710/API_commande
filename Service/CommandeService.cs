using API_produit.Models;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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
                // Créer la chaîne de requête avec les IDs des produits
                var produitIdsString = string.Join("&", produitIds.Select(id => $"produitsId={id}"));
                var response = await _httpClient.GetAsync($"Produit/produitInCommande?{produitIdsString}");

                response.EnsureSuccessStatusCode(); // Lève une exception si le code de statut est en erreur

                var content = await response.Content.ReadAsStringAsync();
                var produits = JsonSerializer.Deserialize<List<Produit>>(content, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                return produits ?? new List<Produit>(); // Retourner une liste vide si la désérialisation échoue
            }
            catch (HttpRequestException httpEx)
            {
                throw new Exception("Erreur HTTP lors de la récupération des produits", httpEx);
            }
            catch (Exception ex)
            {
                throw new Exception("Erreur lors de la récupération des produits", ex);
            }
        }
    }
}
