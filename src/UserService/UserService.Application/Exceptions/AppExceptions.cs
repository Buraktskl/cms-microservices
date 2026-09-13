namespace UserService.Application.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(Guid userId)
        : base($"User with id '{userId}' was not found.") { }
}

public class UserAlreadyExistsException : Exception
{
    public UserAlreadyExistsException(string field, string value)
        : base($"A user with {field} '{value}' already exists.") { }
}

public class UserDeletionFailedException : Exception
{
    public UserDeletionFailedException(Guid userId, string reason)
        : base($"Failed to delete user '{userId}': {reason}") { }
}
