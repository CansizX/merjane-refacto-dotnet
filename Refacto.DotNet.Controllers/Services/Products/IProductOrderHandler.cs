using Refacto.DotNet.Controllers.Entities;

namespace Refacto.DotNet.Controllers.Services.Products
{
    /// <summary>
    /// Applies the business rules of one product type when that product is ordered:
    /// update the stock and notify customers when the product cannot be delivered right away.
    /// </summary>
    public interface IProductOrderHandler
    {
        /// <summary>The product type (see <see cref="Entities.ProductType"/>) handled by this implementation.</summary>
        string ProductType { get; }

        void Handle(Product product);
    }
}
