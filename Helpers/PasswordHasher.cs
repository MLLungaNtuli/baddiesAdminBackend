using BCrypt.Net;

public static class PasswordHasher
{
    private const int WorkFactor = 12;
    
    public static string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }
    
    public static bool Verify(string password, string hashedPassword)
    {
        if (string.IsNullOrEmpty(hashedPassword))
        {
            return false;
        }
        
        try
        {
            // Try standard verify first
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
        catch (Exception)
        {
            // If that fails, try without enhanced entropy
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword, false);
        }
    }
}