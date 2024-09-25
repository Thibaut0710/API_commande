using Moq;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API_Commande.Models;
using API_Commande.Service;
using API_Commande.Controllers;
using API_Commande.Context;
using Microsoft.EntityFrameworkCore;

namespace YourNamespace.Tests
{
    public class CommandeControllerTests
    {
        private readonly Mock<CommandeContext> _mockContext;
        private readonly Mock<CommandeService> _mockService;
        private readonly CommandeController _controller;

        public CommandeControllerTests()
        {
            _mockContext = new Mock<CommandeContext>();
            _mockService = new Mock<CommandeService>();

            _controller = new CommandeController(_mockContext.Object, _mockService.Object);
        }

        [Fact]
        public async Task GetOrders_ReturnsOrdersList()
        {
            var mockOrders = new List<Commande>
            {
                new Commande { Id = 1, CustomerName = "Jean JAQUES", OrderDate = new DateTime(1389102), TotalAmount = 10000, ClientID = 19, ProduitIDs = new List<int>() { 3 } },
                new Commande { Id = 2, CustomerName = "Daniel DUPONT", OrderDate = new DateTime(13893202), TotalAmount = 50, ClientID = 32, ProduitIDs = new List<int>() { 1, 2, 8 } }
            }.AsQueryable();

            var mockSet = new Mock<DbSet<Commande>>();
            mockSet.As<IQueryable<Commande>>().Setup(m => m.Provider).Returns(mockOrders.Provider);
            mockSet.As<IQueryable<Commande>>().Setup(m => m.Expression).Returns(mockOrders.Expression);
            mockSet.As<IQueryable<Commande>>().Setup(m => m.ElementType).Returns(mockOrders.ElementType);
            mockSet.As<IQueryable<Commande>>().Setup(m => m.GetEnumerator()).Returns(mockOrders.GetEnumerator());

            _mockContext.Setup(c => c.Orders).Returns(mockSet.Object);

            var result = await _controller.GetOrders() as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            var orders = result.Value as IEnumerable<Commande>;
            Assert.Equal(2, orders.Count());
        }

        [Fact]
        public async Task GetOrder_OrderExists_ReturnsOrder()
        {
            var mockOrder = new Commande { Id = 1, CustomerName = "Jean JAQUES", OrderDate = new DateTime(1389102), TotalAmount = 10000, ClientID = 19, ProduitIDs = new List<int>() { 3 } };
            var mockSet = new Mock<DbSet<Commande>>();
            mockSet.Setup(m => m.FindAsync(1)).ReturnsAsync(mockOrder);

            _mockContext.Setup(c => c.Orders).Returns(mockSet.Object);

            var result = await _controller.GetOrder(1) as OkObjectResult;

            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            var order = result.Value as Commande;
            Assert.Equal(1, order.Id);
        }

        [Fact]
        public async Task GetOrder_OrderDoesNotExist_ReturnsNotFound()
        {
            var mockSet = new Mock<DbSet<Commande>>();
            _mockContext.Setup(c => c.Orders).Returns(mockSet.Object);

            var result = await _controller.GetOrder(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task PostOrder_ValidOrder_ReturnsCreatedResponse()
        {
            var newOrder = new Commande { Id = 2, CustomerName = "Daniel DUPONT", OrderDate = new DateTime(13893202), TotalAmount = 50, ClientID = 32, ProduitIDs = new List<int>() { 1, 2, 8 } };

            var mockSet = new Mock<DbSet<Commande>>();
            _mockContext.Setup(c => c.Orders).Returns(mockSet.Object);

            var result = await _controller.PostOrder(newOrder) as CreatedAtActionResult;

            Assert.NotNull(result);
            Assert.Equal(201, result.StatusCode);
            Assert.Equal(nameof(_controller.GetOrder), result.ActionName);
        }

        [Fact]
        public async Task PostOrder_InvalidOrder_ReturnsBadRequest()
        {
            _controller.ModelState.AddModelError("Total", "Required");

            var newOrder = new Commande { Id = 2, CustomerName = "Jean CHRISTIAN", OrderDate = new DateTime(3293202), TotalAmount = 50, ClientID = 32, ProduitIDs = new List<int>() { 8 } };

            var result = await _controller.PostOrder(newOrder);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task DeleteOrder_OrderExists_ReturnsOk()
        {
            var mockOrder = new Commande { Id = 2, CustomerName = "Jean CHRISTIAN", OrderDate = new DateTime(3293202), TotalAmount = 50, ClientID = 32, ProduitIDs = new List<int>() { 8 } };

            var mockSet = new Mock<DbSet<Commande>>();
            mockSet.Setup(m => m.FindAsync(1)).ReturnsAsync(mockOrder);

            _mockContext.Setup(c => c.Orders).Returns(mockSet.Object);

            var result = await _controller.DeleteOrder(1);

            Assert.NotNull(result);
            var okResult = result as OkObjectResult;
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task DeleteOrder_OrderDoesNotExist_ReturnsNotFound()
        {
            var mockSet = new Mock<DbSet<Commande>>();
            _mockContext.Setup(c => c.Orders).Returns(mockSet.Object);

            var result = await _controller.DeleteOrder(99);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
