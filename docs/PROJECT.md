# PROJECT.md — Pikwise

## Problem
Users can compare specifications manually, but raw specifications do not directly answer:
- Which laptop fits my needs?
- Which option gives better value?
- What trade-offs am I making?
- Is paying more actually worth it?

## Solution
Pikwise stores verified product facts, applies deterministic recommendation rules, and exposes comparison/recommendation results through a Web API.

AI may later:
- understand natural-language needs,
- explain recommendation results,
- summarize trade-offs.

AI does **not** become the decision engine.

## MVP scope
Category: **Laptops**

Initial backend capabilities:
- product CRUD,
- brand/category/specification model,
- filtering,
- pagination,
- comparison,
- deterministic recommendation,
- value-for-money analysis,
- authentication,
- later favorites/preferences.

## Out of scope for MVP
- Microservices
- Redis/Kafka/event bus
- Multiple product categories
- Browser extension
- Mobile app
- Large scraping pipeline
- LLM-controlled ranking
- Premature optimization

## Main users
- Visitor: browse, filter, compare.
- Authenticated user: later save favorites/preferences.
- Admin: later manage product data.

## V0.1 success criteria
The backend can:
1. connect to SQL Server,
2. persist related laptop data,
3. expose clean product endpoints,
4. keep Controller/Service/Repository responsibilities separated,
5. authenticate protected requests,
6. return controlled DTO responses,
7. compare products,
8. produce deterministic recommendations,
9. explain why the ranking happened.
