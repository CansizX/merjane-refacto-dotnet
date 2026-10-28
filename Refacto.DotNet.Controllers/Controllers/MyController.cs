using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services.Products;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IEnumerable<IProductOrderHandler> _productHandlers;
        private readonly AppDbContext _ctx;

        public OrdersController(IEnumerable<IProductOrderHandler> productHandlers, AppDbContext ctx)
        {
            _productHandlers = productHandlers;
            _ctx = ctx;
        }

        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(200)]
        public ActionResult<ProcessOrderResponse> ProcessOrder(long orderId)
        {
            Order? order = _ctx.Orders
                .Include(o => o.Items)
                .SingleOrDefault(o => o.Id == orderId);

            foreach (Product product in order!.Items!)
            {
                IProductOrderHandler? handler = _productHandlers.FirstOrDefault(h => h.ProductType == product.Type);
                handler?.Handle(product);
            }

            _ = _ctx.SaveChanges();

            return new ProcessOrderResponse(order.Id);
        }
    }
}
