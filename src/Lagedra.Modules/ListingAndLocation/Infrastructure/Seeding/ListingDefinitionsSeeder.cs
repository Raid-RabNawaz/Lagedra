using Lagedra.Modules.ListingAndLocation.Domain.Entities;
using Lagedra.Modules.ListingAndLocation.Domain.Enums;
using Lagedra.Modules.ListingAndLocation.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Lagedra.Modules.ListingAndLocation.Infrastructure.Seeding;

public static class ListingDefinitionsSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ListingsDbContext>();

        await SeedAmenitiesAsync(dbContext).ConfigureAwait(false);
        await SeedSafetyDevicesAsync(dbContext).ConfigureAwait(false);
        await SeedConsiderationsAsync(dbContext).ConfigureAwait(false);
    }

    private static async Task SeedAmenitiesAsync(ListingsDbContext dbContext)
    {
        if (await dbContext.AmenityDefinitions.AnyAsync().ConfigureAwait(false))
        {
            return;
        }

        var amenities = new (string Name, AmenityCategory Category, string IconKey, int SortOrder)[]
        {
            // Kitchen (1–15)
            ("Kitchen", AmenityCategory.Kitchen, "cooking-pot", 1),
            ("Refrigerator", AmenityCategory.Kitchen, "refrigerator", 2),
            ("Microwave", AmenityCategory.Kitchen, "microwave", 3),
            ("Oven", AmenityCategory.Kitchen, "flame-kindling", 4),
            ("Stove", AmenityCategory.Kitchen, "flame", 5),
            ("Dishwasher", AmenityCategory.Kitchen, "glasses", 6),
            ("Coffee Maker", AmenityCategory.Kitchen, "coffee", 7),
            ("Espresso Machine", AmenityCategory.Kitchen, "cup-soda", 8),
            ("Toaster", AmenityCategory.Kitchen, "sandwich", 9),
            ("Blender", AmenityCategory.Kitchen, "blend", 10),
            ("Kettle", AmenityCategory.Kitchen, "coffee", 11),
            ("Cooking Basics", AmenityCategory.Kitchen, "utensils", 12),
            ("Dishes & Silverware", AmenityCategory.Kitchen, "utensils-crossed", 13),
            ("Freezer", AmenityCategory.Kitchen, "snowflake", 14),
            ("Trash Compactor", AmenityCategory.Kitchen, "trash-2", 15),

            // Bathroom (16–25)
            ("Private Bathroom", AmenityCategory.Bathroom, "bath", 16),
            ("Bathtub", AmenityCategory.Bathroom, "bath", 17),
            ("Walk-In Shower", AmenityCategory.Bathroom, "shower-head", 18),
            ("Hair Dryer", AmenityCategory.Bathroom, "wind", 19),
            ("Shampoo", AmenityCategory.Bathroom, "droplets", 20),
            ("Conditioner", AmenityCategory.Bathroom, "droplets", 21),
            ("Body Wash", AmenityCategory.Bathroom, "sparkles", 22),
            ("Hot Water", AmenityCategory.Bathroom, "thermometer", 23),
            ("Bidet", AmenityCategory.Bathroom, "droplet", 24),
            ("Towels Provided", AmenityCategory.Bathroom, "shirt", 25),

            // Bedroom (26–35)
            ("King Bed", AmenityCategory.Bedroom, "bed-double", 26),
            ("Queen Bed", AmenityCategory.Bedroom, "bed-double", 27),
            ("Single Bed", AmenityCategory.Bedroom, "bed-single", 28),
            ("Sofa Bed", AmenityCategory.Bedroom, "sofa", 29),
            ("Bed Linens", AmenityCategory.Bedroom, "bed-double", 30),
            ("Extra Pillows & Blankets", AmenityCategory.Bedroom, "layers", 31),
            ("Blackout Curtains", AmenityCategory.Bedroom, "blinds", 32),
            ("Walk-In Closet", AmenityCategory.Bedroom, "door-closed", 33),
            ("Hangers", AmenityCategory.Bedroom, "grip-horizontal", 34),
            ("Dresser", AmenityCategory.Bedroom, "archive", 35),

            // Living Area (36–45)
            ("TV", AmenityCategory.LivingArea, "tv", 36),
            ("Smart TV", AmenityCategory.LivingArea, "monitor-smartphone", 37),
            ("Streaming Services", AmenityCategory.LivingArea, "play", 38),
            ("Cable TV", AmenityCategory.LivingArea, "cable", 39),
            ("Fireplace", AmenityCategory.LivingArea, "flame", 40),
            ("Sofa", AmenityCategory.LivingArea, "sofa", 41),
            ("Dining Table", AmenityCategory.LivingArea, "table", 42),
            ("Bookshelf", AmenityCategory.LivingArea, "book-open", 43),
            ("Sound System", AmenityCategory.LivingArea, "speaker", 44),
            ("Board Games", AmenityCategory.LivingArea, "dice-5", 45),

            // Outdoor (46–60)
            ("Balcony", AmenityCategory.Outdoor, "fence", 46),
            ("Patio", AmenityCategory.Outdoor, "trees", 47),
            ("Terrace", AmenityCategory.Outdoor, "sun", 48),
            ("Garden", AmenityCategory.Outdoor, "flower-2", 49),
            ("BBQ Grill", AmenityCategory.Outdoor, "beef", 50),
            ("Pool", AmenityCategory.Outdoor, "waves", 51),
            ("Hot Tub", AmenityCategory.Outdoor, "thermometer", 52),
            ("Outdoor Furniture", AmenityCategory.Outdoor, "armchair", 53),
            ("Outdoor Dining Area", AmenityCategory.Outdoor, "utensils", 54),
            ("Sun Loungers", AmenityCategory.Outdoor, "sun", 55),
            ("Hammock", AmenityCategory.Outdoor, "tent", 56),
            ("Fire Pit", AmenityCategory.Outdoor, "flame", 57),
            ("Outdoor Shower", AmenityCategory.Outdoor, "shower-head", 58),
            ("Private Backyard", AmenityCategory.Outdoor, "fence", 59),
            ("Rooftop Access", AmenityCategory.Outdoor, "building", 60),

            // Parking (61–66)
            ("Free Parking on Premises", AmenityCategory.Parking, "car", 61),
            ("Garage", AmenityCategory.Parking, "warehouse", 62),
            ("Street Parking", AmenityCategory.Parking, "square-parking", 63),
            ("Paid Parking on Premises", AmenityCategory.Parking, "ticket", 64),
            ("EV Charger", AmenityCategory.Parking, "plug-zap", 65),
            ("Covered Parking", AmenityCategory.Parking, "car", 66),

            // Entertainment (67–73)
            ("Game Console", AmenityCategory.Entertainment, "gamepad-2", 67),
            ("Pool Table", AmenityCategory.Entertainment, "circle-dot", 68),
            ("Ping Pong Table", AmenityCategory.Entertainment, "table-tennis", 69),
            ("Gym / Fitness Equipment", AmenityCategory.Entertainment, "dumbbell", 70),
            ("Yoga Mat", AmenityCategory.Entertainment, "stretch-horizontal", 71),
            ("Bicycle", AmenityCategory.Entertainment, "bike", 72),
            ("Kayak / Canoe", AmenityCategory.Entertainment, "sailboat", 73),

            // Workspace (74–79)
            ("Dedicated Workspace", AmenityCategory.WorkSpace, "laptop", 74),
            ("Desk", AmenityCategory.WorkSpace, "monitor", 75),
            ("Office Chair", AmenityCategory.WorkSpace, "armchair", 76),
            ("External Monitor", AmenityCategory.WorkSpace, "monitor", 77),
            ("Printer", AmenityCategory.WorkSpace, "printer", 78),
            ("High-Speed Internet", AmenityCategory.WorkSpace, "wifi", 79),

            // Accessibility (80–85)
            ("Wheelchair Accessible", AmenityCategory.Accessibility, "accessibility", 80),
            ("Step-Free Entry", AmenityCategory.Accessibility, "door-open", 81),
            ("Wide Doorways", AmenityCategory.Accessibility, "move", 82),
            ("Elevator", AmenityCategory.Accessibility, "arrow-up-down", 83),
            ("Grab Bars", AmenityCategory.Accessibility, "grip-horizontal", 84),
            ("Accessible Bathroom", AmenityCategory.Accessibility, "accessibility", 85),

            // Laundry (86–91)
            ("In-Unit Washer", AmenityCategory.Laundry, "washing-machine", 86),
            ("In-Unit Dryer", AmenityCategory.Laundry, "wind", 87),
            ("Shared Laundry", AmenityCategory.Laundry, "building-2", 88),
            ("Iron", AmenityCategory.Laundry, "shirt", 89),
            ("Ironing Board", AmenityCategory.Laundry, "rectangle-horizontal", 90),
            ("Clothes Drying Rack", AmenityCategory.Laundry, "grip-horizontal", 91),

            // Climate Control (92–99)
            ("Central Air Conditioning", AmenityCategory.ClimateControl, "snowflake", 92),
            ("Portable Air Conditioner", AmenityCategory.ClimateControl, "snowflake", 93),
            ("Central Heating", AmenityCategory.ClimateControl, "thermometer-sun", 94),
            ("Radiant Heating", AmenityCategory.ClimateControl, "sun", 95),
            ("Ceiling Fans", AmenityCategory.ClimateControl, "fan", 96),
            ("Portable Fan", AmenityCategory.ClimateControl, "fan", 97),
            ("Portable Heater", AmenityCategory.ClimateControl, "flame", 98),
            ("Heated Floors", AmenityCategory.ClimateControl, "thermometer", 99),

            // Internet (100–103)
            ("WiFi", AmenityCategory.Internet, "wifi", 100),
            ("High-Speed WiFi (100+ Mbps)", AmenityCategory.Internet, "zap", 101),
            ("Ethernet Connection", AmenityCategory.Internet, "cable", 102),
            ("Fiber Optic Internet", AmenityCategory.Internet, "zap", 103),
        };

        foreach (var (name, category, iconKey, sortOrder) in amenities)
        {
            dbContext.AmenityDefinitions.Add(AmenityDefinition.Create(name, category, iconKey, sortOrder));
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private static async Task SeedSafetyDevicesAsync(ListingsDbContext dbContext)
    {
        if (await dbContext.SafetyDeviceDefinitions.AnyAsync().ConfigureAwait(false))
        {
            return;
        }

        var devices = new (string Name, string IconKey, int SortOrder)[]
        {
            ("Smoke Detector", "alarm-smoke", 1),
            ("Carbon Monoxide Detector", "cloud", 2),
            ("Fire Extinguisher", "flame", 3),
            ("First Aid Kit", "cross", 4),
            ("Security Camera (Exterior)", "camera", 5),
            ("Deadbolt Lock", "lock", 6),
            ("Smart Lock", "key-round", 7),
            ("Safe / Lockbox", "lock-keyhole", 8),
            ("Fire Escape Route", "door-open", 9),
            ("Emergency Exit Map", "map", 10),
            ("Window Locks", "lock", 11),
            ("Outdoor Lighting", "lamp", 12),
            ("Motion Sensor Lights", "scan-eye", 13),
            ("Fire Blanket", "shield", 14),
            ("Security Alarm System", "siren", 15),
        };

        foreach (var (name, iconKey, sortOrder) in devices)
        {
            dbContext.SafetyDeviceDefinitions.Add(SafetyDeviceDefinition.Create(name, iconKey, sortOrder));
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private static async Task SeedConsiderationsAsync(ListingsDbContext dbContext)
    {
        if (await dbContext.ConsiderationDefinitions.AnyAsync().ConfigureAwait(false))
        {
            return;
        }

        var considerations = new (string Name, string IconKey, int SortOrder)[]
        {
            ("Stairs / Multi-Level", "footprints", 1),
            ("Shared Spaces", "users", 2),
            ("Road Noise", "volume-2", 3),
            ("Near Busy Road", "car", 4),
            ("Construction Nearby", "hard-hat", 5),
            ("Pets on Property", "paw-print", 6),
            ("Unfenced Pool", "waves", 7),
            ("Limited Natural Light", "sun-dim", 8),
            ("No Elevator", "arrow-up-down", 9),
            ("Street Parking Only", "square-parking", 10),
            ("Security Cameras on Property", "camera", 11),
            ("Weapons on Property", "shield-alert", 12),
            ("Lake / River / Water Nearby", "droplets", 13),
            ("Animals on Property", "rabbit", 14),
            ("Hot Tub Without Fence", "thermometer", 15),
            ("Ceiling Height Under 8 ft", "ruler", 16),
            ("Shared Entrance", "door-open", 17),
            ("No Doorman / Concierge", "user-x", 18),
        };

        foreach (var (name, iconKey, sortOrder) in considerations)
        {
            dbContext.ConsiderationDefinitions.Add(PropertyConsiderationDefinition.Create(name, iconKey, sortOrder));
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
