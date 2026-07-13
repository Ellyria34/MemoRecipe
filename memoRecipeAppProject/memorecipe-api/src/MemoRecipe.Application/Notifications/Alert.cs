namespace MemoRecipe.Application.Notifications;

public record Alert(
    AlertLevel Level,
    string Title,
    string Message,
    DateTimeOffset OccurredAt);
