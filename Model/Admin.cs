public class Admin
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "admin";
    public bool MfaEnabled { get; set; }
    public string? MfaSecret { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public DateTime? LastLogin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}


// using Supabase.Postgrest.Attributes;
// using Supabase.Postgrest.Models;

// [Table("admins")] // ← ADD THIS ATTRIBUTE
// public class Admin : BaseModel
// {
//     [PrimaryKey("id")] // ← ADD THIS ATTRIBUTE
//     public int Id { get; set; } // Change from int to Guid
    
//     [Column("username")]
//     public string Username { get; set; } = "";
    
//     [Column("password")]
//     public string Password { get; set; } = "";
    
//     [Column("created_at")]
//     public DateTime CreatedAt { get; set; }
// }