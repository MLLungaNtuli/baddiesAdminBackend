using System.Text.Json.Serialization;

public class EscortImage
{
    public Guid Id { get; set; }

    [JsonPropertyName("escort_id")]
    public Guid Escort_Id { get; set; }

    [JsonPropertyName("image_url")]
    public string Image_Url { get; set; }

    public bool? Approved { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? Created_At { get; set; }

    [JsonPropertyName("is_profile")]
    public bool? Is_Profile { get; set; }
}