using Moq;
using Refacto.Dotnet.Controllers.Tests.Fakes;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;
using Refacto.DotNet.Controllers.Services.Products;

namespace Refacto.Dotnet.Controllers.Tests.Services.Products
{
    public class SeasonalProductHandlerTests
    {
        private static readonly DateTime Now = new(2026, 6, 15, 10, 0, 0);

        private readonly Mock<INotificationService> _notificationService = new();
        private readonly SeasonalProductHandler _handler;

        public SeasonalProductHandlerTests()
        {
            _handler = new SeasonalProductHandler(_notificationService.Object, new FixedDateTimeProvider(Now));
        }

        [Fact]
        public void Handle_WhenInSeasonAndInStock_ShouldDecrementStock()
        {
            Product product = CreateProduct(available: 30, leadTime: 15, seasonStart: Now.AddDays(-10), seasonEnd: Now.AddDays(30));

            _handler.Handle(product);

            Assert.Equal(29, product.Available);
            _notificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenOutOfStockAndRestockedBeforeSeasonEnd_ShouldNotifyDelay()
        {
            Product product = CreateProduct(available: 0, leadTime: 5, seasonStart: Now.AddDays(-10), seasonEnd: Now.AddDays(30));

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notificationService.Verify(n => n.SendDelayNotification(5, "Watermelon"), Times.Once());
            _notificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenOutOfStockAndRestockedAfterSeasonEnd_ShouldNotifyOutOfStock()
        {
            Product product = CreateProduct(available: 0, leadTime: 10, seasonStart: Now.AddDays(-10), seasonEnd: Now.AddDays(3));

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notificationService.Verify(n => n.SendOutOfStockNotification("Watermelon"), Times.Once());
            _notificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenSeasonIsOver_ShouldNotifyOutOfStockAndMakeProductUnavailable()
        {
            Product product = CreateProduct(available: 10, leadTime: 5, seasonStart: Now.AddDays(-60), seasonEnd: Now.AddDays(-1));

            _handler.Handle(product);

            Assert.Equal(0, product.Available);
            _notificationService.Verify(n => n.SendOutOfStockNotification("Watermelon"), Times.Once());
            _notificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenSeasonHasNotStarted_ShouldNotifyOutOfStockAndKeepStock()
        {
            Product product = CreateProduct(available: 10, leadTime: 5, seasonStart: Now.AddDays(30), seasonEnd: Now.AddDays(90));

            _handler.Handle(product);

            Assert.Equal(10, product.Available);
            _notificationService.Verify(n => n.SendOutOfStockNotification("Watermelon"), Times.Once());
            _notificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public void Handle_WhenSeasonStartsToday_ShouldNotSellYetAndNotifyDelay()
        {
            // Existing behaviour: the season bounds are exclusive, so the first day is not sellable.
            Product product = CreateProduct(available: 10, leadTime: 5, seasonStart: Now.Date, seasonEnd: Now.AddDays(30));

            _handler.Handle(product);

            Assert.Equal(10, product.Available);
            _notificationService.Verify(n => n.SendDelayNotification(5, "Watermelon"), Times.Once());
            _notificationService.VerifyNoOtherCalls();
        }

        private static Product CreateProduct(int available, int leadTime, DateTime seasonStart, DateTime seasonEnd)
        {
            return new Product
            {
                Type = ProductType.Seasonal,
                Name = "Watermelon",
                Available = available,
                LeadTime = leadTime,
                SeasonStartDate = seasonStart,
                SeasonEndDate = seasonEnd
            };
        }
    }
}
