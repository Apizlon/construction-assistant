namespace UserService.Application.Attributes;

/// <summary>
/// Attribute to specify required permissions for an action
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class NeedPermissionsAttribute : Attribute
{
    public string[] RequiredPermissions { get; }

    /// <summary>
    /// Specifies required permissions for an action
    /// </summary>
    /// <param name="requiredPermissions">Array of required permission strings</param>
    public NeedPermissionsAttribute(params string[] requiredPermissions)
    {
        RequiredPermissions = requiredPermissions ?? Array.Empty<string>();
    }
}