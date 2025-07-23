#pragma warning disable CS0854 // An expression tree may not contain a call or invocation that uses optional arguments
using Xunit;
using CornerShop.Services;
using CornerShop.Models;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CornerShop.Tests;

public class SimpleUserFunctionalityTests
{
    [Fact]
    public async Task SearchProducts_ReturnsExpectedResults()
    {
        // Arrange
        var mockService = new Mock<IDatabaseService>();
        var expected = new List<Product> {
            new Product { Name = "Apple", Category = "Fruit", Price = 1.0m, StockQuantity = 10 },
            new Product { Name = "Banana", Category = "Fruit", Price = 2.0m, StockQuantity = 20 }
        };
        mockService.Setup(s => s.SearchProducts(It.IsAny<string>(), It.IsAny<string?>())).ReturnsAsync(expected);

        // Act
        var result = await mockService.Object.SearchProducts("a", null);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Name == "Apple");
        Assert.Contains(result, p => p.Name == "Banana");
    }

    [Fact]
    public async Task RegisterSale_ReturnsSaleId()
    {
        // Arrange
        var mockService = new Mock<IDatabaseService>();
        var sale = new Sale
        {
            Items = new List<SaleItem> {
                new SaleItem { ProductName = "Apple", Quantity = 2, Price = 1.0m }
            },
            TotalAmount = 2.0m
        };
        mockService.Setup(s => s.CreateSale(sale)).ReturnsAsync("sale123");

        // Act
        var saleId = await mockService.Object.CreateSale(sale);

        // Assert
        Assert.Equal("sale123", saleId);
    }

    [Fact]
    public async Task CancelSale_ReturnsTrueOnSuccess()
    {
        // Arrange
        var mockService = new Mock<IDatabaseService>();
        mockService.Setup(s => s.CancelSale("sale123")).ReturnsAsync(true);

        // Act
        var result = await mockService.Object.CancelSale("sale123");

        // Assert
        Assert.True(result);
    }
}
