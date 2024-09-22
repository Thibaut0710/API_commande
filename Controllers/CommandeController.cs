using API_Commande.Context;
using API_Commande.Models;
using API_Commande.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_Commande.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CommandeController : ControllerBase
    {
        private readonly CommandeContext _context;
        private readonly CommandeService _commandeService;

        public CommandeController(CommandeContext context, CommandeService commandeService)
        {
            _context = context;
            _commandeService = commandeService;
        }

        // GET: api/orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Commande>>> GetOrders()
        {
            return await _context.Orders.AsNoTracking().ToListAsync();
        }

        // GET: api/orders/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Commande>> GetOrder(int id)
        {
            var order = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound(new { message = "Commande non trouvée." });
            }

            return Ok(order);
        }

        // GET: api/orders/client/{id}
        [HttpGet("client/{id}")]
        public async Task<ActionResult<IEnumerable<Commande>>> GetOrderByClientId(int id)
        {
            var orders = await _context.Orders.AsNoTracking()
                .Where(order => order.ClientID == id).ToListAsync();

            if (!orders.Any())
            {
                return NotFound(new { message = "Aucune commande trouvée pour ce client." });
            }

            return Ok(orders);
        }


        [HttpGet("client/{id}/produits")]
        public async Task<IActionResult> GetOrderByClientIdWithProducts(int id)
        {
            var commandes = await _context.Orders.Where(order => order.ClientID == id).ToListAsync();
            if (commandes == null  || !commandes.Any())
            {
                return NotFound(new { message = "Client non trouvé." });
            }

            var commandesAvecProduits = await _commandeService.GetProduitsByIds(commandes);


            // Retourner le client avec ses commandes
            return Ok(commandesAvecProduits);
        }

        // GET: api/orders/{id}/produits
        [HttpGet("{id}/produits")]
        public async Task<IActionResult> GetOrderWithProducts(int id)
        {
            var commandes = await _context.Orders.Where(order => order.Id == id).ToListAsync();
            if (commandes == null || !commandes.Any())
            {
                return NotFound(new { message = "Commande non trouvée." });
            }

            try
            {
                var response = await _commandeService.GetProduitsByIds(commandes);
                if (response == null || !response.Any())
                {
                    return NotFound(new { message = "Aucun produit trouvé pour cette commande." });
                }

                return Ok(new
                {
                    Commande = response
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la récupération des produits.", detail = ex.Message });
            }
        }

        // POST: api/orders
        [HttpPost]
        public async Task<ActionResult<Commande>> PostOrder([FromBody] Commande order)
        {
            if (order == null)
            {
                return BadRequest(new { message = "Les informations de la commande ne peuvent pas être nulles." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }

        // PUT: api/orders/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, [FromBody] Commande order)
        {
            if (id != order.Id)
            {
                return BadRequest(new { message = "L'ID de la commande ne correspond pas à l'ID dans l'URL." });
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la mise à jour de la commande.", detail = ex.Message });
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
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la suppression de la commande.", detail = ex.Message });
            }

            return Ok(new { message = "Commande supprimée avec succès." });
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
    }
}
