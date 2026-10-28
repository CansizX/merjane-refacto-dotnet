using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Products
{
    /// <summary>
    /// NORMAL products: sold while in stock; otherwise customers are told the restocking delay.
    /// </summary>
    public class NormalProductHandler : IProductOrderHandler
    {
        private readonly INotificationService _notificationService;

        public NormalProductHandler(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public string ProductType => Entities.ProductType.Normal;

        public void Handle(Product product)
        {
            if (product.IsInStock)
            {
                product.DecrementStock();
                return;
            }

            if (product.LeadTime > 0)
            {
                _notificationService.SendDelayNotification(product.LeadTime, product.Name!);
            }
        }
    }
}
