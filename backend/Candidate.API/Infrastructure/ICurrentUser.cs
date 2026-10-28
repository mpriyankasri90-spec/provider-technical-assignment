namespace Candidate.API.Infrastructure;

/// <summary>
/// Identity used for audit columns. Authentication is out of scope for this assignment,
/// so <see cref="ConfiguredCurrentUser"/> returns a configured name; a real implementation
/// would read the authenticated principal from <c>HttpContext.User</c>.
/// </summary>
public interface ICurrentUser
{
    string UserName { get; }
}

public sealed class ConfiguredCurrentUser(IConfiguration configuration) : ICurrentUser
{
    public string UserName { get; } = configuration["Audit:DefaultUser"] ?? "system";
}
