using MemoRecipe.Web.Models;

namespace MemoRecipe.Web.Services;

public interface IFeatureFlagsService
{
    Task<FeatureFlagsDto> GetAsync();
}
