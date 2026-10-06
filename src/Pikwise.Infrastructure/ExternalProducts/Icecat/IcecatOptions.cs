namespace Pikwise.Infrastructure.ExternalProducts.Icecat;

// Open Icecat settings. Username and Password must come from User Secrets or environment
// variables (Icecat__Username, Icecat__Password) and must never be committed.
public sealed class IcecatOptions
{
    public const string SectionName = "Icecat";

    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Language { get; set; } = "en";
    public string LiveBaseUrl { get; set; } = "https://live.icecat.biz/api";
    public string IndexUrl { get; set; } = "https://data.icecat.biz/export/freexml/EN/files.index.xml.gz";
    public string CategoriesUrl { get; set; } = "https://data.icecat.biz/export/freexml/refs/CategoriesList.xml.gz";
    // Pause between product requests to respect Icecat's fair-use rate limits.
    public int RequestDelayMilliseconds { get; set; } = 1000;
}
