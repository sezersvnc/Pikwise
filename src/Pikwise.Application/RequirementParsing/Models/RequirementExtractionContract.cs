namespace Pikwise.Application.RequirementParsing.Models;

// Provider-neutral structured-output contract. A provider adapter sends Instructions as the
// system message, the user's text as the only user message and JsonSchema as the output format.
// No product data is ever part of the request.
public static class RequirementExtractionContract
{
    public const string Instructions = """
        You convert a laptop shopper's message (usually Turkish) into JSON that matches the given schema.
        Return only the JSON object. Do not recommend, name or rank any product.

        Hard constraints (budgetMax, minRamGb, minStorageGb, maxWeightKg):
        - Set one only when the user states an explicit number for it; otherwise null.
        - budgetMax is the maximum price in Turkish lira. An amount that contains digits IS written with
          digits, also when followed by a word: "50 bin TL" = 50000, "50.000 TL" = 50000, "50000 TL" = 50000,
          "50k" = 50000, "1,2 milyon" = 1200000. "bin" means thousand.
        - Only when the amount contains no digit at all (for example "elli bin") leave budgetMax null and
          add "budget must be written with digits" to unsupported.
        - Vague wishes such as "çok ağır olmasın" or "bol depolama" are importance levels, never hard constraints.

        Importance levels (ram, storage, cpu, gpu, weight, refreshRate):
        - 5 = very important, 4 = important, 3 = mentioned neutrally, 2 = less important, 1 = not needed.
          null = the message says nothing related. Do not answer 3 for everything: infer from the use cases.
        - weight importance means a lighter laptop is preferred.
        - Cues:
          software development, coding, "yazılım", "kod", "programlama" -> cpu 4, ram 4;
          occasional gaming ("arada", "bazen" oyun) -> gpu 4, refreshRate 3;
          frequent or serious gaming -> gpu 5, refreshRate 5;
          no gaming ("oyun oynamam") -> gpu 1, refreshRate 1;
          carrying it, "hafif olsun", "ağır olmasın" -> weight 4; "çok", "kesinlikle" before it -> weight 5;
          video editing or design -> cpu 4, gpu 4, ram 4, storage 4; many files or games installed -> storage 4;
          office work, school, web and films only -> cpu 2, ram 3 unless something else raises them.
          When several cues apply, use the highest level for each criterion.

        unsupported: short English phrases for wishes that none of the fields cover
        (for example battery life, screen type, operating system, brand, colour). Empty array when none.

        Example. Message: "35 bin TL'ye kadar, çoğunlukla ofis işleri ve film için, her gün çantamda
        taşıyacağım, oyun oynamam, pil ömrü uzun olsun."
        Answer: {"budgetMax":35000,"minRamGb":null,"minStorageGb":null,"maxWeightKg":null,
        "importance":{"ram":3,"storage":null,"cpu":2,"gpu":1,"weight":5,"refreshRate":1},
        "unsupported":["battery life"]}
        """;

    // Every property is required and nullable, as strict structured-output modes expect.
    // Numeric ranges are enforced after parsing by the recommendation request validator.
    public const string JsonSchema = """
        {
          "type": "object",
          "additionalProperties": false,
          "required": ["budgetMax", "minRamGb", "minStorageGb", "maxWeightKg", "importance", "unsupported"],
          "properties": {
            "budgetMax": { "type": ["number", "null"], "description": "Maximum price in TL, only when stated with digits." },
            "minRamGb": { "type": ["integer", "null"], "description": "Minimum RAM in GB, only when stated." },
            "minStorageGb": { "type": ["integer", "null"], "description": "Minimum storage in GB, only when stated." },
            "maxWeightKg": { "type": ["number", "null"], "description": "Maximum weight in kg, only when stated." },
            "importance": {
              "type": "object",
              "additionalProperties": false,
              "required": ["ram", "storage", "cpu", "gpu", "weight", "refreshRate"],
              "properties": {
                "ram": { "type": ["integer", "null"], "description": "1..5" },
                "storage": { "type": ["integer", "null"], "description": "1..5" },
                "cpu": { "type": ["integer", "null"], "description": "1..5" },
                "gpu": { "type": ["integer", "null"], "description": "1..5" },
                "weight": { "type": ["integer", "null"], "description": "1..5, higher prefers lighter" },
                "refreshRate": { "type": ["integer", "null"], "description": "1..5" }
              }
            },
            "unsupported": { "type": "array", "items": { "type": "string" } }
          }
        }
        """;
}
