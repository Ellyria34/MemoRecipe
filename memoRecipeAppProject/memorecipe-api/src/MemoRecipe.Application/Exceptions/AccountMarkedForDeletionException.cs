namespace MemoRecipe.Application.Exceptions;

public class AccountMarkedForDeletionException : Exception
{
    public AccountMarkedForDeletionException()
        : base("Your account is marked for deletion. You cannot create or modify recipes.")
    {
    }

    
}
