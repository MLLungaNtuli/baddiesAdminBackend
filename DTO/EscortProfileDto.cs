public class EscortProfileDto
{
    public Guid Id { get; set; }
    public string StageName { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; }
    public string Bio { get; set; }
    public string Location { get; set; }
    public decimal PricePerHour { get; set; }
    public bool Available { get; set; }
    public bool Active { get; set; }
    public bool Verified { get; set; }
    public string? ProfileImage { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Profile details
    public string Nationality { get; set; }
    public string Height { get; set; }
    public string Weight { get; set; }
    public string Bust { get; set; }
    public string Waist { get; set; }
    public string Hips { get; set; }
    public string HairColor { get; set; }
    public string EyeColor { get; set; }
    public string Ethnicity { get; set; }
    public string[] Languages { get; set; }
    public string[] Services { get; set; }
    public string Measurements { get; set; }
    public string BodyType { get; set; }
    public bool Smoking { get; set; }
    public bool Drinking { get; set; }
    public bool Tattoos { get; set; }
    public bool Piercings { get; set; }
    public string AvailabilityTimes { get; set; }
    public decimal? IncallRate { get; set; }
    public decimal? OutcallRate { get; set; }
    public int? TravelRadius { get; set; }
    public decimal? TravelFee { get; set; }
    public string VerificationStatus { get; set; }
    public bool Featured { get; set; }
    public decimal Rating { get; set; }
    
    // Images
    public List<EscortImageDto> Images { get; set; }
}

public class EscortImageDto
{
    public Guid Id { get; set; }
    public string ImageUrl { get; set; }
    public bool Approved { get; set; }
    public bool IsProfile { get; set; }
}

// public class EscortStats
// {
//     public int Total { get; set; }
//     public int Active { get; set; }
//     public int Verified { get; set; }
// }