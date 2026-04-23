using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase;

[ApiController]
[Route("api/admin/images")]
[Authorize]
public class ImageUploadController : ControllerBase
{
    private readonly Client _supabase;
    private readonly IConfiguration _config;
    private readonly EscortImageService _imageService;

    public ImageUploadController(
        Client supabase,
        IConfiguration config,
        EscortImageService imageService)
    {
        _supabase = supabase;
        _config = config;
        _imageService = imageService;
    }

    [HttpPost("escort/{escortId}")]
    [RequestSizeLimit(5_000_000)]
    public async Task<IActionResult> UploadEscortImage(
        Guid escortId,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded");

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequest("Invalid file type");

        var fileExt = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{fileExt}";
        var filePath = $"escorts/escort_{escortId}/{fileName}";

        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        await _supabase.Storage
            .From("escort-images")
            .Upload(stream.ToArray(), filePath, new Supabase.Storage.FileOptions
            {
                ContentType = file.ContentType,
                Upsert = false
            });

        var publicUrl =
            $"{_config["Supabase:Url"]}/storage/v1/object/public/escort-images/{filePath}";

        // ✅ SAVE IMAGE AS UNAPPROVED
        await _imageService.SaveAsync(escortId, publicUrl);

        return Ok(new
        {
            url = publicUrl,
            status = "pending_approval"
        });
    }
}
