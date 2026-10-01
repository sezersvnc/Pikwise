# RECOMMENDATION_ENGINE.md — Pikwise

## Principle
Recommendation must be deterministic, reviewable, testable, and explainable.

The LLM must not independently decide the winner.

## Planned flow

```text
User Requirements
  ->
Hard Filters
  ->
Normalization
  ->
Weighted Scoring
  ->
Value / Price Analysis
  ->
Ranking
  ->
Top 3
  ->
Optional AI Explanation
```

## Candidate user inputs
- budget
- usage type
- gaming requirement
- portability importance
- battery importance
- screen importance
- CPU/performance importance
- GPU importance
- minimum RAM/storage

## Hard constraints
Examples:
- budget ceiling
- minimum RAM
- required GPU class
- required OS
- maximum weight

Products failing a hard constraint are removed before scoring.

## Normalization
Different product features must be transformed into comparable ranges before weighted scoring.

Exact formulas are not decided yet.

## Weighted scoring
Each requirement has an importance weight.

The engine must retain score components so the application can answer:
> Why did this product receive this score?

## Value-for-Money
The engine should distinguish:
- strongest product
- best fit
- best value

It should be able to identify:
- large price increase for tiny suitability gain,
- cheaper product with nearly equal fit,
- meaningful vs meaningless upgrades.

## Rule
Do not implement scoring before formulas and missing-data behavior are explicitly defined.
