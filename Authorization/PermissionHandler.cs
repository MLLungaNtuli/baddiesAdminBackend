// Authorization/PermissionHandler.cs
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly DbConnectionFactory _dbFactory;

    public PermissionHandler(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Fail();
            return;
        }

        var adminIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
        {
            context.Fail();
            return;
        }
        
        using var conn = _dbFactory.Create();
        
        // Check if user is super admin (bypasses all permissions)
        var isSuperAdmin = await conn.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(
                SELECT 1 
                FROM admin_role_assignments ara
                JOIN admin_roles ar ON ara.role_id = ar.id
                WHERE ara.admin_id = @adminId 
                AND ar.name = 'super_admin'
            )",
            new { adminId });
        
        if (isSuperAdmin)
        {
            context.Succeed(requirement);
            return;
        }
        
        // Check direct permissions
        var hasDirectPermission = await conn.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(
                SELECT 1 FROM admin_permissions 
                WHERE admin_id = @adminId 
                AND resource = @resource 
                AND action = @action
            )",
            new { adminId, requirement.Resource, requirement.Action });
        
        if (hasDirectPermission)
        {
            context.Succeed(requirement);
            return;
        }
        
        // Check role permissions
        var hasRolePermission = await conn.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(
                SELECT 1 
                FROM admin_role_assignments ara
                JOIN role_permissions rp ON ara.role_id = rp.role_id
                WHERE ara.admin_id = @adminId 
                AND rp.resource = @resource 
                AND rp.action = @action
            )",
            new { adminId, requirement.Resource, requirement.Action });
        
        if (hasRolePermission)
        {
            context.Succeed(requirement);
            return;
        }
        
        // Check wildcard permissions (*:*)
        var hasWildcardPermission = await conn.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(
                SELECT 1 
                FROM admin_role_assignments ara
                JOIN role_permissions rp ON ara.role_id = rp.role_id
                WHERE ara.admin_id = @adminId 
                AND rp.resource = '*' 
                AND rp.action = '*'
            )",
            new { adminId });
        
        if (hasWildcardPermission)
        {
            context.Succeed(requirement);
            return;
        }
        
        context.Fail();
    }
}