using System.Globalization;

namespace Pikwise.Application.ExternalProducts;

// Price, stock and active flag for one imported laptop. Open Icecat supplies no commercial data,
// so these values come from a companion CSV and are DEVELOPMENT/TEST data, not market data.
public sealed record DevelopmentPriceEntry(string ExternalId, decimal Price, int Stock, bool IsActive);

public sealed record DevelopmentPriceParseResult(
    IReadOnlyDictionary<string, DevelopmentPriceEntry> Entries, IReadOnlyList<string> Errors);

// Parses the companion CSV. Required header columns: ExternalId, Price, Stock, IsActive.
// Extra columns (for example a note) are ignored; blank lines and lines starting with # are skipped.
// Prices use a dot as the decimal separator regardless of the machine's culture.
public static class DevelopmentPriceCsvParser
{
    private static readonly string[] RequiredColumns = ["ExternalId", "Price", "Stock", "IsActive"];

    public static DevelopmentPriceParseResult Parse(TextReader reader)
    {
        var entries = new Dictionary<string, DevelopmentPriceEntry>(StringComparer.Ordinal);
        var errors = new List<string>();
        Dictionary<string, int>? columns = null;
        var lineNumber = 0;
        while (reader.ReadLine() is { } line)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#')) continue;
            var cells = line.Split(',').Select(cell => cell.Trim()).ToArray();
            if (columns is null)
            {
                columns = cells.Select((name, index) => (name, index))
                    .GroupBy(pair => pair.name, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(group => group.Key, group => group.First().index, StringComparer.OrdinalIgnoreCase);
                var missing = RequiredColumns.Where(required => !columns.ContainsKey(required)).ToArray();
                if (missing.Length > 0)
                {
                    errors.Add($"Line {lineNumber}: header is missing column(s): {string.Join(", ", missing)}.");
                    return new DevelopmentPriceParseResult(entries, errors);
                }
                continue;
            }
            ParseRow(cells, columns, lineNumber, entries, errors);
        }
        if (columns is null) errors.Add("The file has no header row.");
        return new DevelopmentPriceParseResult(entries, errors);
    }

    private static void ParseRow(string[] cells, Dictionary<string, int> columns, int lineNumber,
        Dictionary<string, DevelopmentPriceEntry> entries, List<string> errors)
    {
        string Cell(string name) => columns[name] < cells.Length ? cells[columns[name]] : string.Empty;
        var externalId = Cell("ExternalId");
        if (externalId.Length == 0) { errors.Add($"Line {lineNumber}: ExternalId is empty."); return; }
        if (entries.ContainsKey(externalId)) { errors.Add($"Line {lineNumber}: duplicate ExternalId {externalId}."); return; }
        if (!decimal.TryParse(Cell("Price"), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price)
            || price < 0 || decimal.Round(price, 2) != price)
        { errors.Add($"Line {lineNumber}: Price must be a non-negative number with at most 2 decimals (use a dot)."); return; }
        if (!int.TryParse(Cell("Stock"), NumberStyles.None, CultureInfo.InvariantCulture, out var stock))
        { errors.Add($"Line {lineNumber}: Stock must be a non-negative whole number."); return; }
        var activeText = Cell("IsActive");
        bool isActive;
        if (activeText.Equals("true", StringComparison.OrdinalIgnoreCase) || activeText == "1") isActive = true;
        else if (activeText.Equals("false", StringComparison.OrdinalIgnoreCase) || activeText == "0") isActive = false;
        else { errors.Add($"Line {lineNumber}: IsActive must be true/false or 1/0."); return; }
        entries[externalId] = new DevelopmentPriceEntry(externalId, price, stock, isActive);
    }
}
