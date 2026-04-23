using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("api/admin")]
[ApiController]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly AdminEscortService _escortService;
    private readonly EscortImageService _imageService;

    public AdminController(
        AdminEscortService escortService,
        EscortImageService imageService)
    {
        _escortService = escortService;
        _imageService = imageService;
    }

    // GET: /api/admin/users
    [HttpGet("users")]
    public IActionResult GetAllUsers()
    {
        return Ok(new { message = "All users returned" });
    }

    // PATCH: /api/admin/images/{imageId}/approve
    [HttpPatch("images/{imageId}/approve")]
    public async Task<IActionResult> ApproveImage(Guid imageId)
    {
        var adminId = Guid.Parse(User.FindFirst("adminId")!.Value);
        await _imageService.ApproveImageAsync(imageId, adminId);
        return Ok(new { message = "Image approved" });
    }

    // PATCH: /api/admin/images/{imageId}/reject
    [HttpPatch("images/{imageId}/reject")]
    public async Task<IActionResult> RejectImage(Guid imageId, [FromBody] RejectImageDto dto)
    {
        var adminId = Guid.Parse(User.FindFirst("adminId")!.Value);
        await _imageService.RejectImageAsync(imageId, adminId, dto.Reason);
        return Ok(new { message = "Image rejected" });
    }


public class SetFeaturedDto
{
    public bool Featured { get; set; }
}   
}
