using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Domain.Contracts;

public record ProductDto(int Id, string Sku, IReadOnlyList<string> Tags);

public class ProductContractTests
{
    [Fact]
    public void Contract_reads_sku_cleanly()
    {
        // Arrange
        var product = new ProductDto(1, "SKU-100", ["hardware", "tools"]);

        // Act
        var sku = product.Sku;

        // Assert
        sku.Should().Be("SKU-100", "product sku matches initialized value");
    }
}
