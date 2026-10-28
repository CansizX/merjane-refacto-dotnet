using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services.Time;

namespace Refacto.DotNet.Controllers.Services.Products
{
    /// <summary>
    /// SEASONAL products: sold only during their season. When they cannot be sold, customers are told
    /// the restocking delay, unless the product cannot be restocked before the end of the season
    /// or its season has not started yet, in which case it is considered unavailable.
    /// </summary>
    public class SeasonalProductHandler : IProductOrderHandler
    {
        private readonly INotificationService _notificationService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public SeasonalProductHandler(INotificationService notificationService, IDateTimeProvider dateTimeProvider)
        {
            _notificationService = notificationService;
            _dateTimeProvider = dateTimeProvider;
        }

        public string ProductType => Entities.ProductType.Seasonal;

        public void Handle(Product product)
        {
            DateTime now = _dateTimeProvider.Now;

            if (product.IsInStock && IsInSeason(product, now.Date))
            {
                product.DecrementStock();
                return;
            }

            if (now.AddDays(product.LeadTime) > product.SeasonEndDate)
            {
                // Restocking would only arrive after the end of the season.
                _notificationService.SendOutOfStockNotification(product.Name!);
                product.MarkAsUnavailable();
                return;
            }

            if (product.SeasonStartDate > now)
            {
                _notificationService.SendOutOfStockNotification(product.Name!);
                return;
            }

            _notificationService.SendDelayNotification(product.LeadTime, product.Name!);
        }

        private static bool IsInSeason(Product product, DateTime today)
        {
            return today > product.SeasonStartDate && today < product.SeasonEndDate;
        }
    }
}
