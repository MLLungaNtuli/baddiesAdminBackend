// Infrastructure/SupabaseService.cs
using Supabase;

public class SupabaseService
{
    private readonly Supabase.Client _client;
    private readonly ILogger<SupabaseService> _logger;

    public SupabaseService(IConfiguration config, ILogger<SupabaseService> logger)
    {
        var url = config["Supabase:Url"] ?? throw new Exception("Supabase URL is missing");
        var key = config["Supabase:ServiceRoleKey"] ?? throw new Exception("Supabase key is missing");
        
        _client = new Supabase.Client(url, key);
        _logger = logger;
    }

    public async Task InitializeAsync()
    {
        await _client.InitializeAsync();
        _logger.LogInformation("Supabase client initialized");
    }

    public Supabase.Client GetClient() => _client;

    // Upload image to Supabase Storage
    public async Task<string> UploadImageAsync(string bucket, string fileName, Stream fileStream, string contentType)
    {
        var storage = _client.Storage;
        var bucketInstance = storage.From(bucket);
        
        // Convert stream to byte array
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();
        
        var result = await bucketInstance.Upload(bytes, fileName, new Supabase.Storage.FileOptions
        {
            ContentType = contentType,
            CacheControl = "max-age=31536000"
        });
        
        if (result == null)
            throw new Exception("Failed to upload image");
        
        // Get public URL
        var publicUrl = bucketInstance.GetPublicUrl(fileName);
        return publicUrl;
    }

    // Delete image from Supabase Storage
    public async Task DeleteImageAsync(string bucket, string fileName)
    {
        var storage = _client.Storage;
        var bucketInstance = storage.From(bucket);
        
        var files = new List<string> { fileName };
        await bucketInstance.Remove(files);
    }
}