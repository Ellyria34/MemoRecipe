using AutoMapper;
using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.Mappings.Profiles;
using MemoRecipe.Application.Services.Recipes;
using MemoRecipe.Application.Tests.Fakes;
using MemoRecipe.Domain.Entities.Recipes;

namespace MemoRecipe.Application.Tests.Services;

public class RecipeServiceTests
{
    private readonly FakeRecipeRepository _repository;
    private readonly IMapper _mapper;
    private readonly RecipeService _service;

    public RecipeServiceTests()
    {
        _repository = new FakeRecipeRepository();

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<RecipeProfile>();
            cfg.AddProfile<IngredientProfile>();
            cfg.AddProfile<StepProfile>();
            cfg.AddProfile<CategoryProfile>();
        });
        _mapper = config.CreateMapper();

        _service = new RecipeService(_repository, _mapper);
    }

    [Fact]
    public async Task GetByIdAsync_WithOwnPublicRecipe_ReturnsRecipe()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Tarte aux pommes",
            UserId = userId,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(recipe);

        // Act
        var result = await _service.GetByIdAsync(recipe.Id, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tarte aux pommes", result.Title);
    }

    [Fact]
    public async Task GetByIdAsync_WithOtherUserPrivateRecipe_ReturnsNull()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Recette secrète",
            UserId = ownerId,
            IsPublic = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(recipe);

        // Act
        var result = await _service.GetByIdAsync(recipe.Id, otherUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_WithOtherUserPublicRecipe_ReturnsRecipe()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Recette publique",
            UserId = ownerId,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(recipe);

        // Act
        var result = await _service.GetByIdAsync(recipe.Id, otherUserId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Recette publique", result.Title);
    }

    [Fact]
    public async Task DeleteAsync_WithOwnRecipe_ReturnsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "À supprimer",
            UserId = userId,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(recipe);

        // Act
        var result = await _service.DeleteAsync(recipe.Id, userId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_WithOtherUserRecipe_ReturnsFalse()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Pas la mienne",
            UserId = ownerId,
            IsPublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(recipe);

        // Act
        var result = await _service.DeleteAsync(recipe.Id, otherUserId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetAllByUserAsync_ReturnsOnlyUserRecipes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        await _repository.AddAsync(new Recipe { Id = Guid.NewGuid(), Title = "Ma recette 1", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _repository.AddAsync(new Recipe { Id = Guid.NewGuid(), Title = "Ma recette 2", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _repository.AddAsync(new Recipe { Id = Guid.NewGuid(), Title = "Recette autre", UserId = otherUserId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        // Act
        var result = await _service.GetAllByUserAsync(userId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(userId, r.UserId));
    }

    [Fact]
    public async Task CreateAsync_ReturnsRecipeWithCorrectUserIdAndGeneratedId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new RecipeCreateDto { Title = "Nouvelle recette" };

        // Act
        var result = await _service.CreateAsync(dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Nouvelle recette", result.Title);
        Assert.Equal(userId, result.UserId);
        Assert.NotEqual(Guid.Empty, result.Id);
    }

    [Fact]
    public async Task CreateAsync_SetsCreatedAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new RecipeCreateDto { Title = "Recette avec date" };
        var before = DateTime.UtcNow;

        // Act
        var result = await _service.CreateAsync(dto, userId);

        // Assert
        Assert.True(result.CreatedAt >= before);
    }

    [Fact]
    public async Task UpdateAsync_WithOwnRecipe_UpdatesTitle()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Titre original",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(recipe);
        var dto = new RecipeUpdateDto { Title = "Nouveau titre" };

        // Act
        var result = await _service.UpdateAsync(recipe.Id, dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Nouveau titre", result.Title);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentRecipe_ReturnsNull()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new RecipeUpdateDto { Title = "Titre" };

        // Act
        var result = await _service.UpdateAsync(Guid.NewGuid(), dto, userId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_WithOtherUserRecipe_ReturnsNull()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Recette protégée",
            UserId = ownerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(recipe);
        var dto = new RecipeUpdateDto { Title = "Tentative de modification" };

        // Act
        var result = await _service.UpdateAsync(recipe.Id, dto, otherUserId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_PartialUpdate_DoesNotOverwriteNullFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Titre original",
            Description = "Description originale",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(recipe);
        var dto = new RecipeUpdateDto { Title = "Nouveau titre" }; // Description non envoyée

        // Act
        var result = await _service.UpdateAsync(recipe.Id, dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Nouveau titre", result.Title);
        Assert.Equal("Description originale", result.Description);
    }
}
