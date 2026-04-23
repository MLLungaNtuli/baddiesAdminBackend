using System.Text.Json.Serialization;
public class Escort
{
    public Guid Id { get; set; }
    
    [JsonPropertyName("user_id")]
    public Guid? User_Id { get; set; }
    
    [JsonPropertyName("stage_name")]
    public string Stage_Name { get; set; }
    
    public int Age { get; set; }
    
    [JsonPropertyName("phone_number")]
    public string Phone_Number { get; set; }
    
    public string Bio { get; set; }
    public string Location { get; set; }
    
    [JsonPropertyName("price_per_hour")]
    public decimal Price_Per_Hour { get; set; }
    
    public bool Available { get; set; }
    public bool Active { get; set; }
    public bool Verified { get; set; }
    
    [JsonPropertyName("profile_image")]
    public string Profile_Image { get; set; }
    
    [JsonPropertyName("created_at")]
    public DateTime Created_At { get; set; }
    
    [JsonPropertyName("deleted_at")]
    public DateTime? Deleted_At { get; set; }
    
    [JsonPropertyName("deleted_by")]
    public string Deleted_By { get; set; }
    
    [JsonPropertyName("delete_reason")]
    public string Delete_Reason { get; set; }
    
    // Profile fields
    public string Nationality { get; set; }
    public string Height { get; set; }
    public string Weight { get; set; }
    public string Bust { get; set; }
    public string Waist { get; set; }
    public string Hips { get; set; }
    
    [JsonPropertyName("hair_color")]
    public string Hair_Color { get; set; }
    
    [JsonPropertyName("eye_color")]
    public string Eye_Color { get; set; }
    
    public string Ethnicity { get; set; }
    public string[] Languages { get; set; }
    public string[] Services { get; set; }
    public string Measurements { get; set; }
    
    [JsonPropertyName("body_type")]
    public string Body_Type { get; set; }
    
    public bool Smoking { get; set; }
    public bool Drinking { get; set; }
    public bool Tattoos { get; set; }
    public bool Piercings { get; set; }
    
    [JsonPropertyName("availability_times")]
    public string Availability_Times { get; set; }
    
    [JsonPropertyName("incall_rate")]
    public decimal? Incall_Rate { get; set; }
    
    [JsonPropertyName("outcall_rate")]
    public decimal? Outcall_Rate { get; set; }
    
    [JsonPropertyName("travel_radius")]
    public int? Travel_Radius { get; set; }
    
    [JsonPropertyName("travel_fee")]
    public decimal? Travel_Fee { get; set; }
    
    [JsonPropertyName("verification_status")]
    public string Verification_Status { get; set; }
    
    public bool Featured { get; set; }
    public decimal Rating { get; set; }
}