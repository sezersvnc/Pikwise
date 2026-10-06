# RECOMMENDATION_ENGINE.md — Pikwise

## Principle
Recommendation must be deterministic, reviewable, testable, and explainable.

The LLM must not independently decide the winner.

## Planned flow

```text
User Requirements
  ->
Eligibility + Hard Filters
  ->
Normalization
  ->
Weighted Scoring
  ->
Ranking
  ->
Top 3
  ->
Value / Price Analysis (Session 13)
  ->
Optional AI Explanation (Session 15)
```

## Session 11 — design decisions

Status: **design in progress.** Decisions below were made by the project owner in
Session 11 (see ADR-019). Items under "Open decisions" are intentionally not decided;
nothing in this file is implemented yet and no formula constant (range, weight,
tier) has been chosen beyond what is written here.

### Decided

1. **Eligibility.** A product with `IsActive = false` or `Stock = 0` is removed
   before scoring. This applies only to recommendation; catalog, favorites and
   comparison behavior is unchanged.
2. **Hard constraints are strict.** A product that violates a hard constraint
   (for example price above the budget ceiling) is removed before scoring.
   There is no tolerance band.
3. **Score range.** The engine computes component scores in 0..1 and the final
   score is shown on a 0..100 scale (`score = 100 * weighted sum`).
4. **Normalization uses fixed reference ranges.** Every scored criterion has a
   documented `[min, max]` reference range. A value is clamped to the range and
   mapped to 0..1. Scores therefore do not shift when the catalog changes. The
   concrete ranges are not chosen yet (see Open decisions).
   - Higher-is-better criteria: `n = (clamp(v) - min) / (max - min)`
   - Lower-is-better criteria (for example weight): `n = 1 - (clamp(v) - min) / (max - min)`
5. **Importance and weights.** Each criterion receives an importance level from
   1 to 5. A criterion the user does not mention gets level 3 (medium), so default
   weights are equal. Weight = level / sum of levels of the criteria that are
   scored for that product.
6. **Missing data.** A criterion whose value is unknown for a product is left
   out of that product's score and the remaining weights are rescaled so they
   again sum to 1. The result must report which criteria were unknown and the
   share of the original weight that was actually known. Nothing is guessed and
   no penalty is applied.
7. **CPU and GPU.** Free-text `Processor` and `GPU` are scored through a
   manually maintained, deterministic tier table. A model that is not in the
   table is treated as unknown (rule 6). The table and its tiers are not defined yet.
8. **Tie-breaking.** Equal scores are ordered by price ascending, then by
   product Id ascending.
9. **Output.** The engine returns the Top 3 with per-criterion components
   (raw value, normalized value, weight, contribution), unknown criteria and the
   known-weight share.

### Formula

```text
For a product p and the set K of criteria whose value is known for p:

  w_i      = level_i / sum(level_j for j in K)
  score(p) = 100 * sum(w_i * n_i(p) for i in K)
```

### Worked example (mechanics only)

The products, reference ranges and importance levels below are **illustrative
numbers chosen to demonstrate the arithmetic. They are not real laptops and not
project decisions.**

Illustrative ranges: RAM 8..32 GB (higher is better), weight 1.0..3.0 kg (lower is
better), storage 256..2048 GB (higher is better). Illustrative importance: RAM 5,
weight 3, storage 3.

| Product | RAM n (5/11) | Weight n (3/11) | Storage n (3/11) | Score |
|---|---|---|---|---|
| A: 16 GB, 1.4 kg, 512 GB | 0.3333 | 0.8000 | 0.1429 | 40.87 |
| B: 32 GB, 2.6 kg, 1024 GB | 1.0000 | 0.2000 | 0.4286 | 62.60 |
| C: 16 GB, 2.0 kg, 512 GB | 0.3333 | 0.5000 | 0.1429 | 32.68 |
| D: 16 GB, weight unknown, 1024 GB | 0.3333 (5/8) | unknown | 0.4286 (3/8) | 36.90 (known weight share 8/11) |

Check for A: `100 * (5/11*0.3333 + 3/11*0.8000 + 3/11*0.1429) = 40.87`.
Ranking for this example: B, A, D, C. A ranks above C because both have the same
RAM and storage but A is lighter. D is scored only on RAM and storage with
weights 5/8 and 3/8 and is reported with a known-weight share of 8/11.

### Open decisions (not decided, do not implement)

- The set of scored criteria and the direction of each (for example whether
  screen size, resolution or refresh rate score at all, and how).
- The reference range `[min, max]` of every criterion. Intended to be chosen
  together with the Session 11.5 dataset, not guessed.
- The CPU and GPU tier table: tier scale, which models, where it is stored.
- Whether price is also a scored criterion or only a hard filter (budget ceiling)
  in V1. Value-for-money is Session 13.
- Behavior when a hard-constraint field is unknown for a product (for example
  the product has no specification). Safest candidate: it cannot be shown to
  satisfy the constraint, so it is removed; not confirmed.
- Minimum known-weight share below which a product is not recommended.
- Rounding rule for comparing scores (candidate: decimal arithmetic, compare
  scores rounded to two decimals before tie-breaking).
- Concrete hard constraints list and request model (`UserRequirements`):
  budget ceiling, minimum RAM and storage, maximum weight, required OS and GPU
  class are candidates from the original draft.
- Battery life and usage type appear in the original candidate inputs but there
  is no battery field in `LaptopSpecification`; they cannot be scored until
  verified data exists.
- Current schema note: all `LaptopSpecification` fields are non-null, so
  field-level missing data cannot occur today; only a missing specification
  can. Session 11.5 may need nullable fields or a skip-record rule.

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

## Value-for-Money
The engine should distinguish:
- strongest product
- best fit
- best value

It should be able to identify:
- large price increase for tiny suitability gain,
- cheaper product with nearly equal fit,
- meaningful vs meaningless upgrades.

Designed in Session 13, after the base engine exists.

## Rule
Do not implement scoring before the open decisions above are explicitly defined.
