namespace MemoRecipe.Application.DTOs.Recipes
{
    public class RecipeQueryParams
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? OrderBy { get; set; }
        public bool Descending { get; set; } = true;
        public int? Limit { get; set; }
    }
}
