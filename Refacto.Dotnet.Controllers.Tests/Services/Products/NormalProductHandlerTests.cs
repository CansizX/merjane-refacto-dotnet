using Moq;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Products;

namespace Refacto.Dotnet.Controllers.Tests.Services.Products
{
    public class NormalProductHandlerTests
    {
        private readonly Mock<INotificationService> _notificationService = new();
        private readonly NormalProductHandler _handler;

        public NormalProductHandlerTests()
        {
            _handler = new NormalProductHandler(_notificationService.Object);
        }

        [Fact]
        public void Handle_WhenInStock_ShouldDecrementStock()
        {
            Product product = new() { Type = ProductType.Normal, Name = "USB Cable", Available = 30, LeadTime = 15 };

            _handler.Handle(product);

            Assert.Equal(29, product.Available);
            _notificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenOutOfStock_ShouldNotifyDelay()
        {
            Product product = new() { Type = ProductType.Normal, Name = "RJ45 Cable", Available = 0, LeadTime = 15 };

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            Assert.Equal(15, product.LeadTime);
            _notificationService.Verify(n => n.SendDelayNotification(15, "RJ45 Cable"), Times.Once());
            _notificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenOutOfStockWithoutLeadTime_ShouldDoNothing()
        {
            Product product = new() { Type = ProductType.Normal, Name = "Ethernet Switch", Available = 0, LeadTime = 0 };

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notificationService.VerifyNoOtherCalls();
        }
    }
}
