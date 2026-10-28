using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services.Products;

namespace Refacto.DotNet.Controllers.Services.Orders
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IReadOnlyDictionary<string, IProductOrderHandler> _handlersByProductType;

        public OrderService(IOrderRepository orderRepository, IEnumerable<IProductOrderHandler> productHandlers)
        {
            _orderRepository = orderRepository;
            _handlersByProductType = productHandlers.ToDictionary(handler => handler.ProductType);
        }

        public bool ProcessOrder(long orderId)
        {
            Order? order = _orderRepository.FindByIdWithItems(orderId);
            if (order is null)
            {
                return false;
            }

            foreach (Product product in order.Items ?? Enumerable.Empty<Product>())
            {
                // Products of an unknown type are left untouched, as before.
                if (product.Type is not null && _handlersByProductType.TryGetValue(product.Type, out IProductOrderHandler? handler))
                {
                    handler.Handle(product);
                }
            }

            _orderRepository.SaveChanges();
            return true;
        }
    }
}
