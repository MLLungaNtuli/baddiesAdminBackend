using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;  

[ApiController]
[Route("api/admin/escorts")]
[Authorize]  // Temporarily comment out while testing
public class AdminEscortsController : ControllerBase
{
    
    private readonly EscortStatisticsService _statisticsService;
    private readonly AdminEscortService _service;
    private readonly EscortImageService _imageService;
    private readonly ActivityLogService _logs;
    private readonly DbConnectionFactory _db;

    public AdminEscortsController(
        EscortStatisticsService statisticsService,
        AdminEscortService service,
        EscortImageService imageService,
        ActivityLogService logs,
        DbConnectionFactory db)
    {
        _statisticsService = statisticsService;
        _service = service;
        _imageService = imageService;
        _logs = logs;
        _db = db;
    }

    // GET /api/admin/escorts
    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    // GET /api/admin/escorts/{id}/profile
    [HttpGet("{id}/profile")]
    public async Task<IActionResult> GetProfile(Guid id)
    {
        var profile = await _service.GetProfileAsync(id);
        if (profile == null)
            return NotFound();
        
        return Ok(profile);
    }

    // PUT /api/admin/escorts/{id}/profile
    [HttpPut("{id}/profile")]
    public async Task<IActionResult> UpdateProfile(Guid id, UpdateEscortDto dto)
    {
        await _service.UpdateProfileAsync(id, dto);
        return Ok();
    }
    
    
    // POST /api/admin/escorts
// POST /api/admin/escorts
[HttpPost]
public async Task<IActionResult> Create(CreateEscortDto dto)
{
    var id = await _service.CreateAsync(dto);  // This line is fine if CreateAsync returns Guid
    return Ok(new { id = id, message = "Escort created successfully" });
}

    // PATCH /api/admin/escorts/{id}/active - FIXED: No adminId needed
    [HttpPatch("{id}/active")]
    public async Task<IActionResult> SetActive(Guid id, SetEscortActiveDto dto)
    {
        await _service.SetActiveAsync(id, dto.Active);
        return Ok();
    }

    // PATCH /api/admin/escorts/{id}/verify - FIXED: No adminId needed
    [HttpPatch("{id}/verify")]
    public async Task<IActionResult> Verify(Guid id)
    {
        await _service.VerifyAsync(id);
        return Ok();
    }

    // DELETE /api/admin/escorts/{id} - FIXED: Use email from token
    [HttpDelete("{id}")]
    public async Task<IActionResult> SoftDelete(Guid id, [FromBody] string reason = null)
    {
        // Get admin email from token if authenticated
        var adminEmail = User?.Identity?.IsAuthenticated == true 
            ? User.FindFirst(ClaimTypes.Name)?.Value 
            : "system";
        
        await _service.SoftDeleteAsync(id, adminEmail, reason ?? "No reason provided");
        return Ok();
    }

    // GET /api/admin/escorts/{id}/images
    [HttpGet("{id}/images")]
    public async Task<IActionResult> GetImages(Guid id)
        => Ok(await _imageService.GetByEscortAsync(id));

    // PATCH /api/admin/escorts/{id}/profile-image
    [HttpPatch("{id}/profile-image")]
    public async Task<IActionResult> SetProfileImage(Guid id, SetProfileImageDto dto)
    {
        await _service.SetProfileImageAsync(id, dto.ImageUrl);
        return Ok();
    }

    // GET /api/admin/escorts/stats
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
        => Ok(await _service.GetStatsAsync());

  [HttpGet("options/body-types")]
public async Task<IActionResult> GetBodyTypes()
{
    using var conn = _db.Create();  // <-- use Create() here
    var result = await conn.QueryAsync<string>("SELECT name FROM body_types ORDER BY name");
    return Ok(result);
}

[HttpGet("options/hair-colors")]
public async Task<IActionResult> GetHairColors()
{
    using var conn = _db.Create();
    var result = await conn.QueryAsync<string>("SELECT name FROM hair_colors ORDER BY name");
    return Ok(result);
}

[HttpGet("options/eye-colors")]
public async Task<IActionResult> GetEyeColors()
{
    using var conn = _db.Create();
    var result = await conn.QueryAsync<string>("SELECT name FROM eye_colors ORDER BY name");
    return Ok(result);
}

[HttpGet("options/ethnicities")]
public async Task<IActionResult> GetEthnicities()
{
    using var conn = _db.Create();
    var result = await conn.QueryAsync<string>("SELECT name FROM ethnicities ORDER BY name");
    return Ok(result);
}

[HttpGet("options/languages")]
public async Task<IActionResult> GetLanguages()
{
    using var conn = _db.Create();
    var result = await conn.QueryAsync<string>("SELECT name FROM languages ORDER BY name");
    return Ok(result);
}

[HttpGet("options/services")]
public async Task<IActionResult> GetServices()
{
    using var conn = _db.Create();
    var result = await conn.QueryAsync<string>("SELECT name FROM services ORDER BY name");
    return Ok(result);
}

[HttpGet("{id}/statistics")]
public async Task<IActionResult> GetStatistics(Guid id)
{
    try
    {
        var stats = await _statisticsService.GetStatisticsAsync(id);
        return Ok(stats);
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}

[HttpPost("{id}/track-view")]
public async Task<IActionResult> TrackView(Guid id, [FromBody] TrackViewDto dto)
{
    try
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        
        await _statisticsService.TrackProfileViewAsync(id, userId, ipAddress);
        return Ok();
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}

[HttpPost("{id}/track-contact")]
public async Task<IActionResult> TrackContact(Guid id)
{
    try
    {
        await _statisticsService.TrackContactClickAsync(id);
        return Ok();
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { error = ex.Message });
    }
}

public class TrackViewDto
{
    public string? UserId { get; set; }
}


}