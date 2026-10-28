using Moq;
using Refacto.Dotnet.Controllers.Tests.Fakes;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Products;

namespace Refacto.Dotnet.Controllers.Tests.Services.Products
{
    public class ExpirableProductHandlerTests
    {
        private static readonly DateTime Now = new(2026, 6, 15, 10, 0, 0);

        private readonly Mock<INotificationService> _notificationService = new();
        private readonly ExpirableProductHandler _handler;

        public ExpirableProductHandlerTests()
        {
            _handler = new ExpirableProductHandler(_notificationService.Object, new FixedDateTimeProvider(Now));
        }

        [Fact]
        public void Handle_WhenNotExpiredAndInStock_ShouldDecrementStock()
        {
            Product product = CreateProduct(available: 30, expiryDate: Now.AddDays(26));

            _handler.Handle(product);

            Assert.Equal(29, product.Available);
            _notificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenExpired_ShouldNotifyExpirationAndMakeProductUnavailable()
        {
            DateTime expiryDate = Now.AddDays(-2);
            Product product = CreateProduct(available: 6, expiryDate: expiryDate);

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notificationService.Verify(n => n.SendExpirationNotification("Milk", expiryDate), Times.Once());
            _notificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenExpiringToday_ShouldBeConsideredExpired()
        {
            Product product = CreateProduct(available: 6, expiryDate: Now.Date);

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notificationService.Verify(n => n.SendExpirationNotification("Milk", Now.Date), Times.Once());
            _notificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenOutOfStockButNotExpired_ShouldNotifyExpiration()
        {
            // Existing behaviour kept as is: an out-of-stock expirable product triggers an expiration notification.
            DateTime expiryDate = Now.AddDays(10);
            Product product = CreateProduct(available: 0, expiryDate: expiryDate);

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notificationService.Verify(n => n.SendExpirationNotification("Milk", expiryDate), Times.Once());
            _notificationService.VerifyNoOtherCalls();
        }

        private static Product CreateProduct(int available, DateTime expiryDate)
        {
            return new Product
            {
                Type = ProductType.Expirable,
                Name = "Milk",
                Available = available,
                LeadTime = 15,
                ExpiryDate = expiryDate
            };
        }
    }
}
