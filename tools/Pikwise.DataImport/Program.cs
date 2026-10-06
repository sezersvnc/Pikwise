using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pikwise.Application.ExternalProducts;
using Pikwise.DataImport;
using Pikwise.Infrastructure.ExternalProducts;
using Pikwise.Infrastructure.ExternalProducts.Icecat;
using Pikwise.Infrastructure.Persistence;

// Development tool for Session 11.5. Commands:
//   discover  --out <manifest.json> [--prices-template <csv>] [--category-id <id>] [--count 30] [--suppliers 12] [--exclude-suppliers 7,9] [--max-scan 100000000]
//   categories --search <text> [--head]          (finds Icecat category ids; --head prints the raw file start)
//   inspect   --id <icecatId>                       (prints raw feature names/values; no database)
//   import    --manifest <manifest.json> [--prices <csv>] [--apply]
// `import` is a DRY RUN unless --apply is given. A dry run never opens a database connection.

var arguments = Arguments.Parse(args);
if (arguments.Command is null)
{
    Console.WriteLine("Usage: discover | inspect | import  (see the comment at the top of Program.cs)");
    return 1;
}

// Credentials come from User Secrets or environment variables only (Icecat__Username, Icecat__Password).
var configuration = new ConfigurationBuilder()
    .AddUserSecrets(typeof(Arguments).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.Configure<IcecatOptions>(configuration.GetSection(IcecatOptions.SectionName));
services.AddHttpClient<IcecatLaptopProvider>(client => client.Timeout = TimeSpan.FromMinutes(2));
services.AddHttpClient<IcecatIndexDiscovery>(client => client.Timeout = TimeSpan.FromMinutes(30));
await using var provider = services.BuildServiceProvider();
var progress = new Progress<string>(message => Console.WriteLine(message));
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancellation.Cancel(); };

try
{
    return arguments.Command switch
    {
        "discover" => await DiscoverAsync(),
        "inspect" => await InspectAsync(),
        "categories" => await CategoriesAsync(),
        "import" => await ImportAsync(),
        _ => Fail($"Unknown command '{arguments.Command}'.")
    };
}
catch (Exception exception) when (exception is InvalidOperationException or HttpRequestException or IOException or ArgumentException)
{
    return Fail(exception.Message);
}

static int Fail(string message)
{
    Console.Error.WriteLine($"Error: {message}");
    return 1;
}

async Task<int> DiscoverAsync()
{
    var outPath = arguments.Require("out");
    var count = arguments.Int("count", 30);
    var discovery = provider.GetRequiredService<IcecatIndexDiscovery>();
    var categoryIds = arguments.Get("category-id") is { } categoryId ? new[] { categoryId } : null;
    var candidates = await discovery.DiscoverAsync(
        count, arguments.Int("suppliers", 12), arguments.Int("max-scan", 100_000_000), categoryIds,
        arguments.Get("exclude-suppliers")?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
        progress, cancellation.Token);
    var manifest = new ImportManifest("icecat", DateTimeOffset.UtcNow,
        candidates.Select(c => new ManifestEntry(c.ExternalId, c.SupplierId, c.BrandPartCode, c.ModelName)).ToList());
    manifest.Save(outPath);
    Console.WriteLine($"Wrote {manifest.Entries.Count} entries to {outPath}");

    if (arguments.Get("prices-template") is { } templatePath)
    {
        // Price is left blank on purpose: it must be filled in by hand as development/test data.
        var lines = new List<string>
        {
            "# DEVELOPMENT/TEST DATA ONLY. Not real market prices. Removed in Session 13.",
            "ExternalId,Price,Stock,IsActive,Note"
        };
        lines.AddRange(manifest.Entries.Select(e => $"{e.ExternalId},,10,true,{(e.ModelName ?? e.BrandPartCode ?? string.Empty).Replace(',', ' ')}"));
        File.WriteAllLines(templatePath, lines);
        Console.WriteLine($"Wrote price template to {templatePath} (fill in Price; Stock/IsActive are suggestions).");
    }
    return 0;
}

async Task<int> CategoriesAsync()
{
    var discovery = provider.GetRequiredService<IcecatIndexDiscovery>();
    if (arguments.Flag("head"))
    {
        Console.WriteLine("=== First 3000 characters of the category file ===");
        Console.WriteLine(await discovery.ReadCategoriesHeadAsync(3000, cancellation.Token));
        Console.WriteLine("=== End ===");
    }
    var term = arguments.Require("search");
    var matches = await discovery.SearchCategoriesAsync(term, exact: false, cancellation.Token);
    Console.WriteLine($"{matches.Count} category name(s) containing '{term}':");
    foreach (var match in matches)
        Console.WriteLine($"  category id={match.CategoryId} langid={match.LangId ?? "-"} name={match.Name}");
    return 0;
}

async Task<int> InspectAsync()
{
    var id = arguments.Require("id");
    var icecat = provider.GetRequiredService<IcecatLaptopProvider>();
    var features = await icecat.GetFeaturesAsync(id, cancellation.Token);
    if (features is null) return Fail($"No data for Icecat product {id}.");
    foreach (var pair in features.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase))
        Console.WriteLine($"{pair.Key} = {pair.Value}");
    return 0;
}

async Task<int> ImportAsync()
{
    var manifest = ImportManifest.Load(arguments.Require("manifest"));
    var apply = arguments.Flag("apply");

    var prices = new Dictionary<string, DevelopmentPriceEntry>();
    if (arguments.Get("prices") is { } pricesPath)
    {
        using var reader = new StreamReader(pricesPath);
        var parsed = DevelopmentPriceCsvParser.Parse(reader);
        foreach (var error in parsed.Errors) Console.WriteLine($"price csv: {error}");
        if (apply && parsed.Errors.Count > 0) return Fail("Fix the price CSV errors before using --apply.");
        prices = new Dictionary<string, DevelopmentPriceEntry>(parsed.Entries);
    }

    var icecat = provider.GetRequiredService<IcecatLaptopProvider>();
    var preparer = new LaptopImportPreparer(icecat);
    var prepared = await preparer.PrepareAsync(manifest.Entries.Select(e => e.ExternalId).ToList(), progress, cancellation.Token);
    var report = LaptopImportReportBuilder.Build(prepared);
    ReportPrinter.Print(prepared, report, prices);

    if (!apply)
    {
        Console.WriteLine();
        Console.WriteLine("DRY RUN: nothing was written to the database. Use --apply to import.");
        return 0;
    }

    var connectionString = configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
        return Fail("Configure ConnectionStrings:DefaultConnection using User Secrets or environment variables.");
    var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;
    await using var context = new ApplicationDbContext(dbOptions);
    var service = new LaptopImportService(new LaptopImportRepository(context));
    var outcome = await service.ImportAsync(icecat.ProviderName, prepared, prices, cancellation.Token);

    Console.WriteLine();
    Console.WriteLine("=== Import result ===");
    foreach (var item in outcome.Items)
        Console.WriteLine($"  [{item.ExternalId}] {item.ProductName} -> {item.Status}{(item.Reason is null ? "" : $" ({item.Reason})")}");
    foreach (ImportStatus status in Enum.GetValues<ImportStatus>())
        Console.WriteLine($"  {status}: {outcome.Count(status)}");
    return 0;
}

namespace Pikwise.DataImport
{
    // Minimal "--name value" / "--flag" parser; the first token is the command.
    public sealed class Arguments
    {
        private readonly Dictionary<string, string?> values = new(StringComparer.OrdinalIgnoreCase);
        public string? Command { get; private init; }

        public static Arguments Parse(string[] args)
        {
            var parsed = new Arguments { Command = args.Length > 0 && !args[0].StartsWith("--") ? args[0] : null };
            for (var i = parsed.Command is null ? 0 : 1; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--")) continue;
                var name = args[i][2..];
                var hasValue = i + 1 < args.Length && !args[i + 1].StartsWith("--");
                parsed.values[name] = hasValue ? args[++i] : null;
            }
            return parsed;
        }

        public string? Get(string name) => values.GetValueOrDefault(name);
        public bool Flag(string name) => values.ContainsKey(name);
        public string Require(string name) => Get(name) ?? throw new ArgumentException($"Missing --{name}.");
        public int Int(string name, int fallback) =>
            Get(name) is { } text && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : fallback;
    }
}
