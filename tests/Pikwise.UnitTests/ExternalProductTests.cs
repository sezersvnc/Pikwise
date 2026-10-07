using System.Text.Json;
using Pikwise.Application.ExternalProducts.Interfaces;
using Pikwise.Application.ExternalProducts.Models;
using Pikwise.Application.ExternalProducts.Normalization;
using Pikwise.Application.ExternalProducts.Services;
using Pikwise.Domain.Entities;
using Pikwise.Infrastructure.ExternalProducts.Icecat;

namespace Pikwise.UnitTests;

public sealed class LaptopNormalizerTests
{
    private static ExternalLaptopRecord Record(Func<ExternalLaptopRecord, ExternalLaptopRecord>? change = null)
    {
        var record = new ExternalLaptopRecord("1", "  Lenovo ", " IdeaPad   5 ", " Intel  Core i5 ", null,
            16, 512, 1.8m, 60, 15.6m, "1920 x 1080 pixels", "Windows 11 Home");
        return change is null ? record : change(record);
    }

    [Fact]
    public void Text_is_trimmed_and_whitespace_collapsed_and_blank_becomes_null()
    {
        var result = LaptopNormalizer.Normalize(Record(r => r with { GpuName = "   " })).Record;
        Assert.Equal("Lenovo", result.BrandName);
        Assert.Equal("IdeaPad 5", result.ModelName);
        Assert.Equal("Intel Core i5", result.CpuName);
        Assert.Null(result.GpuName);
    }

    [Fact]
    public void Trademark_symbols_are_removed() =>
        Assert.Equal("Intel Core i9", LaptopNormalizer.CleanText("Intel® Core™ i9"));

    [Theory]
    [InlineData("1920 x 1080 pixels", "1920x1080")]
    [InlineData("2560×1600", "2560x1600")]
    [InlineData("Full HD", "Full HD")]
    public void Resolution_is_standardized_or_kept_as_supplied(string input, string expected) =>
        Assert.Equal(expected, LaptopNormalizer.Normalize(Record(r => r with { Resolution = input })).Record.Resolution);

    [Fact]
    public void Non_positive_numbers_are_dropped_and_reported()
    {
        var result = LaptopNormalizer.Normalize(Record(r => r with { RamGb = 0, WeightKg = -1m }));
        Assert.Null(result.Record.RamGb);
        Assert.Null(result.Record.WeightKg);
        Assert.Equal(2, result.Issues.Count);
    }

    [Fact]
    public void Too_long_text_is_dropped_not_truncated()
    {
        var result = LaptopNormalizer.Normalize(Record(r => r with { CpuName = new string('x', 201) }));
        Assert.Null(result.Record.CpuName);
        Assert.Single(result.Issues);
    }

    [Theory]
    [InlineData("Lenovo", "IdeaPad 5", "Lenovo IdeaPad 5")]
    [InlineData("Lenovo", "Lenovo IdeaPad 5", "Lenovo IdeaPad 5")]
    [InlineData(null, "IdeaPad 5", "IdeaPad 5")]
    public void Product_name_is_brand_and_model_without_repeating_the_brand(string? brand, string model, string expected) =>
        Assert.Equal(expected, LaptopNormalizer.BuildProductName(brand, model));
}

public sealed class DevelopmentPriceCsvParserTests
{
    [Fact]
    public void Valid_rows_are_parsed_and_comments_and_extra_columns_ignored()
    {
        var csv = "# dev data\nExternalId,Price,Stock,IsActive,Note\n101,999.90,5,true,x\n102,500,0,0,y\n";
        var result = DevelopmentPriceCsvParser.Parse(new StringReader(csv));
        Assert.Empty(result.Errors);
        Assert.Equal(999.90m, result.Entries["101"].Price);
        Assert.False(result.Entries["102"].IsActive);
        Assert.Equal(0, result.Entries["102"].Stock);
    }

    [Theory]
    [InlineData("101,,5,true")]        // blank price
    [InlineData("101,10.123,5,true")]  // too many decimals
    [InlineData("101,10,-1,true")]     // negative stock
    [InlineData("101,10,5,maybe")]     // bad flag
    public void Invalid_rows_are_reported_and_not_added(string row)
    {
        var result = DevelopmentPriceCsvParser.Parse(new StringReader("ExternalId,Price,Stock,IsActive\n" + row));
        Assert.Single(result.Errors);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public void Duplicate_ids_and_missing_header_columns_are_reported()
    {
        var duplicate = DevelopmentPriceCsvParser.Parse(new StringReader("ExternalId,Price,Stock,IsActive\n1,1,1,true\n1,2,2,true"));
        Assert.Single(duplicate.Errors);
        var header = DevelopmentPriceCsvParser.Parse(new StringReader("ExternalId,Price\n1,1"));
        Assert.Single(header.Errors);
    }
}

public sealed class IcecatLaptopMapperTests
{
    // Synthetic payload shaped like the documented Icecat response; real feature names are verified
    // later with the DataImport `inspect` command.
    // Feature names and value formats copied from a live Icecat notebook (inspect output).
    private const string Json = """
    {"msg":"OK","data":{
      "GeneralInfo":{"Brand":"ExampleBrand","ProductName":"Example Book 15","BrandPartCode":"EX123-AB","Title":"long title"},
      "FeaturesGroups":[
        {"Features":[
          {"Feature":{"Name":{"Value":"Processor manufacturer"}},"PresentationValue":"Intel"},
          {"Feature":{"Name":{"Value":"Processor model"}},"PresentationValue":"i9-13900H"},
          {"Feature":{"Name":{"Value":"Processor family"}},"PresentationValue":"Intel® Core™ i9"},
          {"Feature":{"Name":{"Value":"On-board graphics card model"}},"PresentationValue":"Intel Iris Xe Graphics"},
          {"Feature":{"Name":{"Value":"Discrete graphics card model"}},"PresentationValue":"NVIDIA GeForce RTX 3050"},
          {"Feature":{"Name":{"Value":"Internal memory"}},"PresentationValue":"16 GB"},
          {"Feature":{"Name":{"Value":"Total storage capacity"}},"PresentationValue":"1 TB"},
          {"Feature":{"Name":{"Value":"Weight"}},"PresentationValue":"1.8 kg"},
          {"Feature":{"Name":{"Value":"Display diagonal"}},"PresentationValue":"39.6 cm (15.6\")"},
          {"Feature":{"Name":{"Value":"Display resolution"}},"PresentationValue":"2880 x 1620 pixels"},
          {"Feature":{"Name":{"Value":"Maximum refresh rate"}},"PresentationValue":"120 Hz"},
          {"Feature":{"Name":{"Value":"Operating system installed"}},"PresentationValue":"Windows 11 Home"}
        ]}]}}
    """;

    [Fact]
    public void Mapper_extracts_and_converts_live_icecat_feature_names()
    {
        using var document = JsonDocument.Parse(Json);
        var record = IcecatLaptopMapper.Map("123", document)!;
        Assert.Equal("ExampleBrand", record.BrandName);
        Assert.Equal("Example Book 15", record.ModelName);
        Assert.Equal("Intel Core i9-13900H", record.CpuName);
        Assert.Equal("NVIDIA GeForce RTX 3050", record.GpuName);
        Assert.Equal(16, record.RamGb);
        Assert.Equal(1000, record.StorageGb);
        Assert.Equal(1.8m, record.WeightKg);
        Assert.Equal(15.6m, record.ScreenSizeInch);
        Assert.Equal(120, record.RefreshRateHz);
        Assert.Equal("2880 x 1620 pixels", record.Resolution);
        Assert.Equal("Windows 11 Home", record.OperatingSystem);
    }

    [Fact]
    public void Missing_features_stay_null_and_onboard_gpu_is_used_without_discrete()
    {
        using var document = JsonDocument.Parse("""
        {"data":{"GeneralInfo":{"Brand":"B","ProductName":"M"},"FeaturesGroups":[{"Features":[
          {"Feature":{"Name":{"Value":"On-board graphics card model"}},"PresentationValue":"Intel UHD Graphics"}]}]}}
        """);
        var record = IcecatLaptopMapper.Map("1", document)!;
        Assert.Equal("Intel UHD Graphics", record.GpuName);
        Assert.Null(record.CpuName);
        Assert.Null(record.RamGb);
        Assert.Null(record.RefreshRateHz);
    }

    [Fact]
    public void Placeholder_texts_and_uma_are_treated_as_missing()
    {
        using var document = JsonDocument.Parse("""
        {"data":{"GeneralInfo":{"Brand":"B","ProductName":"M"},"FeaturesGroups":[{"Features":[
          {"Feature":{"Name":{"Value":"Discrete graphics card model"}},"PresentationValue":"Not available"},
          {"Feature":{"Name":{"Value":"On-board graphics card model"}},"PresentationValue":"UMA"},
          {"Feature":{"Name":{"Value":"Operating system installed"}},"PresentationValue":"No"}]}]}}
        """);
        var record = IcecatLaptopMapper.Map("1", document)!;
        Assert.Null(record.GpuName);
        Assert.Null(record.OperatingSystem);
    }

    [Fact]
    public void Response_without_data_maps_to_null()
    {
        using var document = JsonDocument.Parse("""{"msg":"Not found"}""");
        Assert.Null(IcecatLaptopMapper.Map("1", document));
    }

    [Theory]
    [InlineData("AMD", "AMD Ryzen AI 5", "PRO 450", "AMD Ryzen AI 5 PRO 450")]
    [InlineData("Intel", "Intel Core Ultra 7", "155H", "Intel Core Ultra 7 155H")]
    [InlineData("Intel", "Intel Core i9", "i9-13900H", "Intel Core i9-13900H")]
    [InlineData("Intel", null, "i5-1245U", "Intel i5-1245U")]
    [InlineData("Qualcomm", "Snapdragon X", "X1-26-100", "Qualcomm Snapdragon X X1-26-100")]
    [InlineData(null, null, null, null)]
    public void Cpu_name_combines_supplied_manufacturer_family_and_model(string? maker, string? family, string? model, string? expected) =>
        Assert.Equal(expected, IcecatLaptopMapper.BuildCpuName(maker, family, model));

    [Theory]
    [InlineData("16 GB", 16)]
    [InlineData("512GB", 512)]
    [InlineData("2 TB", 2000)]
    [InlineData("8192 MB", null)]
    [InlineData("n/a", null)]
    public void Gigabyte_parsing_does_not_guess_units(string text, int? expected) =>
        Assert.Equal(expected, IcecatLaptopMapper.ParseGigabytes(text));

    [Theory]
    [InlineData("1.8 kg", 1.8)]
    [InlineData("1800 g", 1.8)]
    [InlineData("heavy", null)]
    public void Weight_is_converted_to_kilograms(string text, double? expected)
    {
        decimal? expectedKg = expected.HasValue ? (decimal)expected.Value : null;
        Assert.Equal(expectedKg, IcecatLaptopMapper.ParseWeightKg(text));
    }
}

public sealed class LaptopImportTests
{
    private sealed class FakeProvider(Dictionary<string, ExternalLaptopRecord?> data) : IExternalLaptopProvider
    {
        public string ProviderName => "fake";
        public Task<ExternalLaptopRecord?> GetLaptopAsync(string externalId, CancellationToken cancellationToken = default) =>
            externalId == "boom" ? throw new HttpRequestException("network") : Task.FromResult(data.GetValueOrDefault(externalId));
    }

    private sealed class FakeRepository : ILaptopImportRepository
    {
        public HashSet<string> Existing { get; } = [];
        public List<Product> Added { get; } = [];
        public int Saves { get; private set; }
        private readonly Dictionary<string, Brand> brands = [];
        private readonly Dictionary<string, Category> categories = [];

        public Task<bool> ReferenceExistsAsync(string provider, string externalId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Existing.Contains($"{provider}:{externalId}"));
        public Task<Brand> GetOrAddBrandAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!brands.TryGetValue(name, out var brand)) brands[name] = brand = new Brand { Name = name };
            return Task.FromResult(brand);
        }
        public Task<Category> GetOrAddCategoryAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!categories.TryGetValue(name, out var category)) categories[name] = category = new Category { Name = name };
            return Task.FromResult(category);
        }
        public void Add(Product product) => Added.Add(product);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) { Saves++; return Task.CompletedTask; }
    }

    private static ExternalLaptopRecord Laptop(string id, string brand = "Acer", string? model = "Aspire 5") =>
        new(id, brand, model, "Core i5", null, 16, 512, 1.7m, 60, 15.6m, "1920x1080", null);

    [Fact]
    public async Task Preparer_normalizes_and_isolates_failures()
    {
        var noHardware = new ExternalLaptopRecord("4", "3M", "Privacy Filter", null, null, null, null, 0.2m, null, 15.6m, null, null);
        var provider = new FakeProvider(new() { ["1"] = Laptop("1"), ["2"] = null, ["3"] = Laptop("3", model: null), ["4"] = noHardware });
        var prepared = await new LaptopImportPreparer(provider).PrepareAsync(["1", "2", "3", "boom", "4"]);
        Assert.True(prepared[0].IsUsable);
        Assert.Contains("no record", prepared[1].FailureReason);
        Assert.Contains("model", prepared[2].FailureReason);
        Assert.Contains("Fetch failed", prepared[3].FailureReason);
        Assert.Contains("No CPU, GPU, RAM or storage", prepared[4].FailureReason);
    }

    [Fact]
    public async Task Report_counts_nulls_and_ranges_from_usable_laptops_only()
    {
        var provider = new FakeProvider(new() { ["1"] = Laptop("1"), ["2"] = Laptop("2") with { RamGb = 32, WeightKg = null } });
        var prepared = await new LaptopImportPreparer(provider).PrepareAsync(["1", "2", "missing"]);
        var report = LaptopImportReportBuilder.Build(prepared);
        Assert.Equal(3, report.RequestedCount);
        Assert.Equal(2, report.UsableCount);
        Assert.Equal(2, report.NullSummaries.Single(s => s.Field == "GpuName").NullCount);
        Assert.Equal(1, report.NullSummaries.Single(s => s.Field == "WeightKg").NullCount);
        var ram = report.Ranges.Single(r => r.Field == "RamGb");
        Assert.Equal(16m, ram.Min);
        Assert.Equal(32m, ram.Max);
        Assert.Single(report.UniqueCpus);
        Assert.Equal(2, report.UniqueCpus[0].Count);
    }

    [Fact]
    public async Task Import_creates_product_with_reference_and_development_values_and_skips_unpriced_or_duplicate()
    {
        var provider = new FakeProvider(new() { ["1"] = Laptop("1"), ["2"] = Laptop("2"), ["3"] = Laptop("3") });
        var prepared = await new LaptopImportPreparer(provider).PrepareAsync(["1", "2", "3"]);
        var repository = new FakeRepository();
        repository.Existing.Add("fake:2");
        var prices = new Dictionary<string, DevelopmentPriceEntry>
        {
            ["1"] = new("1", 899.99m, 7, true), ["2"] = new("2", 100m, 1, true)
        };

        var outcome = await new LaptopImportService(repository).ImportAsync("fake", prepared, prices);

        Assert.Equal(1, outcome.Count(ImportStatus.Imported));
        Assert.Equal(1, outcome.Count(ImportStatus.AlreadyImported));
        Assert.Equal(1, outcome.Count(ImportStatus.SkippedNoDevelopmentPrice));
        var product = Assert.Single(repository.Added);
        Assert.Equal("Acer Aspire 5", product.Name);
        Assert.Equal(899.99m, product.Price);
        Assert.Equal(7, product.Stock);
        Assert.Equal("fake", product.ExternalReferences.Single().Provider);
        Assert.Equal("1", product.ExternalReferences.Single().ExternalId);
        Assert.Null(product.LaptopSpecification!.GPU);
        Assert.Equal(16, product.LaptopSpecification.RamGb);
        Assert.Equal(1, repository.Saves);
    }
}
