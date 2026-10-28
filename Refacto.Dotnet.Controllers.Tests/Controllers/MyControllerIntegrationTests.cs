using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Refacto.DotNet.Controllers.Database.Context;
using Refacto.DotNet.Controllers.Dtos.Product;
using Refacto.DotNet.Controllers.Entities;
using Refacto.DotNet.Controllers.Services;

namespace Refacto.Dotnet.Controllers.Tests.Controllers
{
    /// <summary>
    /// End-to-end tests of POST /orders/{orderId}/processOrder.
    /// They were written against the original implementation (characterization tests)
    /// to lock the existing behaviour before any refactoring.
    /// </summary>
    [Collection("Sequential")]
    public class MyControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly AppDbContext _context;
        private readonly Mock<INotificationService> _mockNotificationService;

        public MyControllerIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _mockNotificationService = new Mock<INotificationService>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                _ = builder.ConfigureServices(services =>
                {
                    _ = services.AddSingleton(_mockNotificationService.Object);

                    ServiceDescriptor? descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        _ = services.Remove(descriptor);
                    }

                    // Add ApplicationDbContext using an in-memory database for testing
                    _ = services.AddDbContext<AppDbContext>(options =>
                    {
                        _ = options.UseInMemoryDatabase($"InMemoryDbForTesting-{GetType()}");
                    });
                    _ = services.AddScoped((_sp) => _mockNotificationService.Object);
                });
            });

            IServiceScope scope = _factory.Services.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task ProcessOrder_ShouldUpdateStockAndNotify_ForEachProductType()
        {
            // GIVEN
            HttpClient client = _factory.CreateClient();
            List<Product> products = new()
            {
                new Product { LeadTime = 15, Available = 30, Type = "NORMAL", Name = "USB Cable" },
                new Product { LeadTime = 10, Available = 0, Type = "NORMAL", Name = "USB Dongle" },
                new Product { LeadTime = 15, Available = 30, Type = "EXPIRABLE", Name = "Butter", ExpiryDate = DateTime.Now.AddDays(26) },
                new Product { LeadTime = 90, Available = 6, Type = "EXPIRABLE", Name = "Milk", ExpiryDate = DateTime.Now.AddDays(-2) },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Watermelon", SeasonStartDate = DateTime.Now.AddDays(-2), SeasonEndDate = DateTime.Now.AddDays(58) },
                new Product { LeadTime = 15, Available = 30, Type = "SEASONAL", Name = "Grapes", SeasonStartDate = DateTime.Now.AddDays(180), SeasonEndDate = DateTime.Now.AddDays(240) }
            };
            Order order = await SaveOrderAsync(products);

            // WHEN
            HttpResponseMessage response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);

            // THEN
            _ = response.EnsureSuccessStatusCode();
            ProcessOrderResponse? body = await response.Content.ReadFromJsonAsync<ProcessOrderResponse>();
            Assert.Equal(order.Id, body!.id);

            Dictionary<string, int> stock = await GetStockByProductNameAsync();
            Assert.Equal(29, stock["USB Cable"]);   // NORMAL in stock: decremented
            Assert.Equal(0, stock["USB Dongle"]);   // NORMAL out of stock: delay notified
            Assert.Equal(29, stock["Butter"]);      // EXPIRABLE not expired: decremented
            Assert.Equal(0, stock["Milk"]);         // EXPIRABLE expired: no longer available
            Assert.Equal(29, stock["Watermelon"]);  // SEASONAL in season: decremented
            Assert.Equal(30, stock["Grapes"]);      // SEASONAL before season: untouched

            _mockNotificationService.Verify(n => n.SendDelayNotification(10, "USB Dongle"), Times.Once());
            _mockNotificationService.Verify(n => n.SendExpirationNotification("Milk", products[3].ExpiryDate!.Value), Times.Once());
            _mockNotificationService.Verify(n => n.SendOutOfStockNotification("Grapes"), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task ProcessOrder_ShouldHandleEdgeCases_ForEachProductType()
        {
            // GIVEN
            HttpClient client = _factory.CreateClient();
            List<Product> products = new()
            {
                new Product { LeadTime = 0, Available = 0, Type = "NORMAL", Name = "Ethernet Switch" },
                new Product { LeadTime = 5, Available = 0, Type = "SEASONAL", Name = "Pumpkin", SeasonStartDate = DateTime.Now.AddDays(-10), SeasonEndDate = DateTime.Now.AddDays(30) },
                new Product { LeadTime = 10, Available = 0, Type = "SEASONAL", Name = "Strawberry", SeasonStartDate = DateTime.Now.AddDays(-10), SeasonEndDate = DateTime.Now.AddDays(3) },
                new Product { LeadTime = 5, Available = 10, Type = "SEASONAL", Name = "Chestnut", SeasonStartDate = DateTime.Now.AddDays(-60), SeasonEndDate = DateTime.Now.AddDays(-1) },
                new Product { LeadTime = 5, Available = 0, Type = "EXPIRABLE", Name = "Yogurt", ExpiryDate = DateTime.Now.AddDays(10) },
                new Product { LeadTime = 5, Available = 5, Type = "UNKNOWN", Name = "Mystery Box" }
            };
            Order order = await SaveOrderAsync(products);

            // WHEN
            HttpResponseMessage response = await client.PostAsync($"/orders/{order.Id}/processOrder", null);

            // THEN
            _ = response.EnsureSuccessStatusCode();

            Dictionary<string, int> stock = await GetStockByProductNameAsync();
            Assert.Equal(0, stock["Ethernet Switch"]); // NORMAL out of stock without lead time: nothing happens
            Assert.Equal(0, stock["Pumpkin"]);         // SEASONAL restock within season: delay notified
            Assert.Equal(0, stock["Strawberry"]);      // SEASONAL restock after season end: out of stock
            Assert.Equal(0, stock["Chestnut"]);        // SEASONAL season over: out of stock, stock reset
            Assert.Equal(0, stock["Yogurt"]);          // EXPIRABLE out of stock: expiration notified (existing behaviour)
            Assert.Equal(5, stock["Mystery Box"]);     // unknown type: ignored

            _mockNotificationService.Verify(n => n.SendDelayNotification(5, "Pumpkin"), Times.Once());
            _mockNotificationService.Verify(n => n.SendOutOfStockNotification("Strawberry"), Times.Once());
            _mockNotificationService.Verify(n => n.SendOutOfStockNotification("Chestnut"), Times.Once());
            _mockNotificationService.Verify(n => n.SendExpirationNotification("Yogurt", products[4].ExpiryDate!.Value), Times.Once());
            _mockNotificationService.VerifyNoOtherCalls();
        }

        private async Task<Order> SaveOrderAsync(List<Product> products)
        {
            await _context.Products.AddRangeAsync(products);
            Order order = new() { Items = new HashSet<Product>(products) };
            _ = await _context.Orders.AddAsync(order);
            _ = await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
            return order;
        }

        private Task<Dictionary<string, int>> GetStockByProductNameAsync()
        {
            return _context.Products
                .AsNoTracking()
                .ToDictionaryAsync(p => p.Name!, p => p.Available);
        }
    }
}
