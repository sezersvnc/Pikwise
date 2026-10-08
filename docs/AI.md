# AI.md — Pikwise AI Boundaries

## Architectural boundary

```text
Product Database = source of truth
Recommendation Engine = decision layer
LLM = understanding + explanation layer
```

## AI may
- convert natural-language needs into structured criteria,
- explain why a product fits,
- summarize trade-offs,
- explain score components,
- answer questions using verified ProductFacts + EngineScore.

## AI must not
- independently choose the winning product,
- invent specifications,
- invent price/stock,
- override hard constraints,
- become the product database,
- modify critical product data without validation.

## Intended flow

```text
Natural language
  ->
LLM structured criteria
  ->
Recommendation Engine
  ->
Top 3 + score breakdown
  ->
LLM explanation
  ->
Client
```

## Structured input (Session 14)
`POST /api/recommendations/criteria` implements the first arrow of the flow, as a
separate call: the client reviews the criteria and sends them to the engine itself.

- The language model receives only the user's text, with
  `RequirementExtractionContract.Instructions` and its JSON Schema. No product data is sent.
- `IRequirementExtractor` is the provider abstraction (text in, raw JSON out). Providers
  live in Infrastructure; Application never sees provider types.
- The output is untrusted. RequirementParsingService rejects (502) anything that is not
  exactly the schema, any value outside the manual request rules and any budget when the
  text has no digits. Nothing is clamped or repaired.
- Vague wishes become importance levels, not hard constraints; uncovered wishes are
  reported in `unsupported` instead of being guessed.
- Timeout 15 s; provider failures and timeouts answer 503. The user's text is never logged.
- No provider is configured yet (`UnconfiguredRequirementExtractor`, 503). Before a real
  provider is enabled: owner approval, authentication plus rate limiting on the endpoint,
  and the provider's retention/training terms checked (ADR-026).

## Explanation (Session 15)
`POST /api/recommendations/explanation` implements the last arrows of the flow.

- ExplanationService ranks through IRecommendationService first; the model never sees
  products outside the Top 3 and never decides. An empty ranking makes no model call.
- Input (`ExplanationInput`, serialized by `ExplanationContract.SerializeInput`): criteria,
  candidate/ranked counts, Top 3 stored facts, scores, score components (value and
  contribution) and valueAnalysis. Null facts are unknown.
- `IExplanationGenerator` is the provider abstraction (input JSON in, raw JSON out).
- Output: one explanation per product plus an optional valueComment. Strict JSON, then
  `ExplanationFactChecker`: each Top 3 product exactly once, no value comment without a
  value analysis, text 1..1200 characters, and every number in the text must appear in
  the input (Turkish "68.000" / "1,5", English "68,000", "68 bin", rounding to 0-2 decimals
  allowed). Any failure rejects the whole explanation (502).
- Known limits: numbers written as words and invented facts without digits are not
  detected; a number is checked against the whole input, not per product. The
  instructions forbid both, and the ranking never depends on the model.
- No provider yet (`UnconfiguredExplanationGenerator`, 503). This is the first feature that
  sends Icecat data, so the ADR-025 conditions (zero retention, no training, request-time
  input only) must be verified when the provider is chosen.

## Safety against hallucination
When generating explanations, provide only:
- UserNeeds
- verified ProductFacts
- EngineScore / score components

If a fact is missing, the model should say it is unavailable rather than invent it.

## Data licensing for LLM input
Icecat-sourced product facts may be sent to the LLM only as request-time input for
explanations (written permission, ADR-025). They must never be used for training,
fine-tuning or embeddings, must not be stored by the LLM provider (zero data retention),
and any other AI use needs a new question to Icecat. Development prices are demo data and
must not be described as market prices.
