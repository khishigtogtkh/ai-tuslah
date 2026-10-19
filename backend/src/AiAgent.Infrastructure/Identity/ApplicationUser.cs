using Microsoft.AspNetCore.Identity;

namespace AiAgent.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid> { }

public static class PlatformRoles
{
    public const string Admin = "PlatformAdmin";
}
