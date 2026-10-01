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

## Safety against hallucination
When generating explanations, provide only:
- UserNeeds
- verified ProductFacts
- EngineScore / score components

If a fact is missing, the model should say it is unavailable rather than invent it.
