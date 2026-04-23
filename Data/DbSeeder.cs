using Dapper;
using Npgsql;

namespace Baddies.Admin.Data
{
    public static class DbSeeder
    {
// In DbSeeder.cs
public static async Task SeedAdmin(string connectionString, string username = "admin@baddies.com", string password = "Admin@1021998")
{
    try
    {
        Console.WriteLine($"Seeding admin user with username/email: {username}");
        
        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();
        
        // Check if admin already exists by email
        var existingAdmin = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT id FROM admins WHERE email = @email",
            new { email = username });
        
        if (existingAdmin != null)
        {
            Console.WriteLine($"✅ Admin user '{username}' already exists with ID: {existingAdmin.id}");
            
            // Update username to match email if different
            var currentAdmin = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT username FROM admins WHERE id = @id",
                new { id = existingAdmin.id });
                
            if (currentAdmin?.username != username)
            {
                Console.WriteLine($"Updating username from '{currentAdmin?.username}' to '{username}'");
                await conn.ExecuteAsync(
                    "UPDATE admins SET username = @username WHERE id = @id",
                    new { username, id = existingAdmin.id });
            }
            
            return;
        }
        
        // Hash the password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, 12);
        
        // Insert admin with username same as email
        var adminId = await conn.ExecuteScalarAsync<Guid>(
            @"INSERT INTO admins 
              (username, email, password_hash, full_name, role, created_at, updated_at)
              VALUES 
              (@username, @email, @passwordHash, @fullName, @role, NOW(), NOW())
              RETURNING id",
            new
            {
                username,      // Use the same value for both
                email = username,  // Use the same value for both
                passwordHash,
                fullName = "System Administrator",
                role = "admin"
            });
        
        Console.WriteLine($"✅ Admin user '{username}' created successfully with ID: {adminId}");
        Console.WriteLine($"📋 Admin credentials:");
        Console.WriteLine($"   Username: {username}");
        Console.WriteLine($"   Email: {username}");
        Console.WriteLine($"   Password: {password}");
        
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error seeding admin: {ex.Message}");
        Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        throw;
    }
}
// DbSeeder.cs - Add these methods to your existing DbSeeder class
// In DbSeeder.cs - Fix the SQL to match your table structure
public static async Task SeedEscorts(string connectionString)
{
    try
    {
        Console.WriteLine("Starting escort seeding...");

        using var conn = new NpgsqlConnection(connectionString);
        await conn.OpenAsync();

        var escorts = new List<CreateEscortDto>
        {
            new CreateEscortDto
            {
                StageName = "Luna",
                Age = 25,
                PhoneNumber = "+1-555-1234",
                Bio = "Elegant and sophisticated companion with a passion for fine dining and cultural events.",
                Location = "Los Angeles, CA",
                PricePerHour = 450.00m,
                ProfileImage = "https://images.unsplash.com/photo-1494790108755-2616b786d4b3?w=400&h=400&fit=crop&crop=face",
                Nationality = "American",
                Height = "165",
                Weight = "55",
                Bust = "34",
                Waist = "24",
                Hips = "36",
                HairColor = "Blonde",
                EyeColor = "Blue",
                Ethnicity = "Caucasian",
                Languages = new[] { "English" },
                Services = new[] { "Dinner Date", "GFE (Girlfriend Experience)" },
                Measurements = "34-24-36",
                BodyType = "Athletic",
                Smoking = false,
                Drinking = false,
                Tattoos = false,
                Piercings = false,
                AvailabilityTimes = "Mon-Fri 6pm-12am, Weekends 24/7",
                IncallRate = 450.00m,
                OutcallRate = 500.00m,
                TravelRadius = 50,
                TravelFee = 100.00m,
                Active = true,
                Verified = true,
                VerificationStatus = "verified",
                Featured = false,
                Rating = 4.8m
            },
            new CreateEscortDto
            {
                StageName = "Scarlett",
                Age = 28,
                PhoneNumber = "+1-555-5678",
                Bio = "Adventurous and energetic partner for exploring the city and nightlife.",
                Location = "Miami, FL",
                PricePerHour = 500.00m,
                ProfileImage = "https://images.unsplash.com/photo-1438761681033-6461ffad8d80?w=400&h=400&fit=crop&crop=face",
                Nationality = "American",
                Height = "170",
                Weight = "58",
                Bust = "34",
                Waist = "25",
                Hips = "36",
                HairColor = "Brown",
                EyeColor = "Green",
                Ethnicity = "Caucasian",
                Languages = new[] { "English", "Spanish" },
                Services = new[] { "Dinner Date", "GFE (Girlfriend Experience)" },
                Measurements = "34-25-36",
                BodyType = "Slim",
                Smoking = false,
                Drinking = true,
                Tattoos = false,
                Piercings = true,
                AvailabilityTimes = "Mon-Fri 6pm-12am, Weekends 24/7",
                IncallRate = 500.00m,
                OutcallRate = 550.00m,
                TravelRadius = 60,
                TravelFee = 120.00m,
                Active = true,
                Verified = true,
                VerificationStatus = "verified",
                Featured = true,
                Rating = 4.9m
            },
            new CreateEscortDto
            {
                StageName = "Isabella",
                Age = 23,
                PhoneNumber = "+1-555-9012",
                Bio = "Classic beauty with a gentle personality, perfect for formal events and dinners.",
                Location = "New York, NY",
                PricePerHour = 600.00m,
                ProfileImage = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?w=400&h=400&fit=crop&crop=face",
                Nationality = "American",
                Height = "168",
                Weight = "54",
                Bust = "33",
                Waist = "24",
                Hips = "35",
                HairColor = "Black",
                EyeColor = "Brown",
                Ethnicity = "Caucasian",
                Languages = new[] { "English", "French" },
                Services = new[] { "Dinner Date", "GFE (Girlfriend Experience)" },
                Measurements = "33-24-35",
                BodyType = "Slim",
                Smoking = false,
                Drinking = false,
                Tattoos = false,
                Piercings = false,
                AvailabilityTimes = "Mon-Fri 6pm-12am, Weekends 24/7",
                IncallRate = 600.00m,
                OutcallRate = 650.00m,
                TravelRadius = 70,
                TravelFee = 150.00m,
                Active = true,
                Verified = true,
                VerificationStatus = "verified",
                Featured = false,
                Rating = 4.7m
            },
            new CreateEscortDto
            {
                StageName = "Sofia",
                Age = 30,
                PhoneNumber = "+1-555-3456",
                Bio = "Intelligent conversationalist with international experience and multilingual skills.",
                Location = "Chicago, IL",
                PricePerHour = 550.00m,
                ProfileImage = "https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d?w=400&h=400&fit=crop&crop=face",
                Nationality = "American",
                Height = "167",
                Weight = "57",
                Bust = "34",
                Waist = "25",
                Hips = "36",
                HairColor = "Brown",
                EyeColor = "Blue",
                Ethnicity = "Caucasian",
                Languages = new[] { "English", "German" },
                Services = new[] { "Dinner Date", "GFE (Girlfriend Experience)" },
                Measurements = "34-25-36",
                BodyType = "Athletic",
                Smoking = false,
                Drinking = false,
                Tattoos = false,
                Piercings = true,
                AvailabilityTimes = "Mon-Fri 6pm-12am, Weekends 24/7",
                IncallRate = 550.00m,
                OutcallRate = 600.00m,
                TravelRadius = 55,
                TravelFee = 130.00m,
                Active = true,
                Verified = true,
                VerificationStatus = "verified",
                Featured = true,
                Rating = 4.9m
            },
            new CreateEscortDto
            {
                StageName = "Chloe",
                Age = 26,
                PhoneNumber = "+1-555-7890",
                Bio = "Athletic and health-conscious companion for outdoor activities and wellness retreats.",
                Location = "Las Vegas, NV",
                PricePerHour = 475.00m,
                ProfileImage = "https://images.unsplash.com/photo-1488426862026-3ee34a7d66df?w=400&h=400&fit=crop&crop=face",
                Nationality = "American",
                Height = "166",
                Weight = "56",
                Bust = "34",
                Waist = "24",
                Hips = "36",
                HairColor = "Blonde",
                EyeColor = "Green",
                Ethnicity = "Caucasian",
                Languages = new[] { "English" },
                Services = new[] { "Dinner Date", "GFE (Girlfriend Experience)" },
                Measurements = "34-24-36",
                BodyType = "Athletic",
                Smoking = false,
                Drinking = true,
                Tattoos = false,
                Piercings = false,
                AvailabilityTimes = "Mon-Fri 6pm-12am, Weekends 24/7",
                IncallRate = 475.00m,
                OutcallRate = 500.00m,
                TravelRadius = 50,
                TravelFee = 110.00m,
                Active = true,
                Verified = true,
                VerificationStatus = "verified",
                Featured = false,
                Rating = 4.6m
            }
        };

        var galleryImages = new List<string>
        {
            "https://images.unsplash.com/photo-1517841905240-472988babdf9?w=600&h=400&fit=crop",
            "https://images.unsplash.com/photo-1524504388940-b1c1722653e1?w=600&h=400&fit=crop",
            "https://images.unsplash.com/photo-1488426862026-3ee34a7d66df?w=600&h=400&fit=crop",
            "https://images.unsplash.com/photo-1494790108755-2616b786d4b3?w=600&h=400&fit=crop",
            "https://images.unsplash.com/photo-1534528741775-53994a69daeb?w=600&h=400&fit=crop",
            "https://images.unsplash.com/photo-1529626455594-4ff0802cfb7e?w=600&h=400&fit=crop"
        };

        var random = new Random();

        foreach (var escort in escorts)
        {
            // Check if escort exists
            var existingEscortId = await conn.ExecuteScalarAsync<Guid?>(
                "SELECT id FROM escorts WHERE phone_number = @PhoneNumber OR stage_name = @StageName",
                new { escort.PhoneNumber, escort.StageName });

            if (existingEscortId != null)
            {
                // Update only null or empty fields
                var updateSql = @"
UPDATE escorts
SET
    bio = COALESCE(NULLIF(bio, ''), @Bio),
    location = COALESCE(NULLIF(location, ''), @Location),
    price_per_hour = COALESCE(price_per_hour, @PricePerHour),
    profile_image = COALESCE(profile_image, @ProfileImage),
    nationality = COALESCE(nationality, @Nationality),
    height = COALESCE(height, @Height),
    weight = COALESCE(weight, @Weight),
    bust = COALESCE(bust, @Bust),
    waist = COALESCE(waist, @Waist),
    hips = COALESCE(hips, @Hips),
    hair_color = COALESCE(hair_color, @HairColor),
    eye_color = COALESCE(eye_color, @EyeColor),
    ethnicity = COALESCE(ethnicity, @Ethnicity),
    languages = COALESCE(languages, @Languages),
    services = COALESCE(services, @Services),
    measurements = COALESCE(measurements, @Measurements),
    body_type = COALESCE(body_type, @BodyType),
    smoking = COALESCE(smoking, @Smoking),
    drinking = COALESCE(drinking, @Drinking),
    tattoos = COALESCE(tattoos, @Tattoos),
    piercings = COALESCE(piercings, @Piercings),
    availability_times = COALESCE(availability_times, @AvailabilityTimes),
    incall_rate = COALESCE(incall_rate, @IncallRate),
    outcall_rate = COALESCE(outcall_rate, @OutcallRate),
    travel_radius = COALESCE(travel_radius, @TravelRadius),
    travel_fee = COALESCE(travel_fee, @TravelFee),
    active = COALESCE(active, @Active),
    verified = COALESCE(verified, @Verified),
    verification_status = COALESCE(verification_status, @VerificationStatus),
    featured = COALESCE(featured, @Featured),
    rating = COALESCE(rating, @Rating)
WHERE id = @Id";
                
                await conn.ExecuteAsync(updateSql, new
                {
                    Id = existingEscortId,
                    escort.Bio,
                    escort.Location,
                    escort.PricePerHour,
                    escort.ProfileImage,
                    escort.Nationality,
                    escort.Height,
                    escort.Weight,
                    escort.Bust,
                    escort.Waist,
                    escort.Hips,
                    escort.HairColor,
                    escort.EyeColor,
                    escort.Ethnicity,
                    Languages = escort.Languages,
                    Services = escort.Services,
                    escort.Measurements,
                    escort.BodyType,
                    escort.Smoking,
                    escort.Drinking,
                    escort.Tattoos,
                    escort.Piercings,
                    escort.AvailabilityTimes,
                    escort.IncallRate,
                    escort.OutcallRate,
                    escort.TravelRadius,
                    escort.TravelFee,
                    escort.Active,
                    escort.Verified,
                    escort.VerificationStatus,
                    escort.Featured,
                    escort.Rating
                });

                Console.WriteLine($"↻ Updated missing fields for escort: {escort.StageName}");
                continue;
            }

            // Insert new escort
            var escortId = Guid.NewGuid();
            var insertSql = @"
INSERT INTO escorts 
(id, stage_name, age, phone_number, bio, location, price_per_hour,
 available, active, verified, profile_image, created_at,
 nationality, height, weight, bust, waist, hips,
 hair_color, eye_color, ethnicity, languages, services,
 measurements, body_type, smoking, drinking, tattoos, piercings,
 availability_times, incall_rate, outcall_rate, travel_radius, travel_fee,
 verification_status, featured, rating)
VALUES 
(@Id, @StageName, @Age, @PhoneNumber, @Bio, @Location, @PricePerHour,
 @Available, @Active, @Verified, @ProfileImage, NOW(),
 @Nationality, @Height, @Weight, @Bust, @Waist, @Hips,
 @HairColor, @EyeColor, @Ethnicity, @Languages, @Services,
 @Measurements, @BodyType, @Smoking, @Drinking, @Tattoos, @Piercings,
 @AvailabilityTimes, @IncallRate, @OutcallRate, @TravelRadius, @TravelFee,
 @VerificationStatus, @Featured, @Rating)";

            await conn.ExecuteAsync(insertSql, new
            {
                Id = escortId,
                escort.StageName,
                escort.Age,
                escort.PhoneNumber,
                escort.Bio,
                escort.Location,
                escort.PricePerHour,
                escort.Available,
                escort.Active,
                escort.Verified,
                escort.ProfileImage,
                escort.Nationality,
                escort.Height,
                escort.Weight,
                escort.Bust,
                escort.Waist,
                escort.Hips,
                escort.HairColor,
                escort.EyeColor,
                escort.Ethnicity,
                Languages = escort.Languages,
                Services = escort.Services,
                escort.Measurements,
                escort.BodyType,
                escort.Smoking,
                escort.Drinking,
                escort.Tattoos,
                escort.Piercings,
                escort.AvailabilityTimes,
                escort.IncallRate,
                escort.OutcallRate,
                escort.TravelRadius,
                escort.TravelFee,
                escort.VerificationStatus,
                escort.Featured,
                escort.Rating
            });

            Console.WriteLine($"✓ Created escort: {escort.StageName}");

            // Add 2-3 gallery images
            int imageCount = random.Next(2, 4);
            for (int j = 0; j < imageCount; j++)
            {
                var galleryImage = galleryImages[(random.Next(0, galleryImages.Count))];

                // Skip if image already exists
                var exists = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM escort_images WHERE escort_id = @EscortId AND image_url = @ImageUrl",
                    new { EscortId = escortId, ImageUrl = galleryImage });

                if (exists == 0)
                {
                    var imageSql = @"
INSERT INTO escort_images (id, escort_id, image_url, approved, created_at, approved_at)
VALUES (@Id, @EscortId, @ImageUrl, true, NOW(), NOW())";
                    await conn.ExecuteAsync(imageSql, new
                    {
                        Id = Guid.NewGuid(),
                        EscortId = escortId,
                        ImageUrl = galleryImage
                    });
                }
            }
        }

        Console.WriteLine("✅ Escort seeding completed!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error seeding escorts: {ex.Message}");
        Console.WriteLine(ex.StackTrace);
        throw;
    }
}

public static async Task SeedLookupData(string connectionString)
{
    using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    var bodyTypes = new[] { "Slim", "Athletic", "Average", "Curvy", "Voluptuous", "BBW", "Muscular" };
    var hairColors = new[] { "Blonde", "Brunette", "Black", "Red", "Auburn", "Brown", "Blonde Highlights", "Brunette Highlights", "Silver/Grey", "Platinum", "Other" };
    var eyeColors = new[] { "Blue", "Green", "Brown", "Hazel", "Grey", "Amber", "Other" };
    var ethnicities = new[] { "African", "Asian", "Caucasian", "Hispanic/Latina", "Middle Eastern", "Mixed Race", "Native American", "Pacific Islander", "Other" };
    var languages = new[] { "English", "Spanish", "French", "German", "Italian", "Portuguese", "Russian", "Chinese", "Japanese", "Korean", "Arabic", "Hindi", "Other" };
    var services = new[] { "Dinner Date", "Social Events", "Travel Companion", "GFE (Girlfriend Experience)", "PSE (Pornstar Experience)", "Role Play", "Sensual Massage", "BDSM", "Fetish Friendly", "Dominatrix", "Submissive", "Switch", "Couples", "Photo Shoots", "Strip Tease", "Lap Dance", "Private Shows" };

    async Task SeedArray(string table, string[] items)
    {
        foreach (var item in items)
        {
            await conn.ExecuteAsync($@"
                INSERT INTO {table} (name)
                VALUES (@name)
                ON CONFLICT (name) DO NOTHING;
            ", new { name = item });
        }
    }

    await SeedArray("body_types", bodyTypes);
    await SeedArray("hair_colors", hairColors);
    await SeedArray("eye_colors", eyeColors);
    await SeedArray("ethnicities", ethnicities);
    await SeedArray("languages", languages);
    await SeedArray("services", services);

    Console.WriteLine("✅ Lookup tables seeded successfully!");
}

    }
}
