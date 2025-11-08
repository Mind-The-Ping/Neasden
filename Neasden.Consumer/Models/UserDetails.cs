using Neasden.Models;

namespace Neasden.Consumer.Models;
public record UserDetails(
    Guid Id,
    string PhoneNumber,
    PhoneOS PhoneOS);
