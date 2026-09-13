namespace ContentService.Application.Exceptions;

public class ContentNotFoundException : Exception
{
    public ContentNotFoundException(Guid contentId)
        : base($"Content with id '{contentId}' was not found.") { }
}

public class AuthorNotFoundException : Exception
{
    public AuthorNotFoundException(Guid authorId)
        : base($"Author (User) with id '{authorId}' was not found. Cannot create content for a non-existent user.") { }
}

public class UserServiceUnavailableException : Exception
{
    public UserServiceUnavailableException(string reason)
        : base($"User Service is currently unavailable: {reason}") { }
}
