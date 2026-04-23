public class UpdateEscortDto
{
    public string StageName { get; set; }
    public int Age { get; set; }
    public string PhoneNumber { get; set; }
    public string Bio { get; set; }
    public string Location { get; set; }
    public decimal PricePerHour { get; set; }
    public bool Available { get; set; }
    
    // Physical Attributes
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
    
    // Lifestyle
    public bool Smoking { get; set; }
    public bool Drinking { get; set; }
    public bool Tattoos { get; set; }
    public bool Piercings { get; set; }
    public string AvailabilityTimes { get; set; }
    
    // Rates
    public decimal? IncallRate { get; set; }
    public decimal? OutcallRate { get; set; }
    public int? TravelRadius { get; set; }
    public decimal? TravelFee { get; set; }
}