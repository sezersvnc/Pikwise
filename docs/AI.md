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

## Data licensing for LLM input
Icecat-sourced product facts may be sent to the LLM only as request-time input for
explanations (written permission, ADR-025). They must never be used for training,
fine-tuning or embeddings, must not be stored by the LLM provider (zero data retention),
and any other AI use needs a new question to Icecat. Development prices are demo data and
must not be described as market prices.
