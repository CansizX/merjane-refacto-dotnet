using Microsoft.AspNetCore.Mvc;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Services.Orders;

namespace Refacto.DotNet.Controllers.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [HttpPost("{orderId}/processOrder")]
        [ProducesResponseType(typeof(ProcessOrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<ProcessOrderResponse> ProcessOrder(long orderId)
        {
            bool orderProcessed = _orderService.ProcessOrder(orderId);
            if (!orderProcessed)
            {
                return NotFound();
            }

            return new ProcessOrderResponse(orderId);
        }
    }
}
