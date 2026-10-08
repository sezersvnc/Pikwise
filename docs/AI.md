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
