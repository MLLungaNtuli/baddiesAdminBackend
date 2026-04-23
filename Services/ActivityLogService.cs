using Dapper;

public class ActivityLogService
{
    private readonly DbConnectionFactory _db;

    public ActivityLogService(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task LogAsync(
        Guid adminId,
        string action,
        string entity,
        Guid entityId,
        string description)
    {
        try
        {
            using var conn = _db.Create();
            
            // ✅ FIX: Changed 'entity' to 'entity_type' to match your database schema
            await conn.ExecuteAsync("""
                INSERT INTO activity_logs
                (admin_id, action, entity_type, entity_id, description, created_at)
                VALUES
                (@adminId, @action, @entityType, @entityId, @description, NOW())
            """, new 
            { 
                adminId, 
                action, 
                entityType = entity,  // Map the 'entity' parameter to 'entity_type' column
                entityId, 
                description 
            });
        }
        catch (Exception ex)
        {
            // Log the error but don't throw - logging shouldn't break the main operation
            Console.WriteLine($"Failed to log activity: {ex.Message}");
            // You might want to use ILogger here if available
        }
    }
}