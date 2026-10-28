using Microsoft.EntityFrameworkCore;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services.Orders;

namespace Refacto.DotNet.Controllers.Database.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly AppDbContext _context;

        public OrderRepository(AppDbContext context)
        {
            _context = context;
        }

        public Order? FindByIdWithItems(long orderId)
        {
            return _context.Orders
                .Include(order => order.Items)
                .SingleOrDefault(order => order.Id == orderId);
        }

        public void SaveChanges()
        {
            _ = _context.SaveChanges();
        }
    }
}
