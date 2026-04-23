using Dapper;

public class EscortStatisticsService
{
    private readonly DbConnectionFactory _db;

    public EscortStatisticsService(DbConnectionFactory db)
    {
        _db = db;
    }

    public async Task TrackProfileViewAsync(Guid escortId, string userId = null, string ipAddress = null)
    {
        using var conn = _db.Create();
        
        // Update main stats
        await conn.ExecuteAsync("""
            INSERT INTO escort_stats (escort_id, profile_views, last_viewed_at)
            VALUES (@escortId, 1, NOW())
            ON CONFLICT (escort_id) 
            DO UPDATE SET 
                profile_views = escort_stats.profile_views + 1,
                last_viewed_at = NOW(),
                updated_at = NOW()
        """, new { escortId });
        
        // Update daily stats
        await conn.ExecuteAsync("""
            INSERT INTO escort_daily_stats (escort_id, date, profile_views)
            VALUES (@escortId, CURRENT_DATE, 1)
            ON CONFLICT (escort_id, date) 
            DO UPDATE SET 
                profile_views = escort_daily_stats.profile_views + 1
        """, new { escortId });
        
        // Track unique visitors if userId or IP provided
        if (!string.IsNullOrEmpty(userId) || !string.IsNullOrEmpty(ipAddress))
        {
            await TrackUniqueVisitorAsync(escortId, userId, ipAddress);
        }
    }

    private async Task TrackUniqueVisitorAsync(Guid escortId, string userId, string ipAddress)
    {
        using var conn = _db.Create();
        
        var visitorKey = userId ?? ipAddress;
        
        // Check if this visitor has viewed in the last 24 hours
        var hasViewedRecently = await conn.QueryFirstOrDefaultAsync<bool?>("""
            SELECT EXISTS (
                SELECT 1 FROM escort_daily_stats 
                WHERE escort_id = @escortId 
                AND date = CURRENT_DATE
                AND unique_visitors_log::jsonb @> @visitorKey::jsonb
            )
        """, new { escortId, visitorKey });
        
        if (!hasViewedRecently.GetValueOrDefault())
        {
            await conn.ExecuteAsync("""
                UPDATE escort_stats 
                SET unique_visitors = unique_visitors + 1
                WHERE escort_id = @escortId
            """, new { escortId });
        }
    }

    public async Task TrackContactClickAsync(Guid escortId)
    {
        using var conn = _db.Create();
        
        await conn.ExecuteAsync("""
            INSERT INTO escort_stats (escort_id, contact_clicks)
            VALUES (@escortId, 1)
            ON CONFLICT (escort_id) 
            DO UPDATE SET 
                contact_clicks = escort_stats.contact_clicks + 1,
                updated_at = NOW()
        """, new { escortId });
        
        await conn.ExecuteAsync("""
            INSERT INTO escort_daily_stats (escort_id, date, contact_clicks)
            VALUES (@escortId, CURRENT_DATE, 1)
            ON CONFLICT (escort_id, date) 
            DO UPDATE SET 
                contact_clicks = escort_daily_stats.contact_clicks + 1
        """, new { escortId });
    }

    public async Task TrackImageViewAsync(Guid escortId)
    {
        using var conn = _db.Create();
        
        await conn.ExecuteAsync("""
            INSERT INTO escort_stats (escort_id, image_views)
            VALUES (@escortId, 1)
            ON CONFLICT (escort_id) 
            DO UPDATE SET 
                image_views = escort_stats.image_views + 1,
                updated_at = NOW()
        """, new { escortId });
        
        await conn.ExecuteAsync("""
            INSERT INTO escort_daily_stats (escort_id, date, image_views)
            VALUES (@escortId, CURRENT_DATE, 1)
            ON CONFLICT (escort_id, date) 
            DO UPDATE SET 
                image_views = escort_daily_stats.image_views + 1
        """, new { escortId });
    }

    public async Task<EscortStatisticsDto> GetStatisticsAsync(Guid escortId)
    {
        using var conn = _db.Create();
        
        var stats = await conn.QueryFirstOrDefaultAsync<EscortStatisticsDto>("""
            SELECT 
                COALESCE(profile_views, 0) as ProfileViews,
                COALESCE(unique_visitors, 0) as UniqueVisitors,
                COALESCE(contact_clicks, 0) as ContactClicks,
                COALESCE(image_views, 0) as ImageViews,
                COALESCE(share_count, 0) as ShareCount,
                COALESCE(favorite_count, 0) as FavoriteCount,
                COALESCE(average_time_spent, 0) as AverageTimeSpent,
                last_viewed_at as LastViewedAt,
                updated_at as LastUpdatedAt
            FROM escort_stats 
            WHERE escort_id = @escortId
        """, new { escortId });
        
        // Get last 7 days of views for chart
        var weeklyViews = await conn.QueryAsync<DailyStatDto>("""
            SELECT 
                date,
                profile_views as Views,
                unique_visitors as UniqueVisitors,
                contact_clicks as ContactClicks
            FROM escort_daily_stats 
            WHERE escort_id = @escortId 
            AND date >= CURRENT_DATE - INTERVAL '7 days'
            ORDER BY date DESC
        """, new { escortId });
        
        if (stats != null)
        {
            stats.WeeklyViews = weeklyViews.ToList();
        }
        
        return stats ?? new EscortStatisticsDto();
    }

    // Add this method to your existing EscortStatisticsService class
public async Task TrackTimeSpentAsync(Guid escortId, int durationSeconds)
{
    using var conn = _db.Create();
    
    // Update average time spent
    await conn.ExecuteAsync("""
        INSERT INTO escort_stats (escort_id, average_time_spent, total_time_spent, total_visits)
        VALUES (@escortId, @duration, @duration, 1)
        ON CONFLICT (escort_id) 
        DO UPDATE SET 
            total_time_spent = escort_stats.total_time_spent + @duration,
            total_visits = escort_stats.total_visits + 1,
            average_time_spent = (escort_stats.total_time_spent + @duration)::DECIMAL / (escort_stats.total_visits + 1),
            updated_at = NOW()
    """, new { escortId, duration = durationSeconds });
    
    // Update daily stats
    await conn.ExecuteAsync("""
        INSERT INTO escort_daily_stats (escort_id, date, total_time_spent, visit_count)
        VALUES (@escortId, CURRENT_DATE, @duration, 1)
        ON CONFLICT (escort_id, date) 
        DO UPDATE SET 
            total_time_spent = escort_daily_stats.total_time_spent + @duration,
            visit_count = escort_daily_stats.visit_count + 1,
            average_time_spent = (escort_daily_stats.total_time_spent + @duration)::DECIMAL / (escort_daily_stats.visit_count + 1)
    """, new { escortId, duration = durationSeconds });
}
}

public class EscortStatisticsDto
{
    public int ProfileViews { get; set; }
    public int UniqueVisitors { get; set; }
    public int ContactClicks { get; set; }
    public int ImageViews { get; set; }
    public int ShareCount { get; set; }
    public int FavoriteCount { get; set; }
    public decimal AverageTimeSpent { get; set; }
    public DateTime? LastViewedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public List<DailyStatDto> WeeklyViews { get; set; } = new();
}

public class DailyStatDto
{
    public DateTime Date { get; set; }
    public int Views { get; set; }
    public int UniqueVisitors { get; set; }
    public int ContactClicks { get; set; }
}