using FluentAssertions;
using ProductService.Domain.Entities;
using Xunit;

namespace ProductService.Tests.UnitTests.Domain;

public class CategoryEntityTests
{
    [Fact]
    public void Category_ShouldBeCreated_WithValidData()
    {
        // Arrange
        var name = "Electronics";
        var description = "Electronic products";

        // Act
        var category = new Category(name, description);

        // Assert
        category.Should().NotBeNull();
        category.Id.Should().NotBeEmpty();
        category.Name.Should().Be(name);
        category.Description.Should().Be(description);
        category.Slug.Should().Be("electronics");
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Category_ShouldGenerateSlug_Correctly()
    {
        // Arrange
        var name = "Home & Garden";

        // Act
        var category = new Category(name, "Test");

        // Assert
        category.Slug.Should().Be("home-and-garden");
    }

    [Fact]
    public void Category_ShouldThrowException_WithEmptyName()
    {
        // Act
        Action act = () => new Category("", "Description");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*name*");
    }

    [Fact]
    public void Update_ShouldUpdateCategoryDetails()
    {
        // Arrange
        var category = new Category("Original", "Original Description");
        var newName = "Updated";
        var newDescription = "Updated Description";

        // Act
        category.Update(newName, newDescription, 5);

        // Assert
        category.Name.Should().Be(newName);
        category.Description.Should().Be(newDescription);
        category.DisplayOrder.Should().Be(5);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var category = new Category("Test", "Test");

        // Act
        category.Deactivate();

        // Assert
        category.IsActive.Should().BeFalse();
    }
}
