using API_Commande.Context;
using API_Commande.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API_Commande.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommandeController : ControllerBase
    {
        private readonly CommandeContext _context;
        //private readonly CommandeService _commandeService;

        public CommandeController(CommandeContext context)
        {
            _context = context;
            //_commandeService = commandeService;
        }

        // GET: api/orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Commande>>> GetOrders()
        {
            return await _context.Orders.ToListAsync();
        }

        // GET: api/orders/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Commande>> GetOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound(new { message = "Commande non trouvée." });
            }

            return order;
        }

        // GET: api/orders/{id}
        [HttpGet("client/{id}")]
        public async Task<ActionResult<Commande>> GetOrderByClientId(int id)
        {
            var orders = await _context.Orders.Where(order => order.ClientID == id).ToListAsync();

            // Vérifier si des commandes existent pour ce client
            if (orders == null || !orders.Any())
            {
                return NotFound(new { message = "Aucune commande trouvée pour ce client." });
            }

            return Ok(orders);
        }

       /* [HttpGet("client/{id}/produits")]
        public async Task<IActionResult> GetOrderByClientIdWithProducts(int id)
        {
            var commandes = await _context.Orders.Where(order => order.ClientID == id).ToListAsync();
            if (commandes == null)
            {
                return NotFound(new { message = "Client non trouvé." });
            }

            var commandesAvecProduits = new List<object>();
            foreach (var commande in commandes)
            {
                var produits = await _commandeService.GetProduitsByIds(commande.ProduitIDs);

                // Ajouter la commande et ses produits à la liste
                commandesAvecProduits.Add(new
                {
                    Commande = commande,
                    Produits = produits
                });
            }
            

            // Retourner le client avec ses commandes
            return Ok(commandesAvecProduits);
        }

        [HttpGet("{id}/produits")]
        public async Task<IActionResult> GetClientWithOrders(int id)
        {
            // Récupérer la commande
            var commande = await _context.Orders.FindAsync(id);
            if (commande == null)
            {
                return NotFound(new { message = "Client non trouvé." });
            }

            // Appeler l'API_Commande pour récupérer les commandes liées à ce client
            var produits = await _commandeService.GetProduitsByIds(commande.ProduitIDs);
            if (produits == null || !produits.Any())
            {
                return NotFound(new { message = "Aucune commande trouvée pour ce client." });
            }

            // Retourner le client avec ses commandes
            return Ok(new
            {
                Commande = commande,
                Produits = produits
            });
        }*/


        // POST: api/orders
        [HttpPost]
        public async Task<ActionResult<Commande>> PostOrder(Commande order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        // PUT: api/orders/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Commande order)
        {
            if (id != order.Id)
            {
                return BadRequest(new { message = "L'ID de la commande ne correspond pas à l'ID dans l'URL." });
            }

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound(new { message = "Commande non trouvée." });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new { message = "Commande mise à jour avec succès." });
        }

        // DELETE: api/orders/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = "Commande non trouvée." });
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Commande supprimée avec succès." });
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
