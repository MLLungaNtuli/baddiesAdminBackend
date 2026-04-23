using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Dapper;

namespace Baddies.Admin.Api.Controllers
{
    [Route("api/public")]
    [ApiController]
    [AllowAnonymous] // No authentication required
    public class PublicEscortsController : ControllerBase
    {
        private readonly DbConnectionFactory _db;
        private readonly EscortStatisticsService _statisticsService;
        private readonly ILogger<PublicEscortsController> _logger;

        public PublicEscortsController(
            DbConnectionFactory db,
            EscortStatisticsService statisticsService,
            ILogger<PublicEscortsController> logger)
        {
            _db = db;
            _statisticsService = statisticsService;
            _logger = logger;
        }

        // GET: api/public/escorts/activated
        [HttpGet("escorts/activated")]
        public async Task<IActionResult> GetActivatedEscorts()
        {
            using var conn = _db.Create();
            
            var escorts = await conn.QueryAsync<object>(@"
                SELECT 
                    id,
                    stage_name as ""stageName"",
                    age,
                    phone_number as ""phoneNumber"",
                    bio,
                    location,
                    price_per_hour as ""pricePerHour"",
                    profile_image as ""profileImage"",
                    nationality,
                    height,
                    weight,
                    bust,
                    waist,
                    hips,
                    hair_color as ""hairColor"",
                    eye_color as ""eyeColor"",
                    ethnicity,
                    languages,
                    services,
                    measurements,
                    body_type as ""bodyType"",
                    smoking,
                    drinking,
                    tattoos,
                    piercings,
                    availability_times as ""availabilityTimes"",
                    incall_rate as ""incallRate"",
                    outcall_rate as ""outcallRate"",
                    travel_radius as ""travelRadius"",
                    travel_fee as ""travelFee"",
                    rating,
                    featured,
                    verified
                FROM escorts 
                WHERE 
                    active = true 
                    AND verified = true 
                    AND deleted_at IS NULL
                ORDER BY featured DESC, rating DESC, created_at DESC
            ");
            
            // Get images for each escort
            var escortList = new List<object>();
            foreach (var escort in escorts)
            {
                var images = await conn.QueryAsync<object>(@"
                    SELECT 
                        id,
                        image_url as ""imageUrl"",
                        approved
                    FROM escort_images 
                    WHERE escort_id = @Id AND approved = true
                    ORDER BY created_at DESC
                ", new { Id = ((dynamic)escort).id });
                
                escortList.Add(new
                {
                    escort = escort,
                    images = images
                });
            }
            
            return Ok(escortList);
        }

        // GET: api/public/escorts/{id}
        [HttpGet("escorts/{id}")]
        public async Task<IActionResult> GetEscortById(Guid id)
        {
            using var conn = _db.Create();
            
            var escort = await conn.QueryFirstOrDefaultAsync<object>(@"
                SELECT 
                    id,
                    stage_name as ""stageName"",
                    age,
                    phone_number as ""phoneNumber"",
                    bio,
                    location,
                    price_per_hour as ""pricePerHour"",
                    profile_image as ""profileImage"",
                    nationality,
                    height,
                    weight,
                    bust,
                    waist,
                    hips,
                    hair_color as ""hairColor"",
                    eye_color as ""eyeColor"",
                    ethnicity,
                    languages,
                    services,
                    measurements,
                    body_type as ""bodyType"",
                    smoking,
                    drinking,
                    tattoos,
                    piercings,
                    availability_times as ""availabilityTimes"",
                    incall_rate as ""incallRate"",
                    outcall_rate as ""outcallRate"",
                    travel_radius as ""travelRadius"",
                    travel_fee as ""travelFee"",
                    rating,
                    featured,
                    verified
                FROM escorts 
                WHERE 
                    id = @Id 
                    AND active = true 
                    AND verified = true 
                    AND deleted_at IS NULL
            ", new { Id = id });
            
            if (escort == null)
                return NotFound();
            
            var images = await conn.QueryAsync<object>(@"
                SELECT 
                    id,
                    image_url as ""imageUrl"",
                    approved
                FROM escort_images 
                WHERE escort_id = @Id AND approved = true
                ORDER BY created_at DESC
            ", new { Id = id });
            
            return Ok(new
            {
                escort = escort,
                images = images
            });
        }

        // POST: api/public/escorts/{id}/track-view
        [HttpPost("escorts/{id}/track-view")]
        public async Task<IActionResult> TrackView(Guid id, [FromBody] TrackViewDto dto)
        {
            try
            {
                _logger.LogInformation("Tracking view for escort: {EscortId}, User: {UserId}", id, dto?.UserId);
                
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _statisticsService.TrackProfileViewAsync(id, dto?.UserId, ipAddress);
                
                return Ok(new { success = true, message = "View tracked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking view for escort: {EscortId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/public/escorts/{id}/track-contact
        [HttpPost("escorts/{id}/track-contact")]
        public async Task<IActionResult> TrackContact(Guid id)
        {
            try
            {
                _logger.LogInformation("Tracking contact click for escort: {EscortId}", id);
                
                await _statisticsService.TrackContactClickAsync(id);
                
                return Ok(new { success = true, message = "Contact click tracked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking contact click for escort: {EscortId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/public/escorts/{id}/track-image
        [HttpPost("escorts/{id}/track-image")]
        public async Task<IActionResult> TrackImage(Guid id)
        {
            try
            {
                _logger.LogInformation("Tracking image view for escort: {EscortId}", id);
                
                await _statisticsService.TrackImageViewAsync(id);
                
                return Ok(new { success = true, message = "Image view tracked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking image view for escort: {EscortId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/public/escorts/{id}/track-time
        [HttpPost("escorts/{id}/track-time")]
        public async Task<IActionResult> TrackTimeSpent(Guid id, [FromBody] TrackTimeDto dto)
        {
            try
            {
                _logger.LogInformation("Tracking time spent for escort: {EscortId}, Duration: {Duration}s", id, dto?.Duration);
                
                await _statisticsService.TrackTimeSpentAsync(id, dto?.Duration ?? 0);
                
                return Ok(new { success = true, message = "Time tracked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking time spent for escort: {EscortId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/public/escorts/{id}/track-share
        [HttpPost("escorts/{id}/track-share")]
        public async Task<IActionResult> TrackShare(Guid id)
        {
            try
            {
                _logger.LogInformation("Tracking share for escort: {EscortId}", id);
                
                using var conn = _db.Create();
                await conn.ExecuteAsync("""
                    INSERT INTO escort_stats (escort_id, share_count)
                    VALUES (@escortId, 1)
                    ON CONFLICT (escort_id) 
                    DO UPDATE SET 
                        share_count = escort_stats.share_count + 1,
                        updated_at = NOW()
                """, new { escortId = id });
                
                return Ok(new { success = true, message = "Share tracked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking share for escort: {EscortId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST: api/public/escorts/{id}/track-favorite
        [HttpPost("escorts/{id}/track-favorite")]
        public async Task<IActionResult> TrackFavorite(Guid id)
        {
            try
            {
                _logger.LogInformation("Tracking favorite for escort: {EscortId}", id);
                
                using var conn = _db.Create();
                await conn.ExecuteAsync("""
                    INSERT INTO escort_stats (escort_id, favorite_count)
                    VALUES (@escortId, 1)
                    ON CONFLICT (escort_id) 
                    DO UPDATE SET 
                        favorite_count = escort_stats.favorite_count + 1,
                        updated_at = NOW()
                """, new { escortId = id });
                
                return Ok(new { success = true, message = "Favorite tracked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error tracking favorite for escort: {EscortId}", id);
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // DTOs
    public class TrackViewDto
    {
        public string? UserId { get; set; }
    }

    public class TrackTimeDto
    {
        public int Duration { get; set; } // Duration in seconds
    }
}