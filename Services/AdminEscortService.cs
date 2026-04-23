using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class AdminEscortService
{
    private readonly DbConnectionFactory _db;

    public AdminEscortService(DbConnectionFactory db)
    {
        _db = db;
    }

    // Get escort by ID with profile details
    public async Task<Escort> GetByIdAsync(Guid id)
    {
        using var conn = _db.Create();
        return await conn.QuerySingleOrDefaultAsync<Escort>(
            "SELECT * FROM escorts WHERE id = @id AND deleted_at IS NULL",
            new { id }
        );
    }

    public async Task<IEnumerable<Escort>> GetAllAsync()
    {
        using var conn = _db.Create();
        return await conn.QueryAsync<Escort>(
            "SELECT * FROM escorts WHERE deleted_at IS NULL ORDER BY created_at DESC"
        );
    }

    // Get full escort profile with images
    public async Task<EscortProfileDto> GetProfileAsync(Guid id)
    {
        using var conn = _db.Create();

        // Get escort details
        var escort = await conn.QuerySingleOrDefaultAsync<Escort>(
            "SELECT * FROM escorts WHERE id = @id AND deleted_at IS NULL",
            new { id }
        );

        if (escort == null) return null;

        // Get images
        var images = await conn.QueryAsync<EscortImage>(
            "SELECT * FROM escort_images WHERE escort_id = @id",
            new { id }
        );

        return new EscortProfileDto
        {
            Id = escort.Id,
            StageName = escort.Stage_Name,
            Age = escort.Age,
            PhoneNumber = escort.Phone_Number,
            Bio = escort.Bio,
            Location = escort.Location,
            PricePerHour = escort.Price_Per_Hour,
            Available = escort.Available,
            Active = escort.Active,
            Verified = escort.Verified,
            ProfileImage = escort.Profile_Image,
            CreatedAt = escort.Created_At,
            // Profile details
            Nationality = escort.Nationality,
            Height = escort.Height,
            Weight = escort.Weight,
            Bust = escort.Bust,
            Waist = escort.Waist,
            Hips = escort.Hips,
            HairColor = escort.Hair_Color,
            EyeColor = escort.Eye_Color,
            Ethnicity = escort.Ethnicity,
            Languages = escort.Languages ?? new string[0],
            Services = escort.Services ?? new string[0],
            Measurements = escort.Measurements,
            BodyType = escort.Body_Type,
            Smoking = escort.Smoking,
            Drinking = escort.Drinking,
            Tattoos = escort.Tattoos,
            Piercings = escort.Piercings,
            AvailabilityTimes = escort.Availability_Times,
            IncallRate = escort.Incall_Rate,
            OutcallRate = escort.Outcall_Rate,
            TravelRadius = escort.Travel_Radius,
            TravelFee = escort.Travel_Fee,
            VerificationStatus = escort.Verification_Status,
            Featured = escort.Featured,
            Rating = escort.Rating,
            // Images
            Images = images.Select(i => new EscortImageDto
            {
                Id = i.Id,
                ImageUrl = i.Image_Url,
                Approved = i.Approved ?? false,
                IsProfile = i.Image_Url == escort.Profile_Image
            }).ToList()
        };
    }

   public async Task<Guid> CreateAsync(CreateEscortDto dto)
{
    using var conn = _db.Create();
    var id = Guid.NewGuid();
    
    var sql = @"
        INSERT INTO escorts (
            id, stage_name, age, phone_number, bio, location, price_per_hour, 
            available, active, verified, created_at,
            nationality, height, weight, bust, waist, hips, 
            hair_color, eye_color, ethnicity, languages, services,
            measurements, body_type, smoking, drinking, tattoos, 
            piercings, availability_times, incall_rate, outcall_rate,
            travel_radius, travel_fee
        ) VALUES (
            @Id, @StageName, @Age, @PhoneNumber, @Bio, @Location, @PricePerHour,
            @Available, @Active, @Verified, NOW(),
            @Nationality, @Height, @Weight, @Bust, @Waist, @Hips,
            @HairColor, @EyeColor, @Ethnicity, @Languages, @Services,
            @Measurements, @BodyType, @Smoking, @Drinking, @Tattoos,
            @Piercings, @AvailabilityTimes, @IncallRate, @OutcallRate,
            @TravelRadius, @TravelFee
        )";
    
    await conn.ExecuteAsync(sql, new
    {
        Id = id,
        StageName = dto.StageName,
        Age = dto.Age,
        PhoneNumber = dto.PhoneNumber,
        Bio = dto.Bio,
        Location = dto.Location,
        PricePerHour = dto.PricePerHour,
        Available = true,  // bool (not nullable)
        Active = true,      // bool (not nullable)
        Verified = false,   // bool (not nullable)
        Nationality = string.IsNullOrWhiteSpace(dto.Nationality) ? null : dto.Nationality,
        Height = string.IsNullOrWhiteSpace(dto.Height) ? null : dto.Height,
        Weight = string.IsNullOrWhiteSpace(dto.Weight) ? null : dto.Weight,
        Bust = string.IsNullOrWhiteSpace(dto.Bust) ? null : dto.Bust,
        Waist = string.IsNullOrWhiteSpace(dto.Waist) ? null : dto.Waist,
        Hips = string.IsNullOrWhiteSpace(dto.Hips) ? null : dto.Hips,
        HairColor = string.IsNullOrWhiteSpace(dto.HairColor) ? null : dto.HairColor,
        EyeColor = string.IsNullOrWhiteSpace(dto.EyeColor) ? null : dto.EyeColor,
        Ethnicity = string.IsNullOrWhiteSpace(dto.Ethnicity) ? null : dto.Ethnicity,
        Languages = dto.Languages ?? new string[0],
        Services = dto.Services ?? new string[0],
        Measurements = string.IsNullOrWhiteSpace(dto.Measurements) ? null : dto.Measurements,
        BodyType = string.IsNullOrWhiteSpace(dto.BodyType) ? null : dto.BodyType,
        Smoking = dto.Smoking,     // bool (from DTO, not nullable)
        Drinking = dto.Drinking,   // bool (from DTO, not nullable)
        Tattoos = dto.Tattoos,     // bool (from DTO, not nullable)
        Piercings = dto.Piercings, // bool (from DTO, not nullable)
        AvailabilityTimes = string.IsNullOrWhiteSpace(dto.AvailabilityTimes) ? null : dto.AvailabilityTimes,
        IncallRate = dto.IncallRate,
        OutcallRate = dto.OutcallRate,
        TravelRadius = dto.TravelRadius,
        TravelFee = dto.TravelFee
    });
    
    return id;
}

    // Update escort with all profile fields
    public async Task UpdateProfileAsync(Guid id, UpdateEscortDto dto)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(@"
            UPDATE escorts SET
                stage_name = @StageName,
                age = @Age,
                phone_number = @PhoneNumber,
                bio = @Bio,
                location = @Location,
                price_per_hour = @PricePerHour,
                available = @Available,
                nationality = @Nationality,
                height = @Height,
                weight = @Weight,
                bust = @Bust,
                waist = @Waist,
                hips = @Hips,
                hair_color = @HairColor,
                eye_color = @EyeColor,
                ethnicity = @Ethnicity,
                languages = @Languages,
                services = @Services,
                measurements = @Measurements,
                body_type = @BodyType,
                smoking = @Smoking,
                drinking = @Drinking,
                tattoos = @Tattoos,
                piercings = @Piercings,
                availability_times = @AvailabilityTimes,
                incall_rate = @IncallRate,
                outcall_rate = @OutcallRate,
                travel_radius = @TravelRadius,
                travel_fee = @TravelFee
            WHERE id = @id",
            new
            {
                id,
                dto.StageName,
                dto.Age,
                dto.PhoneNumber,
                dto.Bio,
                dto.Location,
                dto.PricePerHour,
                dto.Available,
                dto.Nationality,
                dto.Height,
                dto.Weight,
                dto.Bust,
                dto.Waist,
                dto.Hips,
                dto.HairColor,
                dto.EyeColor,
                dto.Ethnicity,
                Languages = dto.Languages ?? new string[0],
                Services = dto.Services ?? new string[0],
                dto.Measurements,
                dto.BodyType,
                dto.Smoking,
                dto.Drinking,
                dto.Tattoos,
                dto.Piercings,
                dto.AvailabilityTimes,
                dto.IncallRate,
                dto.OutcallRate,
                dto.TravelRadius,
                dto.TravelFee
            });
    }

    public async Task VerifyAsync(Guid id)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "UPDATE escorts SET verified = true WHERE id = @id",
            new { id }
        );
    }

    public async Task SetActiveAsync(Guid id, bool active)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "UPDATE escorts SET active = @active WHERE id = @id",
            new { id, active }
        );
    }

    public async Task SoftDeleteAsync(Guid escortId, string adminEmail, string reason)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(@"
            UPDATE escorts SET 
                deleted_at = NOW(), 
                deleted_by = @adminEmail, 
                delete_reason = @reason, 
                active = false 
            WHERE id = @escortId",
            new { escortId, adminEmail, reason }
        );
    }

    public async Task SetProfileImageAsync(Guid escortId, string imageUrl)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "UPDATE escorts SET profile_image = @url WHERE id = @escortId",
            new { escortId, url = imageUrl }
        );
    }

public async Task<EscortStats> GetStatsAsync()
{
    using var conn = _db.Create();
    return await conn.QuerySingleAsync<EscortStats>(@"
        SELECT 
            COUNT(*) AS Total,
            COUNT(CASE WHEN active = true THEN 1 END) AS Active,
            COUNT(CASE WHEN verified = true THEN 1 END) AS Verified
        FROM escorts 
        WHERE deleted_at IS NULL"
    );
}
    // Set featured status
    public async Task SetFeaturedAsync(Guid id, bool featured)
    {
        using var conn = _db.Create();
        await conn.ExecuteAsync(
            "UPDATE escorts SET featured = @featured WHERE id = @id",
            new { id, featured }
        );
    }
}