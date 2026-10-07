using Pikwise.Domain.Entities;

namespace Pikwise.UnitTests;

// The 25 laptops imported in Session 11.5, as normalized by the importer (Open Icecat specifications,
// development/test data only; ADR-020). Prices, stock and IsActive come from dev-prices.csv and are
// NOT market data. Ids follow the manifest import order. Used to check the hand-verified ranking in
// RECOMMENDATION_ENGINE.md.
internal static class RecommendationTestData
{
    public static IReadOnlyList<Product> Session115Laptops() =>
    [
        Laptop(1, "Lenovo", "Lenovo ThinkPad T14 Gen 7 (AMD) Copilot+ PC", 62000m, 8, true, "AMD Ryzen AI 7 PRO 450", "AMD Radeon 860M", 16, 512, 1.28m, 60),
        Laptop(2, "ASUS", "ASUS S1407AA-LY066W Copilot+ PC", 32000m, 12, true, "Intel Core Ultra 5 325", "Intel Graphics", 16, 512, 1.46m, 60),
        Laptop(3, "DELL", "DELL PW516265", 70000m, 5, true, "AMD Ryzen AI 7 PRO 450", "AMD Radeon 860M", 32, 1000, 1.89m, 60),
        Laptop(4, "HP", "HP EliteBook 6 G2a 14 Next Gen AI PC", 52000m, 7, true, "AMD Ryzen AI 5 PRO 435", "AMD Radeon 840M", 24, 512, 1.4m, 60),
        Laptop(5, "MSI", "MSI Modern A16 LE J1M-011BEN", 27000m, 15, true, "AMD Ryzen 7 170", "AMD Radeon 680M", 16, 512, 1.49m, 60),
        Laptop(6, "Fujitsu", "Fujitsu UQ-L1", 45000m, 4, true, "Qualcomm Snapdragon X1-26-100", "Intel Arc Graphics", 16, 512, 0.886m, null),
        Laptop(7, "Samsung", "Samsung Galaxy Book4 Edge (15.6\", Core 8, 16GB, 256GB), a Copilot+ PC", 33000m, 9, true, "Qualcomm Snapdragon X1-26-100", null, 16, 256, 1.5m, null),
        Laptop(8, "Alienware", "Alienware DA15265", 58000m, 6, true, "AMD Ryzen 7 260", "NVIDIA GeForce RTX 5050", 16, 1000, 2.25m, 165),
        Laptop(9, "Dynabook", "Dynabook VZ/HY", 48000m, 3, true, "Intel Core Ultra 7 155U", "Intel Graphics", 16, 512, 0.979m, null),
        Laptop(10, "GIGABYTE", "GIGABYTE EAGLE Gaming Laptop", 38000m, 10, true, "AMD Ryzen 5 7533HS", "NVIDIA GeForce RTX 4050", 16, 512, 2.2m, 165),
        Laptop(11, "Lenovo", "Lenovo ThinkPad T16 Gen 5 (Intel) Copilot+ PC", 64000m, 6, true, "Intel Core Ultra 7 355", "Intel Graphics", 16, 512, 1.63m, 60),
        Laptop(12, "ASUS", "ASUS GU405AP-SY019W", 115000m, 2, true, "Intel Core Ultra 9 386H", "NVIDIA GeForce RTX 5070 Laptop GPU", 32, 1000, 1.5m, 120),
        Laptop(13, "DELL", "DELL PW514265", 68000m, 5, true, "AMD Ryzen AI 7 PRO 450", "AMD Radeon 860M", 32, 1000, 1.4m, 60),
        Laptop(14, "HP", "HP ZBook 8 G2i 16", 82000m, 3, true, "Intel Core Ultra 7 366H", "Intel Graphics", 32, 512, 2.03m, 60),
        Laptop(15, "MSI", "MSI Modern A16 LE J1M-010NLN", 27500m, 8, false, "AMD Ryzen 7 170", "AMD Radeon 680M", 16, 512, 1.49m, 60),
        Laptop(16, "Fujitsu", "Fujitsu UH90/J3", 55000m, 4, true, "Intel Core Ultra 7 155H", "Intel Arc Graphics", 16, 512, 0.858m, null),
        Laptop(17, "Alienware", "Alienware AC16250", 85000m, 3, true, "Intel Core 9 270H", "NVIDIA GeForce RTX 5070", 16, 1000, 2.49m, 120),
        Laptop(18, "Dynabook", "Dynabook PZ/LY", 22000m, 0, true, "Intel Core i3-1305U", "Intel UHD Graphics", 8, 256, 1.77m, null),
        Laptop(19, "GIGABYTE", "GIGABYTE AERO X16 1VH93NEC94AH", 62000m, 5, true, "AMD Ryzen AI 7 350", "NVIDIA GeForce RTX 5060", 16, 1000, 1.9m, 165),
        Laptop(20, "Lenovo", "Lenovo ThinkPad T16g Gen 3", 165000m, 2, true, "Intel Core Ultra 7 255HX", "NVIDIA GeForce RTX 5080 Laptop GPU", 64, 1000, 2.54m, 60),
        Laptop(21, "ASUS", "ASUS GU405AP-SY016W", 102000m, 4, true, "Intel Core Ultra 9 386H", "NVIDIA GeForce RTX 5070 Laptop GPU", 16, 1000, 1.5m, 120),
        Laptop(22, "DELL", "DELL PC16250", 30000m, 11, true, "Intel Core 5 120U", "Intel Graphics", 16, 512, 1.92m, 60),
        Laptop(23, "HP", "HP EliteBook 6 G1i 14 AI PC", 50000m, 6, true, null, null, 16, 1000, 1.4m, 60),
        Laptop(24, "MSI", "MSI Modern A14 LE J1M-014BEN", 26000m, 14, true, "AMD Ryzen 7 170", "AMD Radeon 680M", 16, 512, 1.29m, 60),
        Laptop(25, "Fujitsu", "Fujitsu UH90/G2", 36000m, 7, true, "Intel Core i7-1255U", "Intel Iris Xe Graphics", 8, 512, 0.828m, null),
    ];

    private static Product Laptop(int id, string brand, string name, decimal price, int stock, bool isActive,
        string? processor, string? gpu, int ram, int storage, decimal weight, int? refreshRate) => new()
    {
        Id = id, Name = name, Price = price, Stock = stock, IsActive = isActive,
        Brand = new Brand { Id = id, Name = brand },
        LaptopSpecification = new LaptopSpecification
        {
            Processor = processor, GPU = gpu, RamGb = ram, StorageGb = storage, Weight = weight, RefreshRate = refreshRate
        }
    };
}
