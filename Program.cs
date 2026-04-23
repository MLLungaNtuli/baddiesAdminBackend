using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Http.Features;
using System.Text;
using Supabase;
using Npgsql;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi.Models;
using Baddies.Admin.Data;
using Baddies.Admin.Api.Services.Security;

Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// Log startup
// ============================================
Console.WriteLine("========================================");
Console.WriteLine("🚀 Starting Baddies Admin API...");
Console.WriteLine("========================================");

// ============================================
// Configuration Debugging
// ============================================
Console.WriteLine("\n📋 Configuration Check:");
Console.WriteLine($"Environment: {builder.Environment.EnvironmentName}");

// Check all possible connection strings
var supabaseConnection = builder.Configuration.GetConnectionString("SupabaseDb");
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

Console.WriteLine($"SupabaseDb Connection: {(string.IsNullOrEmpty(supabaseConnection) ? "❌ NOT FOUND" : "✅ FOUND")}");
Console.WriteLine($"DefaultConnection: {(string.IsNullOrEmpty(defaultConnection) ? "❌ NOT FOUND" : "✅ FOUND")}");

// Use the first available connection string
var connectionString = supabaseConnection ?? defaultConnection;

if (string.IsNullOrEmpty(connectionString))
{
    Console.WriteLine("\n❌ CRITICAL ERROR: No database connection string found!");
    Console.WriteLine("Please check your appsettings.json file.");
}
else
{
    Console.WriteLine($"\n✅ Using connection string: {connectionString[..Math.Min(60, connectionString.Length)]}...");
    
    // Test PostgreSQL connection
    try
    {
        Console.WriteLine("\n🔧 Testing PostgreSQL connection...");
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        Console.WriteLine("✅ PostgreSQL connection successful!");
        
        // Get PostgreSQL version
        using var cmd = new NpgsqlCommand("SELECT version();", conn);
        var version = await cmd.ExecuteScalarAsync();
        Console.WriteLine($"📊 PostgreSQL Version: {version}");
        
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n❌ PostgreSQL connection failed:");
        Console.WriteLine($"Error: {ex.Message}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner Error: {ex.InnerException.Message}");
        }
    }
}

// ============================================
// JWT Configuration
// ============================================
var jwtSecret = builder.Configuration["Jwt:Key"];
if (string.IsNullOrEmpty(jwtSecret))
{
    Console.WriteLine("\n⚠️ Warning: JWT Secret is not configured!");
}
else
{
    Console.WriteLine($"\n✅ JWT Secret configured");
}

// Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? 
    throw new Exception("JWT Key is missing in configuration");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "BaddiesAdmin";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "BaddiesAdminUsers";

// Configure Authentication
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)
            ),

            ClockSkew = TimeSpan.Zero,

            // 🔥 THESE MUST BE INSIDE TokenValidationParameters
            RoleClaimType = "role",
            NameClaimType = ClaimTypes.NameIdentifier
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"❌ Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"✅ Token validated for: {context.Principal?.Identity?.Name}");
                return Task.CompletedTask;
            }
        };
    });

// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin", "SuperAdmin"));
    
    // Add your custom policies here
    options.AddPolicy("Escorts.View", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "escorts", "view").Result));
    
    options.AddPolicy("Escorts.Create", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "escorts", "create").Result));
    
    options.AddPolicy("Escorts.Edit", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "escorts", "edit").Result));
    
    options.AddPolicy("Escorts.Delete", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "escorts", "delete").Result));
    
    options.AddPolicy("Escorts.Verify", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "escorts", "verify").Result));
    
    options.AddPolicy("Images.View", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "images", "view").Result));
    
    options.AddPolicy("Images.Approve", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "images", "approve").Result));
    
    options.AddPolicy("Images.Reject", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "images", "reject").Result));
    
    options.AddPolicy("Users.View", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "users", "view").Result));
    
    options.AddPolicy("Users.Manage", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "users", "manage").Result));
    
    options.AddPolicy("Admins.View", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "admins", "view").Result));
    
    options.AddPolicy("Admins.Manage", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "admins", "manage").Result));
    
    options.AddPolicy("Bookings.View", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "bookings", "view").Result));
    
    options.AddPolicy("Bookings.Manage", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "bookings", "manage").Result));
    
    options.AddPolicy("Logs.View", policy =>
        policy.RequireAssertion(context => 
            CheckPermissionAsync(context, "logs", "view").Result));
});

// Helper function for permission checking
async Task<bool> CheckPermissionAsync(AuthorizationHandlerContext context, string resource, string action)
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
        return false;

    var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrEmpty(adminId))
        return false;

    try
    {
        // Get DbConnectionFactory from service provider
        var serviceProvider = builder.Services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbFactory = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();
        
        using var conn = dbFactory.Create();
        
        // Check if user is super admin
        var isSuperAdmin = await conn.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(
                SELECT 1 
                FROM admin_role_assignments ara
                JOIN admin_roles ar ON ara.role_id = ar.id
                WHERE ara.admin_id = @adminId 
                AND ar.name = 'super_admin'
            )",
            new { adminId = Guid.Parse(adminId) });
        
        if (isSuperAdmin)
            return true;
        
        // Check direct permissions
        var hasDirectPermission = await conn.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(
                SELECT 1 FROM admin_permissions 
                WHERE admin_id = @adminId 
                AND resource = @resource 
                AND action = @action
            )",
            new { 
                adminId = Guid.Parse(adminId), 
                resource, 
                action 
            });
        
        if (hasDirectPermission)
            return true;
        
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
            new { 
                adminId = Guid.Parse(adminId), 
                resource, 
                action 
            });
        
        if (hasRolePermission)
            return true;
        
        // Check wildcard permissions
        var hasWildcardPermission = await conn.ExecuteScalarAsync<bool>(
            @"SELECT EXISTS(
                SELECT 1 
                FROM admin_role_assignments ara
                JOIN role_permissions rp ON ara.role_id = rp.role_id
                WHERE ara.admin_id = @adminId 
                AND rp.resource = '*' 
                AND rp.action = '*'
            )",
            new { adminId = Guid.Parse(adminId) });
        
        return hasWildcardPermission;
    }
    catch (Exception)
    {
        return false;
    }
}

// Memory Cache
builder.Services.AddMemoryCache();

// HttpContext Accessor
builder.Services.AddHttpContextAccessor();

// ============================================
// Dependency Injection
// ============================================
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<EnhancedJwtService>();
builder.Services.AddScoped<RateLimiter>();
builder.Services.AddScoped<RateLimiterService>(); // If you have this
builder.Services.AddMemoryCache();
builder.Services.AddScoped<AdminEscortService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<RateLimiterService>();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<EscortImageService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddSingleton<SupabaseService>();
builder.Services.AddScoped<EscortStatisticsService>();

// ============================================
// Controllers & File Upload
// ============================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 5_000_000;
});

// ============================================
// Supabase Configuration
// ============================================
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:ServiceRoleKey"];

if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(supabaseKey))
{
    Console.WriteLine("\n⚠️ Warning: Supabase configuration is missing!");
}
else
{
    Console.WriteLine($"\n✅ Supabase URL configured: {supabaseUrl}");
}

// Initialize Supabase client
Client? supabaseClient = null;
try
{
    Console.WriteLine("\n🔧 Initializing Supabase client...");
    supabaseClient = new Client(
        builder.Configuration["Supabase:Url"]!,
        builder.Configuration["Supabase:ServiceRoleKey"]!
    );

    await supabaseClient.InitializeAsync();
    Console.WriteLine("✅ Supabase client initialized successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Supabase initialization failed: {ex.Message}");
}

// Register Supabase client as singleton if it was successfully created
if (supabaseClient != null)
{
    builder.Services.AddSingleton(supabaseClient);
}

// Also register a factory for dependency injection
builder.Services.AddScoped<Client>(sp => 
{
    var config = sp.GetRequiredService<IConfiguration>();
    return new Client(
        config["Supabase:Url"]!,
        config["Supabase:ServiceRoleKey"]!
    );
});

// ============================================
// CORS Configuration
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173","http://localhost:5174","http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ============================================
// Swagger
// ============================================
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Baddies Admin API", 
        Version = "v1" 
    });
    
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================
// BUILD THE APP
// ============================================
var app = builder.Build();

// ============================================
// Middleware Pipeline
// ============================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    Console.WriteLine("\n✅ Swagger enabled for development");
}

app.UseCors("AllowFrontend");

// Add your custom middleware extensions (these should be in separate files, but defined here for now)
// app.Use(async (context, next) =>
// {
//     // Security headers middleware
//     // context.Response.Headers.Add("X-Frame-Options", "DENY");
//     // context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
//     // context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
//     // context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");
    
//     await next();
// });

app.UseAuthentication();
app.UseAuthorization();

// Use your custom middleware
app.UseSecurityHeadersMiddleware();
app.UseAuditMiddleware();

// Audit middleware
app.Use(async (context, next) =>
{
    var startTime = DateTime.UtcNow;
    try
    {
        await next();
        var duration = DateTime.UtcNow - startTime;
        
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var adminId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"AUDIT: {adminId} {context.Request.Method} {context.Request.Path} {context.Response.StatusCode} ({duration.TotalMilliseconds}ms)");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"AUDIT ERROR: {context.Request.Method} {context.Request.Path} - {ex.Message}");
        throw;
    }
});

app.MapControllers();

// Handle preflight OPTIONS requests
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }
    await next();
});

app.MapGet("/", () => "Baddies Admin API is running!");

// ============================================
// Database Seeding
// ============================================
try
{
    Console.WriteLine("\n🌱 Starting database seeding...");
    
    if (string.IsNullOrEmpty(connectionString))
    {
        Console.WriteLine("❌ Cannot seed database: Connection string is null or empty");
    }
    else
    {
        Console.WriteLine("1. Seeding admin user...");
        
        // Get the seeding password from configuration or use default
        var adminEmail = builder.Configuration["Admin:Email"] ?? "admin@baddies.com";
        var adminPassword = builder.Configuration["Admin:Password"] ?? "Admin@1021998";
        
        Console.WriteLine($"   Admin email: {adminEmail}");
        Console.WriteLine($"   Admin password length: {adminPassword.Length} characters");
        
        // Use the existing DbSeeder.SeedAdmin method
        await DbSeeder.SeedAdmin(
            connectionString,
            adminEmail,
            adminPassword
        );
        
        Console.WriteLine("   ✅ Admin seeding completed!");
        
        Console.WriteLine("\n2. Seeding escorts...");
        await DbSeeder.SeedEscorts(connectionString);
        Console.WriteLine("   ✅ Escort seeding completed!");
        await DbSeeder.SeedLookupData(connectionString);
        Console.WriteLine("   ✅ LookupData seeding completed!");
        Console.WriteLine("\n🎉 Database seeding completed successfully!");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Database seeding failed:");
    Console.WriteLine($"   Error: {ex.Message}");
    Console.WriteLine($"   Stack Trace: {ex.StackTrace}");
    
    if (ex.InnerException != null)
    {
        Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
    }
}

// ============================================
// Seed admin user on startup (additional seeding)
// ============================================
async Task SeedAdminUserOnStartup()
{
    using var scope = app.Services.CreateScope();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var dbFactory = scope.ServiceProvider.GetRequiredService<DbConnectionFactory>();
    var logger = scope.ServiceProvider.GetService<ILogger<Program>>();
    
    try
    {
        using var conn = dbFactory.Create();
        
        // Check if admin exists
        var adminEmail = config["Admin:Email"] ?? "admin@baddies.com";
        var adminExists = await conn.ExecuteScalarAsync<bool>(
            "SELECT EXISTS(SELECT 1 FROM admins WHERE email = @email)",
            new { email = adminEmail });
        
        if (!adminExists)
        {
            var adminPassword = config["Admin:Password"] ?? "Admin@1021998";
            var passwordHash = PasswordHasher.Hash(adminPassword);
            
            // Insert admin
            var adminId = await conn.ExecuteScalarAsync<Guid>(
                @"INSERT INTO admins 
                  (email, username, password_hash, full_name, role, created_at, updated_at)
                  VALUES 
                  (@email, @username, @passwordHash, @fullName, @role, NOW(), NOW())
                  RETURNING id",
                new
                {
                    email = adminEmail,
                    username = "admin",
                    passwordHash,
                    fullName = "System Administrator",
                    role = "super_admin"
                });
            
            // Check if admin_roles table exists and has super_admin
            var superAdminRoleExists = await conn.ExecuteScalarAsync<bool>(
                "SELECT EXISTS(SELECT 1 FROM admin_roles WHERE name = 'super_admin')");
            
            if (superAdminRoleExists)
            {
                var superAdminRoleId = await conn.ExecuteScalarAsync<Guid>(
                    "SELECT id FROM admin_roles WHERE name = 'super_admin'");
                
                await conn.ExecuteAsync(
                    @"INSERT INTO admin_role_assignments 
                      (admin_id, role_id, assigned_at)
                      VALUES 
                      (@adminId, @roleId, NOW())",
                    new { adminId, roleId = superAdminRoleId });
            }
            
            logger?.LogInformation("✅ Admin user seeded successfully");
        }
        else
        {
            logger?.LogInformation("✅ Admin user already exists");
        }
    }
    catch (Exception ex)
    {
        logger?.LogError(ex, "❌ Failed to seed admin user");
    }
}

// Run the additional seeding
await SeedAdminUserOnStartup();

// ============================================
// Application Startup
// ============================================
Console.WriteLine("\n========================================");
Console.WriteLine($"📅 Application starting on: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
Console.WriteLine("========================================\n");

try
{
    Console.WriteLine("🚀 Baddies Admin API is running...");
    Console.WriteLine($"📡 Listening on: http://localhost:5068");
    Console.WriteLine($"🔗 Swagger UI: http://localhost:5068/swagger");
    Console.WriteLine("🔑 Admin Login: admin@baddies.com / Admin@1021998");
    Console.WriteLine("\nPress Ctrl+C to stop\n");
    
    await app.RunAsync("http://localhost:5068");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Application failed to start:");
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
    throw;
}

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeadersMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }

    public static IApplicationBuilder UseAuditMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuditMiddleware>();
    }
}