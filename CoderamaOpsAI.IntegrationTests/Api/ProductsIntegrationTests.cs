using System.Net;
using System.Net.Http.Json;
using CoderamaOpsAI.Api.Models;
using CoderamaOpsAI.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CoderamaOpsAI.IntegrationTests.Api;

public class ProductsIntegrationTests : IntegrationTestBase
{
    [Fact]
    public async Task Given_AuthenticatedUser_When_PerformProductsCRUD_Then_AllOperationsSucceed()
    {
        // Arrange - Authenticate
        var token = await GetAuthTokenAsync("test@example.com", "Test123!");
        SetAuthorizationHeader(token);

        // 1. List Products (Initial)
        var listResponse1 = await ApiClient.GetAsync("/api/products");
        listResponse1.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialProducts = await listResponse1.Content.ReadFromJsonAsync<List<ProductResponse>>();
        initialProducts.Should().NotBeNull();
        var initialCount = initialProducts!.Count;

        // 2. Create Product
        var createRequest = new CreateProductRequest
        {
            Name = "Test Product Integration",
            Description = "Created during integration test",
            Price = 99.99m,
            Stock = 100
        };

        var createResponse = await ApiClient.PostAsJsonAsync("/api/products", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse.Headers.Location.Should().NotBeNull();

        var createdProduct = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        createdProduct.Should().NotBeNull();
        createdProduct!.Id.Should().BeGreaterThan(0);
        createdProduct.Name.Should().Be(createRequest.Name);
        createdProduct.Price.Should().Be(createRequest.Price);
        createdProduct.Stock.Should().Be(createRequest.Stock);

        var productId = createdProduct.Id;

        // 3. Get Product by ID
        var getResponse = await ApiClient.GetAsync($"/api/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var retrievedProduct = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        retrievedProduct.Should().NotBeNull();
        retrievedProduct!.Id.Should().Be(productId);
        retrievedProduct.Name.Should().Be(createRequest.Name);

        // 4. Update Product
        var updateRequest = new UpdateProductRequest
        {
            Name = "Test Product Updated",
            Price = 149.99m
        };

        var updateResponse = await ApiClient.PutAsJsonAsync($"/api/products/{productId}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedProduct = await updateResponse.Content.ReadFromJsonAsync<ProductResponse>();
        updatedProduct.Should().NotBeNull();
        updatedProduct!.Name.Should().Be(updateRequest.Name);
        updatedProduct.Price.Should().Be(updateRequest.Price);
        updatedProduct.Stock.Should().Be(100); // Should remain unchanged

        // 5. Verify in Database
        using var dbContext = GetDbContext();
        var dbProduct = await dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId);

        dbProduct.Should().NotBeNull();
        dbProduct!.Name.Should().Be("Test Product Updated");
        dbProduct.Price.Should().Be(149.99m);

        // 6. List Products (After Creation)
        var listResponse2 = await ApiClient.GetAsync("/api/products");
        var productsAfterCreate = await listResponse2.Content.ReadFromJsonAsync<List<ProductResponse>>();
        productsAfterCreate.Should().NotBeNull();
        productsAfterCreate!.Count.Should().Be(initialCount + 1);
        productsAfterCreate.Should().Contain(p => p.Id == productId);

        // 7. Delete Product
        var deleteResponse = await ApiClient.DeleteAsync($"/api/products/{productId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 8. Verify Deletion
        var getDeletedResponse = await ApiClient.GetAsync($"/api/products/{productId}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // 9. List Products (After Deletion)
        var listResponse3 = await ApiClient.GetAsync("/api/products");
        var productsAfterDelete = await listResponse3.Content.ReadFromJsonAsync<List<ProductResponse>>();
        productsAfterDelete.Should().NotBeNull();
        productsAfterDelete!.Count.Should().Be(initialCount);
        productsAfterDelete.Should().NotContain(p => p.Id == productId);
    }

    [Fact]
    public async Task Given_InvalidProductData_When_Create_Then_ReturnsValidationError()
    {
        // Arrange - Authenticate
        var token = await GetAuthTokenAsync("test@example.com", "Test123!");
        SetAuthorizationHeader(token);

        // Test Case 1: Empty Name
        var emptyNameRequest = new CreateProductRequest
        {
            Name = "",
            Price = 99.99m,
            Stock = 100
        };

        var emptyNameResponse = await ApiClient.PostAsJsonAsync("/api/products", emptyNameRequest);
        emptyNameResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var emptyNameError = await emptyNameResponse.Content.ReadAsStringAsync();
        emptyNameError.Should().Contain("Name");

        // Test Case 2: Negative Price
        var negativePriceRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Price = -10m,
            Stock = 100
        };

        var negativePriceResponse = await ApiClient.PostAsJsonAsync("/api/products", negativePriceRequest);
        negativePriceResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var negativePriceError = await negativePriceResponse.Content.ReadAsStringAsync();
        negativePriceError.Should().Contain("Price");

        // Test Case 3: Negative Stock
        var negativeStockRequest = new CreateProductRequest
        {
            Name = "Test Product",
            Price = 99.99m,
            Stock = -5
        };

        var negativeStockResponse = await ApiClient.PostAsJsonAsync("/api/products", negativeStockRequest);
        negativeStockResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var negativeStockError = await negativeStockResponse.Content.ReadAsStringAsync();
        negativeStockError.Should().Contain("Stock");

        // Test Case 4: Name Exceeds Max Length (max is 100)
        var longNameRequest = new CreateProductRequest
        {
            Name = new string('A', 200), // 200 characters
            Price = 99.99m,
            Stock = 100
        };

        var longNameResponse = await ApiClient.PostAsJsonAsync("/api/products", longNameRequest);
        longNameResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var longNameError = await longNameResponse.Content.ReadAsStringAsync();
        longNameError.Should().Contain("Name");
    }
}
