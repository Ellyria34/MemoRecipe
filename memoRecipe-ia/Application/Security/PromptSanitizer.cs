using System.Text.RegularExpressions;

namespace MemoRecipeIA.Application.Security;

public class PromptInjectionDetectedException : Exception
{
    public string Pattern { get; }

    public PromptInjectionDetectedException(string pattern)
        : base($"Prompt injection pattern detected: {pattern}")
    {
        Pattern = pattern;
    }
}

public static class PromptSanitizer
{
    // OWASP LLM01 patterns — catalog 2024/2026 (direct injection, jailbreak, role hijack, safety bypass)
    private static readonly (string Name, Regex Pattern)[] SuspiciousPatterns =
    [
        ("ignore-instructions",   new Regex(@"\bignore\s+(all\s+|any\s+|previous\s+|prior\s+|above\s+)*(instructions?|context|rules?|prompts?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("disregard-instructions", new Regex(@"\bdisregard\s+(all\s+|any\s+|previous\s+|the\s+above\s+)*(instructions?|rules?|context|prompts?|safety)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("jailbreak-persona",     new Regex(@"\byou\s+are\s+now\s+(dan|jailbroken|no\s+longer|a\s+different|an?\s+unrestricted)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("role-hijack",           new Regex(@"^\s*(system|assistant|user)\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.Multiline)),
        ("markdown-role",         new Regex(@"```\s*(system|assistant|user)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("special-tokens",        new Regex(@"<\|(im_start|im_end|endoftext|system|user|assistant)\|>", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("end-instructions",      new Regex(@"\b(end[_\s-]of[_\s-]instructions?|new\s+instructions?\s*:|override\s+instructions?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("reveal-prompt",         new Regex(@"\b(reveal|print|show|repeat|output)\s+(your|the)\s+(system\s+)?(prompt|instructions?|rules?|context)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("act-as",                new Regex(@"\bact\s+as\s+(if\s+you\s+are\s+)?(a\s+different|an?\s+unrestricted|dan|no\s+longer)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
        ("safety-bypass",         new Regex(@"\b(bypass|forget|override|disable)\s+(all\s+|your\s+)?(safety|guidelines?|filters?|restrictions?)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)),
    ];

    public static bool HasSuspiciousPatterns(string? input, out string? matchedPattern)
    {
        matchedPattern = null;
        if (string.IsNullOrWhiteSpace(input)) return false;

        foreach (var (name, regex) in SuspiciousPatterns)
        {
            if (regex.IsMatch(input))
            {
                matchedPattern = name;
                return true;
            }
        }
        return false;
    }

    public static void Sanitize(string? input)
    {
        if (HasSuspiciousPatterns(input, out var pattern))
        {
            throw new PromptInjectionDetectedException(pattern!);
        }
    }
}
