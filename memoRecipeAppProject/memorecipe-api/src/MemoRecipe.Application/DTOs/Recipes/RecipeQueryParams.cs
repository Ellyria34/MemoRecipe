using System.ComponentModel.DataAnnotations;

namespace MemoRecipe.Application.DTOs.Recipes
{
    public class RecipeQueryParams
    {
        [Range(1, int.MaxValue, ErrorMessage = "Page must be >= 1")]
        public int Page { get; set; } = 1;

        [Range(1, 50, ErrorMessage = "PageSize must be between 1 and 50")]
        public int PageSize { get; set; } = 10;
        
        public string? OrderBy { get; set; }
        public bool Descending { get; set; } = true;

    }
}
