namespace GameContent;

public sealed class ResourceNotFoundException(string message) : ResourceException(message);
