using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Neasden.Models;

public record UnReadNotification(
    [property: BsonRepresentation(BsonType.String)]
    Guid UserId,
    [property: BsonId]
    [property: BsonRepresentation(BsonType.String)]
    Guid NotificationId,
    DateTime CreatedAt);