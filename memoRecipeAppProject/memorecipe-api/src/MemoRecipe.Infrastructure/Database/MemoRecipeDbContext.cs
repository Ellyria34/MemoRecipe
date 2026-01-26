using Microsoft.EntityFrameworkCore;
using MemoRecipe.Domain.Entities.Users;
using MemoRecipe.Domain.Entities.Recipes;
using MemoRecipe.Domain.Entities.Ingredients;
using MemoRecipe.Domain.Entities.Steps;
using MemoRecipe.Domain.Entities.RecipeImages;
using MemoRecipe.Domain.Entities.Categories;
using MemoRecipe.Domain.Entities.Comments;
using MemoRecipe.Domain.Entities.Favorites;
using MemoRecipe.Domain.Entities.Nutrition;
using MemoRecipe.Domain.Entities.Products;
using MemoRecipe.Domain.Entities.OCR;
using MemoRecipe.Domain.Entities.Sources;

namespace MemoRecipe.Infrastructure.Database;

public class MemoRecipeDbContext : DbContext{
    public MemoRecipeDbContext(DbContextOptions<MemoRecipeDbContext> options)
        : base(options)
    {
    }

    //DB Sets
    public DbSet<User> Users => Set<User>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<Step> Steps => Set<Step>();
    public DbSet<RecipeImage> RecipeImages => Set<RecipeImage>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RecipeCategory> RecipeCategories => Set<RecipeCategory>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<IngredientNutrition> IngredientNutritions => Set<IngredientNutrition>();
    public DbSet<FoodProduct> FoodProducts => Set<FoodProduct>();
    public DbSet<OCRExtraction> OCRExtractions => Set<OCRExtraction>();
    public DbSet<RecipeSource> RecipeSources => Set<RecipeSource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        //   Table: Users
        modelBuilder.Entity<User>()
            .HasMany(u => u.Recipes)
            .WithOne(r => r.User)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);


        //   Table: Recipes
        modelBuilder.Entity<Recipe>()
            .HasOne(r => r.OCRExtraction)
            .WithOne(o => o.Recipe)
            .HasForeignKey<OCRExtraction>(o => o.RecipeId);

        modelBuilder.Entity<Recipe>()
            .HasOne(r => r.RecipeSource)
            .WithOne(s => s.Recipe)
            .HasForeignKey<RecipeSource>(s => s.RecipeId);


        //   Table: RecipeCategory (Many-to-Many)
        modelBuilder.Entity<RecipeCategory>()
            .HasKey(rc => new { rc.RecipeId, rc.CategoryId });

        modelBuilder.Entity<RecipeCategory>()
            .HasOne(rc => rc.Recipe)
            .WithMany(r => r.RecipeCategories)
            .HasForeignKey(rc => rc.RecipeId);

        modelBuilder.Entity<RecipeCategory>()
            .HasOne(rc => rc.Category)
            .WithMany(c => c.RecipeCategories)
            .HasForeignKey(rc => rc.CategoryId);


        //   Table: Favorites (Many-to-Many simplified)
        modelBuilder.Entity<Favorite>()
            .HasKey(f => new { f.UserId, f.RecipeId });


        //   Table: IngredientNutrition
        modelBuilder.Entity<IngredientNutrition>()
            .Property(p => p.AllergensJson)
            .HasColumnType("jsonb");


        //   Table: OCRExtraction
        modelBuilder.Entity<OCRExtraction>()
            .Property(o => o.JsonData)
            .HasColumnType("jsonb");

 
        //   Table: RecipeSource
        modelBuilder.Entity<RecipeSource>()
            .Property(s => s.MetadataJson)
            .HasColumnType("jsonb");
    }
}