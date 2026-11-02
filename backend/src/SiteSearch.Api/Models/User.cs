namespace SiteSearch.Api.Models;

public record User(string Username, IReadOnlyList<string> ProjectMemberships)
{
    public bool CanAccessProject(string? projectId)
    {
        // Regulations (null projectId) are globally visible to everyone.
        if (string.IsNullOrWhiteSpace(projectId))
            return true;

        // Project documents visible only to members.
        return ProjectMemberships.Contains(projectId, StringComparer.OrdinalIgnoreCase);
    }
}

public static class HardcodedUsers
{
    public static readonly IReadOnlyDictionary<string, User> All = new Dictionary<string, User>(StringComparer.OrdinalIgnoreCase)
    {
        ["asha"] = new User("asha", new[] { "P1" }),
        ["ravi"] = new User("ravi", new[] { "P2" }),
        ["meera"] = new User("meera", new[] { "P1", "P2" }),
        ["guest"] = new User("guest", Array.Empty<string>())
    };

    public static User GetUserOrDefault(string? username)
    {
        if (!string.IsNullOrWhiteSpace(username) && All.TryGetValue(username.Trim(), out var user))
        {
            return user;
        }
        return All["guest"];
    }
}
