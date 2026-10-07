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

Status: **design complete (Session 11 closed); implemented in Session 12.** All
decisions below were made by the project owner (see ADR-019 and ADR-021). The
implementation follows them exactly; see "Session 12 — implementation" below and ADR-022.

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
   concrete ranges are listed in decision 14.
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
   no penalty is applied. A product whose known-weight share is below 50% is not
   recommended (decision 16).
7. **CPU and GPU.** Free-text `Processor` and `GPU` are scored through a
   manually maintained, deterministic tier table (decision 15). A model that is not
   in the table is treated as unknown (rule 6).
8. **Tie-breaking.** Equal scores are ordered by price ascending, then by
   product Id ascending.
9. **Output.** The engine returns the Top 3 with per-criterion components
   (raw value, normalized value, weight, contribution), unknown criteria and the
   known-weight share.
10. **Scored criteria (V1).** Six criteria are scored: RAM (`RamGb`, higher is
    better), storage (`StorageGb`, higher is better), CPU tier (from `Processor`,
    higher is better), GPU tier (from `GPU`, higher is better), weight (`Weight`,
    lower is better) and refresh rate (`RefreshRate`, higher is better). Screen
    size, resolution, operating system and price are not scored in V1; they remain
    verified facts shown with the result.
11. **Hard constraints (V1).** Budget ceiling (`Price <= BudgetMax`), minimum RAM
    (`RamGb >= MinRamGb`), minimum storage (`StorageGb >= MinStorageGb`) and maximum
    weight (`Weight <= MaxWeightKg`). Operating-system and GPU-requirement
    constraints are not part of V1.
12. **Price in V1.** Price only acts as the budget hard filter and is not a score
    component. Price/performance (value-for-money) analysis is a planned, central
    Pikwise capability and must be added in Session 13; it is kept out of the base
    fit score so that "best fit" and "best value" stay separate.
13. **Unknown hard-constraint field.** If a field needed by an active hard
    constraint is unknown for a product (including a missing specification), the
    product is removed. It cannot be shown to satisfy the constraint.
14. **Reference ranges.** Values are clamped to the range, then mapped to 0..1.

    | Criterion | Range | Direction |
    |---|---|---|
    | RAM | 8 .. 32 GB | higher is better |
    | Storage | 256 .. 1024 GB | higher is better |
    | Weight | 1.0 .. 2.5 kg | lower is better |
    | Refresh rate | 60 .. 165 Hz | higher is better |
    | CPU tier | 1 .. 5 | higher is better, `n = (tier - 1) / 4` |
    | GPU tier | 1 .. 5 | higher is better, `n = (tier - 1) / 4` |

    Chosen after reviewing the Session 11.5 dataset (RAM 8-64 GB, storage
    256-1000 GB, weight 0.828-2.54 kg, refresh 60-165 Hz). RAM tops out at 32 GB so
    that 16 GB is not scored as nearly worthless; 64 GB clamps to 1.0.
15. **CPU/GPU tier table (V1).** Five tiers. A name matches only by exact,
    case-insensitive comparison with the normalized names produced by the importer;
    there is no fuzzy matching. The table lives in code (a static class in the
    Application layer, unit tested), not in the database. It is maintained by hand
    and extended when new models are imported. Tiers reflect general performance
    class and were approved by the project owner.

    | Tier | CPU |
    |---|---|
    | 1 | Intel Core i3-1305U |
    | 2 | Intel Core i7-1255U, Intel Core i7-1265U, Intel Core 5 120U, Qualcomm Snapdragon X1-26-100, Intel Core Ultra 5 325 |
    | 3 | AMD Ryzen 7 170, AMD Ryzen 5 7533HS, Intel Core Ultra 7 155U, Intel Core Ultra 7 355, AMD Ryzen AI 5 PRO 435 |
    | 4 | Intel Core Ultra 7 155H, Intel Core Ultra 7 366H, AMD Ryzen AI 7 350, AMD Ryzen AI 7 PRO 450, AMD Ryzen 7 260 |
    | 5 | Intel Core Ultra 9 386H, Intel Core 9 270H, Intel Core Ultra 7 255HX |

    | Tier | GPU |
    |---|---|
    | 1 | Intel UHD Graphics |
    | 2 | Intel Iris Xe Graphics, AMD Radeon 680M, AMD Radeon 840M |
    | 3 | AMD Radeon 860M, Intel Arc Graphics |
    | 4 | NVIDIA GeForce RTX 4050, NVIDIA GeForce RTX 5050, NVIDIA GeForce RTX 5060 |
    | 5 | NVIDIA GeForce RTX 5070, NVIDIA GeForce RTX 5070 Laptop GPU, NVIDIA GeForce RTX 5080 Laptop GPU |

    "Intel Graphics" is deliberately not listed: Icecat uses that name for very
    different integrated GPUs, so it does not identify a model and stays unknown.
16. **Minimum known-weight share.** A product is not recommended when the criteria
    known for it carry less than 50% of the total importance.
17. **Rounding.** Scores are computed with `decimal`, rounded half-up to two decimals,
    and compared after rounding; ties then fall to decision 8.
18. **UserRequirements (confirmed).** All hard constraints are optional (absent means
    not applied); a budget is not mandatory. Importance levels are integers 1..5 and
    default to 3.

```text
UserRequirements
  Hard constraints (each optional)
    BudgetMax      decimal   Price ceiling
    MinRamGb       int       Minimum RAM
    MinStorageGb   int       Minimum storage
    MaxWeightKg    decimal   Maximum weight
  Importance, integer 1..5, default 3 when absent
    Ram, Storage, Cpu, Gpu, Weight, RefreshRate
```

A usage type (school, gaming, development) is not a field of this model.
Translating a described need into importance levels and constraints is the
Session 14 LLM step; the engine only receives the structured values above.
Validation limits of the request (for example the maximum accepted budget) are an
implementation detail for Session 12.

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

### Verification with real data (Session 11 exit criterion)

Hand calculation on the 25 laptops imported in Session 11.5, using the decisions
above. Prices are development/test data, not market prices.

Sample user: budget 70,000, minimum RAM 16 GB; importance RAM 3, storage 3, CPU 4,
GPU 2, weight 5, refresh rate 1 (total 18).

Removed before scoring: 2 by eligibility (MSI Modern A16 J1M-010NLN inactive,
Dynabook PZ/LY zero stock), 5 by budget, 1 by minimum RAM (Fujitsu UH90/G2, 8 GB).
17 products were scored.

| Rank | Product | Price | Known share | Score |
|---|---|---|---|---|
| 1 | Dell PW514265 | 68,000 | 18/18 | 75.41 |
| 2 | Dell PW516265 | 70,000 | 18/18 | 66.33 |
| 3 | Fujitsu UH90/J3 | 55,000 | 17/18 | 64.71 |
| 4 | GIGABYTE AERO X16 | 62,000 | 18/18 | 63.37 |

Why 1 ranks above 2: the two Dell models have identical RAM (32 GB), storage
(1000 GB), CPU (Ryzen AI 7 PRO 450, tier 4) and GPU (Radeon 860M, tier 3). Only
weight differs: 1.40 kg (n = 0.733) versus 1.89 kg (n = 0.407). With weight
importance 5 of 18: `100 * 5/18 * (0.733 - 0.407) = 9.07` points, which matches
75.41 - 66.33.

Check for rank 1: `100 * (3*1.000 + 3*0.969 + 4*0.750 + 2*0.500 + 5*0.733 + 1*0.000) / 18 = 75.41`.

Why 3 ranks above 4: Fujitsu UH90/J3 (0.858 kg, weight n = 1.000) beats GIGABYTE
AERO X16 (1.9 kg, n = 0.400) by `5 * 0.6 = 3.0` weighted points, more than the AERO's
advantage in storage, GPU and refresh rate. The Fujitsu's refresh rate is unknown,
so it is scored on 17/18 of the importance and the weights are rescaled.

Observation for Session 12 tests: HP EliteBook 6 G1i has no CPU or GPU data, is
scored on 12/18 of the importance, and still ranks 5th. That is the agreed
missing-data rule (no guess, no penalty, share reported). The 50% threshold
removes products with less data. Fujitsu UQ-L1 carries an Icecat data error (Intel
GPU listed with a Snapdragon CPU) and is scored as supplied.

## Session 12 — implementation

Endpoint: `POST /api/recommendations` (contract in API.md).

```text
RecommendationsController (Api)          HTTP binding, 200/400
  -> RecommendationService (Application) validate, load candidates, run engine, map
       -> IRecommendationRepository      Infrastructure: one AsNoTracking query,
                                         all products with Brand + LaptopSpecification
       -> RecommendationEngine           pure static function, decisions 1-18
            LaptopPerformanceTiers       CPU/GPU tier table (decision 15)
  -> RecommendationMapper                Top 3 + summary -> response DTOs
```

Code lives in `src/Pikwise.Application/Recommendations/` (DTOs, Interfaces, Models,
Scoring, Services, Validators, Exceptions, Mappers). The engine has no I/O, clock,
randomness or LLM, so the same candidates and requirements always give the same result.

Implementation choices (owner-approved, ADR-022):
- Eligibility and hard constraints run in Application on the loaded candidates, not
  in SQL, so every rule sits in one unit-tested place. Revisit when the catalog grows.
- Request limits: budgetMax 0.01..10,000,000 (2 decimals), minRamGb 1..256,
  minStorageGb 1..16384, maxWeightKg 0.1..10 (2 decimals), importance 1..5.
- The score is computed at full decimal precision and then rounded (decision 17).
  Component normalized value, weight and contribution (in score points) are rounded
  to 4 decimals for display only.
- The 50% threshold compares integer importance sums, so exactly 50% is kept.
- The response includes a summary of how many products each rule removed.

Verification:
- `RecommendationEngineTests` reproduces the hand calculation above on the 25 Session
  11.5 laptops: 2 + 6 removed, 17 ranked, 75.41 / 66.33 / 64.71 / 63.37. HP EliteBook
  6 G1i ranks 5th with 12/18 known importance, CPU and GPU reported unknown, weights
  rescaled over 12 levels and no penalty. Other tests cover each rule and boundary
  (eligibility, inclusive constraints, unknown constraint fields, clamping, inverted
  weight, tiers, rescaling, exactly 50%, half-up rounding, ties after rounding, Top 3,
  input-order independence).
- The same request against local PikwiseDb returned the same ranking and summary.

### Not in V1 (by decision)

- Battery life and usage type: there is no verified battery field, so they cannot
  be scored until such data exists.
- Value-for-money analysis: Session 13.

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

Price/performance is a core Pikwise differentiator (see decision 12) and must not be dropped.

### Session 13 — value analysis (implemented, ADR-024)

Owner decisions: one near-equal threshold plus price per point; threshold 3 points;
returned inside the existing recommendation response; the development price layer stays.

Rules (`ValueAnalyzer`, pure static Application code, runs after the engine):
1. Best fit = rank 1 of the full ranking. No ranked product → `valueAnalysis` is null.
2. Every other ranked product (not only the Top 3) that is cheaper than the best fit and
   whose rounded score is at most 3.00 points lower is a near-equal cheaper alternative.
   Gaps use the displayed, rounded scores.
3. For each alternative: score gap, price difference and price per point
   (price difference / score gap, rounded half-up to 2 decimals). A cheaper product with an
   equal score would already rank first, so the gap is always positive.
4. `isSmallGainUpgrade` is true when at least one alternative exists: paying for the best
   fit buys at most 3 points.
5. Best value = the cheapest of the best fit and its alternatives; ties by higher score,
   then lower Id. Without alternatives the best fit is also the best value.
6. Alternatives are listed in ranking order. The ranking itself never changes: best fit
   and best value stay separate answers.

Example (unit test, default importance, products differ only in weight and price):

| Rank | Price | Score | Gap | Price difference | Price per point |
|---|---|---|---|---|---|
| 1 (best fit) | 60,000 | 49.80 | — | — | — |
| 4 | 50,000 | 47.58 | 2.22 | 10,000 | 4,504.50 |

Rank 4 is a near-equal alternative and the best value; `isSmallGainUpgrade = true`.

On the 25 Session 11.5 laptops the tested requests (the Session 11 sample user, `{}`,
budget 60,000, weight importance 5) produce no alternative: the nearest cheaper product is
at least 7.8 points below rank 1. Prices there are development/test data, so value results
on that dataset are for testing only.

## Rule
The code implements exactly the decisions above (Sessions 12 and 13). Any change to a
range, tier, threshold or rule is a new owner decision recorded in DECISIONS.md.
