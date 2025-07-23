using Xunit;
using Moq;
using CornerShop.Services;
using CornerShop.Models;
using System;
using System.Threading.Tasks;

namespace CornerShop.Tests
{
    public class CashRegisterServiceTests
    {
        private readonly Mock<IDatabaseService> _mockDb;
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<ISaleService> _mockSaleService;
        private readonly ICashRegisterService _cashRegisterService;

        public CashRegisterServiceTests()
        {
            _mockDb = new Mock<IDatabaseService>();
            _mockProductService = new Mock<IProductService>();
            _mockSaleService = new Mock<ISaleService>();

            _cashRegisterService = new CashRegisterService(
                _mockDb.Object,
                _mockProductService.Object,
                _mockSaleService.Object
            );
        }

        [Fact]
        public async Task CreateSaleOnRegister_ValidSale_CreatesSale()
        {
            // Arrange
            var registerId = 0;
            var sale = new Sale
            {
                Id = "test-sale",
                Date = DateTime.UtcNow,
                Items = new List<SaleItem>
                {
                    new SaleItem { ProductName = "Test Product", Quantity = 1 }
                }
            };

            _mockProductService
                .Setup(x => x.ValidateStockAvailability(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(true);

            _mockSaleService
                .Setup(x => x.CreateSale(It.IsAny<Sale>()))
                .ReturnsAsync("test-sale");

            // Act
            var result = await _cashRegisterService.CreateSaleOnRegister(registerId, sale);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(sale.Id, result.Id);
            _mockSaleService.Verify(x => x.CreateSale(It.IsAny<Sale>()), Times.Once);
        }

        [Fact]
        public async Task CreateSaleOnRegister_InsufficientStock_ThrowsException()
        {
            // Arrange
            var registerId = 0;
            var sale = new Sale
            {
                Id = "test-sale",
                Date = DateTime.UtcNow,
                Items = new List<SaleItem>
                {
                    new SaleItem { ProductName = "Test Product", Quantity = 1 }
                }
            };

            _mockProductService
                .Setup(x => x.ValidateStockAvailability(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _cashRegisterService.CreateSaleOnRegister(registerId, sale)
            );
        }

        // [Fact]
        // public async Task CancelSaleOnRegister_ValidSale_CancelsSale()
        // {
        //     // Arrange
        //     var registerId = 0;
        //     var saleId = "test-sale";

        //     // Set up the active sale for the register
        //     var sale = new Sale
        //     {
        //         Id = saleId,
        //         Date = DateTime.UtcNow,
        //         Items = new List<SaleItem>
        //         {
        //             new SaleItem { ProductName = "Test Product", Quantity = 1 }
        //         }
        //     };

        //     // Create a sale first to set up the active sale
        //     _mockProductService
        //         .Setup(x => x.ValidateStockAvailability(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
        //         .ReturnsAsync(true);

        //     _mockSaleService
        //         .Setup(x => x.CreateSale(It.IsAny<Sale>()))
        //         .ReturnsAsync(saleId);

        //     await _cashRegisterService.CreateSaleOnRegister(registerId, sale);
        //     await _cashRegisterService.UnlockRegister(registerId); // Unlock the register after creating the sale

        //     // Now set up the cancel sale mock
        //     _mockSaleService
        //         .Setup(x => x.CancelSale(saleId, It.IsAny<string>()))
        //         .ReturnsAsync(true);

        //     // Act
        //     var result = await _cashRegisterService.CancelSaleOnRegister(registerId, saleId);

        //     // Assert
        //     Assert.True(result);
        //     _mockSaleService.Verify(x => x.CancelSale(saleId, It.IsAny<string>()), Times.Once);
        // }

        [Fact]
        public async Task CancelSaleOnRegister_InvalidSaleId_ReturnsFalse()
        {
            // Arrange
            var registerId = 0;
            var saleId = "invalid-sale";

            _mockSaleService
                .Setup(x => x.CancelSale(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _cashRegisterService.CancelSaleOnRegister(registerId, saleId);

            // Assert
            Assert.False(result);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        public async Task CreateSaleOnRegister_InvalidRegisterId_ThrowsException(int registerId)
        {
            // Arrange
            var sale = new Sale
            {
                Id = "test-sale",
                Date = DateTime.UtcNow,
                Items = new List<SaleItem>()
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => _cashRegisterService.CreateSaleOnRegister(registerId, sale)
            );
        }
    }
}
