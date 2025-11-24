namespace Neasden.Models;

public record UserDeleted(
    Guid UserId, 
    DateTime DeletedAt);