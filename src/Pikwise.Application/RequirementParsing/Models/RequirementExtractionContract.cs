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
        - budgetMax is the maximum price in Turkish lira. "50 bin TL" means 50000.
        - If the budget is written only in words (for example "elli bin"), leave budgetMax null and
          add "budget must be written with digits" to unsupported.
        - Vague wishes such as "not too heavy" or "lots of storage" are importance levels, never hard constraints.

        Importance levels (ram, storage, cpu, gpu, weight, refreshRate):
        - 1 = not important, 3 = normal, 5 = very important; null when the message does not imply the criterion.
        - weight importance means a lighter laptop is preferred.
        - Examples: occasional gaming raises gpu and refreshRate; software development raises cpu and ram;
          carrying the laptop daily raises weight.

        unsupported: short English phrases for wishes that none of the fields cover
        (for example battery life, screen type, operating system, brand, colour). Empty array when none.
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
