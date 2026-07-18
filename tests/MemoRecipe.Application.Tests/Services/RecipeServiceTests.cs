using MemoRecipe.Application.DTOs.Recipes;
using MemoRecipe.Application.DTOs.Ingredients;
using MemoRecipe.Application.DTOs.Steps;
using MemoRecipe.Application.Services.Recipes;
using MemoRecipe.Application.Tests.Fakes;
using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Application.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;



namespace MemoRecipe.Application.Tests.Services;

public class RecipeServiceTests
{
    private readonly FakeRecipeRepository _repository;
    private readonly FakeUserRepository _userRepository;
    private readonly RecipeService _service;


    public RecipeServiceTests()
    {
        _repository = new FakeRecipeRepository();
        _userRepository = new FakeUserRepository();
        _service = new RecipeService(_repository, _userRepository, NullLogger<RecipeService>.Instance);
    }

    #region GetByIdAsync
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
    #endregion

    #region DeleteAsync
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
    #endregion

    #region GetAllByUserAsync
    [Fact]
    public async Task GetAllByUserAsync_ReturnsOnlyUserRecipes()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var queryParams = new RecipeQueryParams();

        await _repository.AddAsync(new Recipe { Id = Guid.NewGuid(), Title = "Ma recette 1", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _repository.AddAsync(new Recipe { Id = Guid.NewGuid(), Title = "Ma recette 2", UserId = userId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await _repository.AddAsync(new Recipe { Id = Guid.NewGuid(), Title = "Recette autre", UserId = otherUserId, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        // Act
        var result = await _service.GetAllByUserAsync(userId, queryParams);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(userId, r.UserId));
    }
    #endregion
    
    #region CreateAsync
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
        Assert.True(result.UpdatedAt >= before);
    }

    [Fact]
    public async Task CreateAsync_WithIngredients_ReturnsRecipeWithCorrectIngredients()
    {
        //// Arrange
        var userId = Guid.NewGuid();
        var dto = new RecipeCreateDto
        {
            Title = "Cheesecake",
            Ingredients = new List<IngredientCreateDto>
            {
                new IngredientCreateDto { Name = "Sucre", Quantity = 100, Unit = "g" },
                new IngredientCreateDto { Name = "Beurre", Quantity = 50, Unit = "g" }
            }
        };

        // Act
        var result = await _service.CreateAsync(dto, userId);

        // Assert
        Assert.Equal(2, result.Ingredients.Count);
        Assert.Equal("Sucre", result.Ingredients[0].Name);
        Assert.Equal(100, result.Ingredients[0].Quantity);
        Assert.Equal("g", result.Ingredients[0].Unit);
        Assert.Equal("Beurre", result.Ingredients[1].Name); 
        Assert.Equal(50, result.Ingredients[1].Quantity);
        Assert.Equal("g", result.Ingredients[1].Unit);
    }

    [Fact]
    public async Task CreateAsync_WithSteps_ReturnsRecipeWithCorrectSteps()
    {
        //// Arrange
        var userId = Guid.NewGuid();
        var dto = new RecipeCreateDto
        {
            Title = "Cheesecake",
            Steps = new List<StepCreateDto>
            {
                new StepCreateDto { Order = 1, Instruction = "instruction1" },
                new StepCreateDto { Order = 2, Instruction = "instruction2" }
            }
        };

        // Act
        var result = await _service.CreateAsync(dto, userId);

        // Assert
        Assert.Equal(2, result.Steps.Count);
        Assert.Equal(1, result.Steps[0].Order);
        Assert.Equal("instruction1", result.Steps[0].Instruction);
        Assert.Equal(2, result.Steps[1].Order);
        Assert.Equal("instruction2", result.Steps[1].Instruction);
    }

    [Fact]
    public async Task CreateAsync_WithoutIsPublicInDto_DefaultsToFalse()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new RecipeCreateDto
        {
            Title = "TestWithoutIsPublic",
        };

        // Act
        var result = await _service.CreateAsync(dto, userId);

        // Assert
        Assert.False(result.IsPublic);
    }

    [Fact]
    public async Task CreateAsync_WithIsPublicTrueInDto_PersistsTrue()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var dto = new RecipeCreateDto
        {
            Title = "TestWithoutIsPublic",
            IsPublic = true,
        };

        // Act
        var result = await _service.CreateAsync(dto, userId);

        // Assert
        Assert.True(result.IsPublic);
    }

    #endregion

    #region UpdateAsync
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
    public async Task UpdateAsync_UpdateAt()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Recette protégée",
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
        Assert.True(result.CreatedAt >= before);
        Assert.True(result.UpdatedAt >= before);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAllFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var before = DateTime.UtcNow;

        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            Title = "Recette protégée",
            UserId = userId,
            IsPublic = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(recipe);

        var dto = new RecipeUpdateDto 
        { 
            Title = "Nouveau titre",
            Description = "New Description",
            Servings = 8,
            PrepTimeMinutes = 20,
            CookTimeMinutes = 20,
            IsPublic = true,
            Difficulty = DifficultyLevel.Hard,
        };

        // Act
        var result = await _service.UpdateAsync(recipe.Id, dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Nouveau titre", result.Title);
        Assert.Equal("New Description", result.Description);
        Assert.Equal(8, result.Servings);
        Assert.Equal(40, result.TotalTimeMinutes);
        Assert.Equal(20, result.PrepTimeMinutes);
        Assert.Equal(20, result.CookTimeMinutes);
        Assert.True(result.IsPublic);
        Assert.Equal(userId, result.UserId);
        Assert.Equal(DifficultyLevel.Hard, result.Difficulty);
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
        var dto = new RecipeUpdateDto { Title = "Nouveau titre" };

        // Act
        var result = await _service.UpdateAsync(recipe.Id, dto, userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Nouveau titre", result.Title);
        Assert.Equal("Description originale", result.Description);
    }
    #endregion

    #region CountByUserAsync
    [Fact]
    public async Task CountByUserAsync_WithRecipes_ReturnNumberOffRecipe()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var recipes = new List<Recipe>
        {
            new Recipe()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "recette 1"
            },
            new Recipe()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = "recette 2"
            }
        };
        foreach(Recipe recipe in recipes)
        {
            await _repository.AddAsync(recipe);
        }

        // Act
        var result = await _service.CountByUserAsync(userId);

        // Assert
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task CountByUserAsync_WithNoRecipe_ReturnZero()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var result = await _service.CountByUserAsync(userId);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task CountByUserAsync_WithRecipesforOtherUsers_ReturnNumberOffRecipeForUser()
    {
        // Arrange
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();

        var recipesForUser1 = new List<Recipe>
        {
            new Recipe()
            {
                Id = Guid.NewGuid(),
                UserId = userId1,
                Title = "recette 1"
            },
            new Recipe()
            {
                Id = Guid.NewGuid(),
                UserId = userId1,
                Title = "recette 2"
            }
        };
        foreach(Recipe recipe in recipesForUser1)
        {
            await _repository.AddAsync(recipe);
        }

        var recipesForUser2 = new List<Recipe>
        {
            new Recipe()
            {
                Id = Guid.NewGuid(),
                UserId = userId2,
                Title = "recette 1bis"
            }
        };
        foreach(Recipe recipe in recipesForUser2)
        {
            await _repository.AddAsync(recipe);
        }


        // Act
        var result = await _service.CountByUserAsync(userId1);

        // Assert
        Assert.Equal(2, result);
    }
    #endregion

        [Fact]
    public async Task CreateAsync_WhenAccountIsMarkedForDeletion_ThrowsException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "marked@example.com",
            Username = "marked",
            DeleteRequestedAt = DateTime.UtcNow
        };
        await _userRepository.AddAsync(user);

        var dto = new RecipeCreateDto { Title = "Should fail" };

        // Act + Assert
        await Assert.ThrowsAsync<AccountMarkedForDeletionException>(
            () => _service.CreateAsync(dto, userId)
        );
    }
}
