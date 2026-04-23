using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;  // ✅ ADD THIS - Required for ClaimTypes

[ApiController]
[Route("api/admin/escort-images")]
[Authorize]
public class AdminEscortImagesController : ControllerBase
{
    private readonly EscortImageService _service;
    private readonly ActivityLogService _logs;

    public AdminEscortImagesController(
        EscortImageService service,
        ActivityLogService logs)
    {
        _service = service;
        _logs = logs;
    }

    [HttpPatch("{id}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        try
        {
            // ✅ FIX: Check if User is null or claims are missing
            if (User == null || User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var adminIdClaim = User.FindFirst("adminId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (adminIdClaim == null)
            {
                return Unauthorized(new { message = "Admin ID not found in token" });
            }

            if (!Guid.TryParse(adminIdClaim.Value, out var adminId))
            {
                return BadRequest(new { message = "Invalid admin ID format" });
            }

            // Call the service
            await _service.ApproveImageAsync(id, adminId);
            
            // ✅ Log the activity with correct parameters
            if (_logs != null)
            {
                await _logs.LogAsync(
                    adminId, 
                    "APPROVE", 
                    "escort_image",  // entity type
                    id,              // entity id
                    "Image approved" // description
                );
            }

            return Ok(new { message = "Image approved successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Approve: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            return StatusCode(500, new { 
                message = "Failed to approve image", 
                error = ex.Message 
            });
        }
    }

    [HttpPatch("{id}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectImageDto dto)
    {
        try
        {
            // ✅ FIX: Check if User is null or claims are missing
            if (User == null || User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.Reason))
            {
                return BadRequest(new { message = "Rejection reason is required" });
            }

            var adminIdClaim = User.FindFirst("adminId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (adminIdClaim == null)
            {
                return Unauthorized(new { message = "Admin ID not found in token" });
            }

            if (!Guid.TryParse(adminIdClaim.Value, out var adminId))
            {
                return BadRequest(new { message = "Invalid admin ID format" });
            }

            // Call the service
            await _service.RejectImageAsync(id, adminId, dto.Reason);
            
            // ✅ Log the activity with correct parameters
            if (_logs != null)
            {
                await _logs.LogAsync(
                    adminId, 
                    "REJECT", 
                    "escort_image",  // entity type
                    id,              // entity id
                    dto.Reason       // description (the rejection reason)
                );
            }

            return Ok(new { message = "Image rejected successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Reject: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            return StatusCode(500, new { 
                message = "Failed to reject image", 
                error = ex.Message 
            });
        }
    }

    // ✅ ADD THIS DELETE ENDPOINT
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            // Check if user is authenticated
            if (User == null || User.Identity == null || !User.Identity.IsAuthenticated)
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var adminIdClaim = User.FindFirst("adminId") ?? User.FindFirst(ClaimTypes.NameIdentifier);
            
            if (adminIdClaim == null)
            {
                return Unauthorized(new { message = "Admin ID not found in token" });
            }

            if (!Guid.TryParse(adminIdClaim.Value, out var adminId))
            {
                return BadRequest(new { message = "Invalid admin ID format" });
            }

            // Call the service to delete the image
            await _service.DeleteImageAsync(id, adminId);
            
            // Log the activity
            if (_logs != null)
            {
                await _logs.LogAsync(
                    adminId, 
                    "DELETE", 
                    "escort_image", 
                    id, 
                    "Image deleted"
                );
            }

            return Ok(new { message = "Image deleted successfully" });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in Delete: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            return StatusCode(500, new { 
                message = "Failed to delete image", 
                error = ex.Message 
            });
        }
    }
}