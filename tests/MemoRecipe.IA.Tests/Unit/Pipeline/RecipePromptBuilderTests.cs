using MemoRecipeIA.Application.Pipeline;
using Xunit;

namespace MemoRecipe.IA.Tests.Unit.Pipeline;

public class RecipePromptBuilderTests
{
    [Fact]
    public void BuildForText_IncludesOcrTextAndSchema()
    {
        var ocrText = "Cheesecake maison\nPour 8 parts\n225g de biscuits";
        var prompt = RecipePromptBuilder.BuildForText(ocrText);

        Assert.Contains(ocrText, prompt);
        Assert.Contains("\"ingredients\": [", prompt);
        Assert.Contains("\"steps\": [ string ]", prompt);
        Assert.Contains("Return raw JSON only", prompt);
        Assert.Contains("OCR TEXT:", prompt);
        Assert.Contains("<<<", prompt);
    }

    [Theory]
    [InlineData("description")]
    [InlineData("prepTimeMinutes")]
    [InlineData("cookTimeMinutes")]
    [InlineData("difficulty")]
    public void BuildForText_IncludesNewSchemaField(string fieldName)
    {
        var prompt = RecipePromptBuilder.BuildForText("dummy");
        Assert.Contains($"\"{fieldName}\":", prompt);
    }

    [Fact]
    public void BuildForVision_ReturnsSchemaWithoutOcrSection()
    {
        var prompt = RecipePromptBuilder.BuildForVision();

        Assert.Contains("\"title\":", prompt);
        Assert.Contains("\"ingredients\": [", prompt);
        Assert.DoesNotContain("OCR TEXT:", prompt);
        Assert.DoesNotContain("<<<", prompt);
    }

    [Fact]
    public void BuildForText_IncludesSecurityRulesAndSealedDelimiters()
    {
        var prompt = RecipePromptBuilder.BuildForText("dummy");

        Assert.Contains("CRITICAL SECURITY RULES", prompt);
        Assert.Contains("UNTRUSTED user data", prompt);
        Assert.Contains("<<<UNTRUSTED_START>>>", prompt);
        Assert.Contains("<<<UNTRUSTED_END>>>", prompt);
    }

    [Fact]
    public void BuildForVision_IncludesSecurityRulesForImageInjection()
    {
        var prompt = RecipePromptBuilder.BuildForVision();

        Assert.Contains("CRITICAL SECURITY RULES", prompt);
        Assert.Contains("text visible in the image", prompt);
    }
}
