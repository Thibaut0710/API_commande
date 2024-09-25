using API_Commande.Context;
using API_Commande.Controllers;
using API_Commande.Models;
using API_Commande.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace API_Commande.Tests
{
    public class CommandeControllerTests
    {
        private readonly CommandeController _controller;
        private readonly Mock<CommandeService> _commandeServiceMock;
        private readonly CommandeContext _context;

        public CommandeControllerTests()
        {
            var options = new DbContextOptionsBuilder<CommandeContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase" + Guid.NewGuid())
                .Options;
            _context = new CommandeContext(options);

            var rabbitMQServiceMock = new Mock<IRabbitMQService>();
            _commandeServiceMock = new Mock<CommandeService>(rabbitMQServiceMock.Object);
            _controller = new CommandeController(_context, _commandeServiceMock.Object);
        }

        private void SeedData()
        {
            _context.Orders.AddRange(
                new Commande { Id = 1, CustomerName = "Client 1", OrderDate = DateTime.Now.AddDays(-1), TotalAmount = 100, ClientID = 1, ProduitIDs = new List<int> { 1 } },
                new Commande { Id = 2, CustomerName = "Client 2", OrderDate = DateTime.Now.AddDays(-2), TotalAmount = 200, ClientID = 1, ProduitIDs = new List<int> { 2 } },
                new Commande { Id = 3, CustomerName = "Client 3", OrderDate = DateTime.Now.AddDays(-3), TotalAmount = 300, ClientID = 2, ProduitIDs = new List<int> { 3 } }
            );
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetOrders_ReturnsAllOrders()
        {
            // Arrange
            SeedData();

            // Act
            var result = await _controller.GetOrders();

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<Commande>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var orders = Assert.IsType<List<Commande>>(okResult.Value);
            Assert.Equal(3, orders.Count); // Il devrait y avoir 3 commandes dans la base de données.
        }

        [Fact]
        public async Task GetOrder_ExistingId_ReturnsOrder()
        {
            // Arrange
            SeedData();
            int testOrderId = 1;

            // Act
            var result = await _controller.GetOrder(testOrderId);

            // Assert
            var actionResult = Assert.IsType<ActionResult<Commande>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var order = Assert.IsType<Commande>(okResult.Value);
            Assert.Equal(testOrderId, order.Id);
        }

        [Fact]
        public async Task GetOrder_NonExistingId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetOrder(99); // Id n'existant pas

            // Assert
            var actionResult = Assert.IsType<ActionResult<Commande>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetOrderByClientId_ExistingClientId_ReturnsOrders()
        {
            // Arrange
            SeedData();
            int clientId = 1;

            // Act
            var result = await _controller.GetOrderByClientId(clientId);

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<Commande>>>(result);
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var orders = Assert.IsType<List<Commande>>(okResult.Value);
            Assert.Equal(2, orders.Count); // Le client 1 a 2 commandes.
        }

        [Fact]
        public async Task GetOrderByClientId_NonExistingClientId_ReturnsNotFound()
        {
            // Act
            var result = await _controller.GetOrderByClientId(99); // Client n'existant pas

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<Commande>>>(result);
            Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetOrderByClientIdWithProducts_ValidClient_ReturnsOrdersWithProducts()
        {
            // Arrange
            SeedData(); // Assure-toi que cette méthode charge les données nécessaires pour les tests
            int clientId = 1;

            var commandes = new List<Commande>
            {
                new Commande { Id = 1, CustomerName = "Client 1", OrderDate = DateTime.Now, TotalAmount = 100.00m, ClientID = clientId, ProduitIDs = new List<int> { 1, 2 } }
            };

                    var produitDictionaryList = new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { "Id", 1 },
                    { "Name", "Produit 1" },
                    { "Price", 10.00m }
                },
                new Dictionary<string, object>
                {
                    { "Id", 2 },
                    { "Name", "Produit 2" },
                    { "Price", 20.00m }
                }
            };

            _commandeServiceMock
                .Setup(service => service.GetProduitsByIds(It.IsAny<List<Commande>>()))
                .ReturnsAsync(produitDictionaryList);

            // Act
            var result = await _controller.GetOrderByClientIdWithProducts(clientId);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result);
            var ordersWithProducts = Assert.IsType<List<Dictionary<string, object>>>(actionResult.Value);

            // Vérification de la structure des données retournées
            Assert.NotEmpty(ordersWithProducts);
            Assert.Equal(2, ordersWithProducts.Count); // Vérifie le nombre de produits
            Assert.Equal("Produit 1", ordersWithProducts[0]["Name"]); // Vérifie le nom du premier produit
            Assert.Equal("Produit 2", ordersWithProducts[1]["Name"]); // Vérifie le nom du deuxième produit
        }


        [Fact]
        public async Task PostOrder_ValidOrder_ReturnsCreatedOrder()
        {
            // Arrange
            var newOrder = new Commande
            {
                Id = 4,
                CustomerName = "Client 4",
                OrderDate = DateTime.Now.AddDays(-1),
                TotalAmount = 150,
                ClientID = 3,
                ProduitIDs = new List<int> { 4 }
            };

            // Act
            var result = await _controller.PostOrder(newOrder);

            // Assert
            var actionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdOrder = Assert.IsType<Commande>(actionResult.Value);
            Assert.Equal(newOrder.CustomerName, createdOrder.CustomerName);
        }

        [Fact]
        public async Task PutOrder_ValidOrder_ReturnsOk()
        {
            // Arrange
            SeedData();

            // Détacher la commande existante si elle est déjà suivie
            var existingOrder = await _context.Orders.FindAsync(1);
            if (existingOrder != null)
            {
                _context.Entry(existingOrder).State = EntityState.Detached;
            }

            // Préparer l'ordre mis à jour
            var updatedOrder = new Commande
            {
                Id = 1,
                CustomerName = "Client 1 Modifié",
                OrderDate = DateTime.Now.AddDays(-1),
                TotalAmount = 120,
                ClientID = 1,
                ProduitIDs = new List<int> { 1, 2 }
            };

            // Act
            var result = await _controller.PutOrder(1, updatedOrder);

            // Assert
            var actionResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Commande mise à jour avec succès.", ((dynamic)actionResult.Value).message);
        }


        [Fact]
public async Task DeleteOrder_ExistingOrder_ReturnsOk()
{
    // Arrange
    SeedData();

    // Act
    var result = await _controller.DeleteOrder(1);

    // Assert
    var actionResult = Assert.IsType<OkObjectResult>(result);
    Assert.Equal("Commande supprimée avec succès.", ((dynamic)actionResult.Value).message);
}

    }
}
