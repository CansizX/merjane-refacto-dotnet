using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services.Time;

namespace Refacto.DotNet.Controllers.Services.Products
{
    /// <summary>
    /// EXPIRABLE products: sold normally until their expiry date; otherwise customers are notified
    /// and the product is no longer available.
    /// </summary>
    public class ExpirableProductHandler : IProductOrderHandler
    {
        private readonly INotificationService _notificationService;
        private readonly IDateTimeProvider _dateTimeProvider;

        public ExpirableProductHandler(INotificationService notificationService, IDateTimeProvider dateTimeProvider)
        {
            _notificationService = notificationService;
            _dateTimeProvider = dateTimeProvider;
        }

        public string ProductType => Entities.ProductType.Expirable;

        public void Handle(Product product)
        {
            DateTime today = _dateTimeProvider.Now.Date;

            if (product.IsInStock && product.ExpiryDate > today)
            {
                product.DecrementStock();
                return;
            }

            _notificationService.SendExpirationNotification(product.Name!, product.ExpiryDate!.Value);
            product.MarkAsUnavailable();
        }
    }
}
