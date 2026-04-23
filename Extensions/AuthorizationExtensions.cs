using Microsoft.AspNetCore.Authorization;

public static class AuthorizationExtensions
{
    public static void AddRoleBasedAuthorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
        
        services.AddAuthorization(options =>
        {
            // Escort management policies
            options.AddPolicy("Escorts.View", policy =>
                policy.Requirements.Add(new PermissionRequirement("escorts", "view")));
            
            options.AddPolicy("Escorts.Create", policy =>
                policy.Requirements.Add(new PermissionRequirement("escorts", "create")));
            
            options.AddPolicy("Escorts.Edit", policy =>
                policy.Requirements.Add(new PermissionRequirement("escorts", "edit")));
            
            options.AddPolicy("Escorts.Delete", policy =>
                policy.Requirements.Add(new PermissionRequirement("escorts", "delete")));
            
            options.AddPolicy("Escorts.Verify", policy =>
                policy.Requirements.Add(new PermissionRequirement("escorts", "verify")));
            
            // Image management policies
            options.AddPolicy("Images.View", policy =>
                policy.Requirements.Add(new PermissionRequirement("images", "view")));
            
            options.AddPolicy("Images.Approve", policy =>
                policy.Requirements.Add(new PermissionRequirement("images", "approve")));
            
            options.AddPolicy("Images.Reject", policy =>
                policy.Requirements.Add(new PermissionRequirement("images", "reject")));
            
            // User management policies
            options.AddPolicy("Users.View", policy =>
                policy.Requirements.Add(new PermissionRequirement("users", "view")));
            
            options.AddPolicy("Users.Manage", policy =>
                policy.Requirements.Add(new PermissionRequirement("users", "manage")));
            
            // Admin management policies
            options.AddPolicy("Admins.View", policy =>
                policy.Requirements.Add(new PermissionRequirement("admins", "view")));
            
            options.AddPolicy("Admins.Manage", policy =>
                policy.Requirements.Add(new PermissionRequirement("admins", "manage")));
            
            // Booking management policies
            options.AddPolicy("Bookings.View", policy =>
                policy.Requirements.Add(new PermissionRequirement("bookings", "view")));
            
            options.AddPolicy("Bookings.Manage", policy =>
                policy.Requirements.Add(new PermissionRequirement("bookings", "manage")));
            
            // Activity logs
            options.AddPolicy("Logs.View", policy =>
                policy.Requirements.Add(new PermissionRequirement("logs", "view")));
        });
    }
}