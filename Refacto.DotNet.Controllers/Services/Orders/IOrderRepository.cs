using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Orders
{
    /// <summary>
    /// Persistence port used by the application layer; implemented in the Database layer.
    /// </summary>
    public interface IOrderRepository
    {
        Order? FindByIdWithItems(long orderId);

        void SaveChanges();
    }
}
