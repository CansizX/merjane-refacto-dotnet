using Moq;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services.Orders;
using Refacto.DotNet.Controllers.Services.Products;

namespace Refacto.Dotnet.Controllers.Tests.Services.Orders
{
    public class OrderServiceTests
    {
        private readonly Mock<IOrderRepository> _orderRepository = new();
        private readonly Mock<IProductOrderHandler> _normalHandler = CreateHandler(ProductType.Normal);
        private readonly Mock<IProductOrderHandler> _seasonalHandler = CreateHandler(ProductType.Seasonal);
        private readonly OrderService _orderService;

        public OrderServiceTests()
        {
            _orderService = new OrderService(_orderRepository.Object, new[] { _normalHandler.Object, _seasonalHandler.Object });
        }

        [Fact]
        public void ProcessOrder_ShouldDispatchEachProductToTheHandlerOfItsType_AndSaveOnce()
        {
            Product normal = new() { Type = ProductType.Normal, Name = "USB Cable" };
            Product seasonal = new() { Type = ProductType.Seasonal, Name = "Watermelon" };
            _ = _orderRepository.Setup(r => r.FindByIdWithItems(1)).Returns(new Order { Id = 1, Items = new List<Product> { normal, seasonal } });

            bool processed = _orderService.ProcessOrder(1);

            Assert.True(processed);
            _normalHandler.Verify(h => h.Handle(normal), Times.Once());
            _normalHandler.Verify(h => h.Handle(seasonal), Times.Never());
            _seasonalHandler.Verify(h => h.Handle(seasonal), Times.Once());
            _orderRepository.Verify(r => r.SaveChanges(), Times.Once());
        }

        [Fact]
        public void ProcessOrder_ShouldIgnoreProductsOfUnknownType()
        {
            Product unknown = new() { Type = "UNKNOWN", Name = "Mystery Box", Available = 5 };
            Product withoutType = new() { Type = null, Name = "No Type", Available = 5 };
            _ = _orderRepository.Setup(r => r.FindByIdWithItems(1)).Returns(new Order { Id = 1, Items = new List<Product> { unknown, withoutType } });

            bool processed = _orderService.ProcessOrder(1);

            Assert.True(processed);
            _normalHandler.Verify(h => h.Handle(It.IsAny<Product>()), Times.Never());
            _seasonalHandler.Verify(h => h.Handle(It.IsAny<Product>()), Times.Never());
            Assert.Equal(5, unknown.Available);
        }

        [Fact]
        public void ProcessOrder_WhenOrderDoesNotExist_ShouldReturnFalseAndSaveNothing()
        {
            _ = _orderRepository.Setup(r => r.FindByIdWithItems(42)).Returns((Order?)null);

            bool processed = _orderService.ProcessOrder(42);

            Assert.False(processed);
            _orderRepository.Verify(r => r.SaveChanges(), Times.Never());
        }

        private static Mock<IProductOrderHandler> CreateHandler(string productType)
        {
            Mock<IProductOrderHandler> handler = new();
            _ = handler.Setup(h => h.ProductType).Returns(productType);
            return handler;
        }
    }
}
