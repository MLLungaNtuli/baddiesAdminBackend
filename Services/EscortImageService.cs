using Dapper;

public class EscortImageService
{
    private readonly DbConnectionFactory _db;

    public EscortImageService(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task SaveAsync(Guid escortId, string imageUrl)
    {
        using var conn = _db.Create();

        await conn.ExecuteAsync("""
            insert into escort_images (escort_id, image_url, approved)
            values (@escortId, @url, false)
        """, new { escortId, url = imageUrl });
    }

    public async Task ApproveImageAsync(Guid imageId, Guid adminId)
    {
        try
        {
            using var conn = _db.Create();
            
            // First check if the image exists
            var image = await conn.QueryFirstOrDefaultAsync<EscortImage>(
                "SELECT * FROM escort_images WHERE id = @imageId",
                new { imageId }
            );
            
            if (image == null)
            {
                throw new Exception($"Image with ID {imageId} not found");
            }
            
            // Update the image
            var rowsAffected = await conn.ExecuteAsync("""
                UPDATE escort_images 
                SET approved = true,
                    approved_at = NOW(),
                    approved_by = @adminId
                WHERE id = @imageId
            """, new { imageId, adminId });
            
            if (rowsAffected == 0)
            {
                throw new Exception($"Failed to update image {imageId}");
            }
            
            Console.WriteLine($"✅ Image {imageId} approved by admin {adminId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in ApproveImageAsync: {ex.Message}");
            throw;
        }
    }

    public async Task RejectImageAsync(Guid imageId, Guid adminId, string reason)
    {
        try
        {
            using var conn = _db.Create();
            
            // First check if the image exists
            var image = await conn.QueryFirstOrDefaultAsync<EscortImage>(
                "SELECT * FROM escort_images WHERE id = @imageId",
                new { imageId }
            );
            
            if (image == null)
            {
                throw new Exception($"Image with ID {imageId} not found");
            }
            
            // Update the image
            var rowsAffected = await conn.ExecuteAsync("""
                UPDATE escort_images 
                SET approved = false,
                    rejected_at = NOW(),
                    rejected_by = @adminId,
                    rejection_reason = @reason
                WHERE id = @imageId
            """, new { imageId, adminId, reason });
            
            if (rowsAffected == 0)
            {
                throw new Exception($"Failed to reject image {imageId}");
            }
            
            Console.WriteLine($"✅ Image {imageId} rejected by admin {adminId}. Reason: {reason}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in RejectImageAsync: {ex.Message}");
            throw;
        }
    }

    // ✅ ADD THIS DELETE METHOD
    public async Task DeleteImageAsync(Guid imageId, Guid adminId)
    {
        try
        {
            using var conn = _db.Create();
            
            // First check if the image exists
            var image = await conn.QueryFirstOrDefaultAsync<EscortImage>(
                "SELECT * FROM escort_images WHERE id = @imageId",
                new { imageId }
            );
            
            if (image == null)
            {
                throw new Exception($"Image with ID {imageId} not found");
            }
            
            // Delete the image from database
            var rowsAffected = await conn.ExecuteAsync("""
                DELETE FROM escort_images 
                WHERE id = @imageId
            """, new { imageId });
            
            if (rowsAffected == 0)
            {
                throw new Exception($"Failed to delete image {imageId}");
            }
            
            Console.WriteLine($"✅ Image {imageId} deleted by admin {adminId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in DeleteImageAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<IEnumerable<EscortImage>> GetByEscortAsync(Guid escortId)
    {
        using var conn = _db.Create();
        return await conn.QueryAsync<EscortImage>(
            "SELECT * FROM escort_images WHERE escort_id = @escortId ORDER BY created_at DESC",
            new { escortId }
        );
    }
}