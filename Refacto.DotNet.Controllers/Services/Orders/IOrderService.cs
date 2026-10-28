namespace Refacto.DotNet.Controllers.Services.Orders
{
    public interface IOrderService
    {
        /// <summary>
        /// Applies the business rules of each ordered product and persists the result.
        /// </summary>
        /// <returns><c>false</c> when the order does not exist.</returns>
        bool ProcessOrder(long orderId);
    }
}
