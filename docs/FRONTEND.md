# FRONTEND.md — Pikwise Web Interface

The frontend is built by a teammate. The backend team does not change frontend code;
this document is the shared contract: the owner's product requirements, the screens,
and how each screen uses the existing API. API details are in [API.md](API.md); this
file links to them instead of repeating every field.

It merges the earlier frontend plan (TASKS.md Sessions 16-17, ROADMAP Stage 13) with
the owner's requirements of 8 October 2026.

---

## 1. Principles (from the backend architecture)

```text
Product DB            = facts
Recommendation Engine = decision (deterministic Top 3)
LLM                   = understanding + explanation
Frontend              = shows these; never re-ranks, never invents facts
```

- The ranking comes only from the API. The frontend must not reorder, re-score or
  "pick a winner" itself.
- AI text is shown as an explanation of the ranking, never as the decision.
- Prices in the current dataset are development demo prices. Show them with a visible
  "Demo fiyat" label; never call them market or current prices (ADR-024, ADR-025).
- Product facts come from Open Icecat; show a small "Ürün verileri: Open Icecat" note in
  the footer (ADR-025 keeps attribution).
- No secrets in frontend source. Only the Supabase project URL and the public anon
  (publishable) key may be in frontend configuration; never the service_role key and
  never the Groq key.

## 2. Recommended tech (unchanged from Session 16)

- React or Next.js, TypeScript preferred
- The existing ASP.NET Core API
- Supabase Auth (`supabase-js`) for registration, login and sessions

Suggested structure:

```text
frontend/
├── src/
│   ├── app/ or pages/
│   ├── components/
│   ├── features/
│   │   ├── landing/
│   │   ├── auth/
│   │   ├── advisor/          # needs input, loading, results, AI comments
│   │   ├── products/
│   │   ├── comparison/
│   │   └── favorites/
│   ├── services/
│   │   └── api/              # one token-aware API client
│   ├── hooks/
│   ├── types/                # mirrors docs/API.md contracts
│   ├── utils/
│   └── config/
├── public/
└── package.json
```

---

## 3. Owner requirements

### R1 — Public landing page (no login)
- Anyone can open it without an account.
- Introduces Pikwise: who we are, why it was built, what it does. For example:
  "Pikwise, ihtiyacını kendi cümlelerinle anlatmana göre laptopları şeffaf bir puanlama
  ile sıralar ve yapay zekâ ile nedenini açıklar."
- Top right: **Giriş yap** and **Kayıt ol** buttons.
- After login the landing content stays reachable (for example a "Pikwise nedir?"
  page or the home page itself); it is not hidden from signed-in users.

### R2 — Login and registration
- **Giriş yap** opens the login screen; **Kayıt ol** opens registration. Both use
  Supabase Auth directly from the frontend. Pikwise has no login or registration
  endpoint.
- After a successful login the user goes to the needs screen (R3).
- Top right after login: the user's e-mail or name, and **Çıkış yap**.

### R3 — Needs screen (signed-in users)
1. A large text box (prompt) where the user describes the need in their own words,
   for example: "50 bin TL bütçem var, okul ve yazılım için kullanacağım, arada oyun
   oynarım, çok ağır olmasın." Max 1000 characters; show a counter.
2. On submit, a **loading screen** is shown until **everything** is ready (both API
   calls in section 5). Nothing partial is shown while loading.
3. When loading ends, **below the prompt box**:
   - "Anladığımız kriterler": the interpreted criteria as small chips (bütçe, min RAM,
     önem dereceleri) and any `unsupported` wishes ("Bu isteği henüz
     değerlendiremiyoruz: battery life").
   - The Top 3 laptops in ranking order, each with its **AI comment** (explanation).
   - The value comment (`valueComment`) under the list.
4. **Editable criteria (decided 8 October 2026):** the user can correct a chip (for
   example change the budget, remove a minimum RAM, change an importance level 1..5)
   and press "Yeniden sırala". This re-runs only the second call
   (`POST /api/recommendations/explanation`) with the edited criteria; it does not call
   `/criteria` again, so it costs one request instead of two. Invalid edits come back as
   400 with `errors` per field (same rules as API.md) and are shown on the chip.
5. For now the AI only comments; the conversation is the advisor (R4).

### R4 — Personal advisor section (bottom of the results)
- **Decided 8 October 2026: the advisor is the AI**, not a human. At the bottom of the
  results there is a "Kişisel danışmana sor" section where the user asks follow-up
  questions about the shown results ("B, A'dan 4.000 TL ucuz, alır mıyım?",
  "Hangisi daha hafif?").
- The AI answers only from the same verified data as the explanation (criteria, Top 3
  facts, scores, value analysis). It cannot change the ranking, pick another winner or
  add facts.
- **The backend endpoint does not exist yet** (planned as its own backend session with
  an ADR, see section 7). Until then the section is a placeholder: the question box is
  shown disabled with "Yakında".

---

## 4. Screens and routes

```text
/                 Landing (public)                       R1
/login            Supabase login                         R2
/register         Supabase registration                  R2
/advisor          Needs screen + results (signed in)     R3, R4   (Roadmap "AI Advisor" / "Recommendation")
/products         Product list, filter/sort/paging       Session 16
/products/:id     Product detail                         Session 16
/compare          Comparison (2-4 products)              Session 10 API
/favorites        Favorites (signed in)                  Session 18
/about            Optional: landing content for signed-in users
```

Routes marked "signed in" redirect to `/login` without a session and come back after
login.

---

## 5. Needs screen flow (R3) and API usage

```text
User submits text
  │  loading screen starts ("İhtiyacın anlaşılıyor…")
  ▼
POST /api/recommendations/criteria         { text }                       (token)
  │  -> criteria + unsupported
  │  loading text: "Laptoplar puanlanıyor ve yorumlar hazırlanıyor…"
  ▼
POST /api/recommendations/explanation      body = criteria (unchanged)   (token)
  │  -> recommendation (Top 3 + summary + valueAnalysis) + explanations + valueComment
  ▼
Loading ends; criteria chips, Top 3 with AI comments, value comment, advisor section
```

- Send `criteria` from the first response **unchanged** as the body of the second call.
- Show the ranking from `recommendation.items` (already ordered); match each AI comment
  by `productId` from `explanations`.
- Show each product's facts from the response (`specification`, `price` with
  "Demo fiyat", `score`). Do not add facts that are not in the response; a null field is
  shown as "Bilgi yok".
- Each call can take up to about 15 seconds, so the whole wait can be about 30
  seconds. The loading screen should show progress text for the two steps and must
  not time out sooner.
- Disable the submit button while loading (prevents double requests and protects the
  rate limit).

### Error handling (all errors are `application/problem+json`)

| Status | Where | What the UI does |
|---|---|---|
| 400 | criteria | Show `errors.Text` under the box (empty or over 1000 characters). |
| 401 | both | Session expired: refresh the Supabase session once and retry; otherwise go to `/login`. |
| 429 | both | "Çok sık istek gönderdin, X saniye sonra tekrar dene." X = `Retry-After` header. |
| 502 | criteria | "İsteğini anlayamadık, farklı kelimelerle tekrar dener misin?" |
| 503 | criteria | "Yapay zekâ şu an kullanılamıyor." (offer manual criteria later) |
| 502 / 503 | explanation | Fallback: call `POST /api/recommendations` with the same criteria (public, no LLM) and show the Top 3 **without** AI comments, with the note "Yorumlar şu an hazırlanamadı." |
| 200, empty `items` | explanation | "Bu kriterlere uyan laptop bulunamadı." and show the criteria chips so the user can rephrase. |

### Rate limit
Both AI endpoints share **5 requests per user per minute** and 25 in total. One search
uses 2 requests, so a user can run about 2 searches per minute. The UI should make the
429 message friendly rather than look like a failure.

---

## 6. Authentication (merged from Session 17)

```text
User -> Supabase login/register (supabase-js) -> access token
     -> frontend API client -> Authorization: Bearer <token> -> Pikwise API
```

- After login call `GET /api/auth/me` once; it creates or loads the local Pikwise
  profile. 403 there means the profile could not be created (for example no e-mail).
- One shared API client adds the Bearer token to protected calls and handles 401/403.
- Persist the session with Supabase's own session handling; do not store tokens in
  custom cookies or in source.
- Public endpoints the frontend uses (no token): `GET /api/products`,
  `GET /api/products/{id}`, `GET /api/products/compare` and `POST /api/recommendations`.
  Token required: `/api/auth/*`, `/api/favorites/*`, `/api/recommendations/criteria`,
  `/api/recommendations/explanation`.
- The product write endpoints (POST/PUT/DELETE `/api/products`) are not part of the
  user-facing frontend; do not build screens for them.

---

## 7. Backend work this frontend needs (backend team)

These are not done yet and need the owner's approval before implementation:

- [ ] **CORS**: the API has no CORS policy, so a browser app on another origin (for
  example `http://localhost:3000`) cannot call it. Add an allow-list policy for the
  frontend origin(s), configured per environment.
- [ ] **AI advisor endpoint (R4)**: a follow-up question endpoint on the shown results.
  Needs its own session and ADR: the same verified input as the explanation, a fact
  check on the answer, the shared language model rate limit, and a decision on whether
  the conversation is stateless (the client resends the context) or stored. The LLM
  must still not pick or change the winner.

## 8. Decisions and open points

Decided by the owner on 8 October 2026:
1. **Personal advisor (R4):** the AI, as follow-up questions on the shown results.
2. **Criteria review (R3):** the user can edit the criteria chips and re-rank.
3. **Brand:** logo and colours as in section 9.

Still open:
- Final Turkish texts for the landing page.
- Vector (SVG) or transparent PNG logo files; the JPGs are in docs/brand/ (section 9).
- Google sign-in, terms/privacy (KVKK) texts (section 10).

## 9. Brand

| Token | Colour | Use |
|---|---|---|
| Vivid Blue | `#2563EB` | primary: logo symbol, primary buttons, links, active navigation |
| Deep Navy | `#0F172A` | wordmark, headings, body text, dark backgrounds and the dark logo variant |
| Accent Light Blue | `#0EA5E9` | accents: highlights, chips, focus rings, chart lines |

- Logo: a "P" combined with a check mark and an upward arrow ("the right pick"),
  followed by the lowercase wordmark "pikwise".
- Variants: horizontal logo on light backgrounds (blue symbol, navy wordmark); on Deep
  Navy backgrounds (blue symbol, white wordmark); single-colour black. The symbol alone
  is used as favicon (16x16), app icon (32x32) and profile avatar.
- Wordmark font: a geometric sans-serif; confirm the exact font name from the logo source.
- Keep enough contrast: Accent Light Blue on white is for decoration, not for body text.

The brand sheet's example phone screen is only a style reference. Its content does not
match Pikwise and must not be copied: Pikwise recommends **laptops** (not clothing),
prices are in **TL** and labelled "Demo fiyat" (not `$`), and there is **no price
history** yet (that is post-MVP store/price tracking, ROADMAP Stage 14). The screen name
"Pikwise Advisor" fits the `/advisor` route.

Logo files (added 9 October 2026, `docs/brand/`; copy to `frontend/public/brand/`):

| File | Use |
|---|---|
| `pikwise-logo-horizontal-light.jpg` | header and login/register on light backgrounds |
| `pikwise-logo-horizontal-dark.jpg` | dark sidebar / Deep Navy backgrounds |
| `pikwise-logo-mono-black.jpg` | single-colour use (print, documents) |
| `pikwise-symbol.jpg` | favicon and small places |
| `pikwise-app-icon.jpg` | app icon, profile avatar, social media |

These are the only approved logos. They are raster JPGs with a white or navy
background; vector (SVG) or transparent PNG versions are still wanted for sharp
rendering on any background.

## 10. Mockup review (9 October 2026)

The first landing, login, register and dashboard mockups match the brand colours and
the overall flow, but several elements promise features Pikwise does not have. The
frontend must not show them, because they would mislead users.

All screens:
- Use only the logos in section 9. The mockups use three different "P" marks and the
  capitalised "Pikwise"; the brand is the P + check + arrow symbol and lowercase "pikwise".
- Laptops only (MVP). Remove Telefon, Kulaklık, Monitör, Tablet and "her kategori" texts.
- No price tracking, price alarms, stock alarms, price drops ("-5% Düşüşte", "(stabil)"),
  market status or "üyelere özel fırsat" (post-MVP, ROADMAP Stage 14).
- No store names, store logos or "Mağazaya git" buttons; there are no store offers yet
  (post-MVP Session 22). Prices are TL with the "Demo fiyat" label.
- No product photos are stored; use a neutral laptop illustration or icon.
- Turkish UI text throughout (no "Go to Store", "Save", "Saved").
- No invented social proof ("Trusted by innovative teams" with made-up logos).

Landing:
- The "Pikwise'a sor" box is fine as an entry point, but the AI endpoints need login:
  on submit, keep the text, send the visitor to login/register and continue to
  `/advisor` with the same text afterwards.
- Replace placeholder texts ("Multi-produkt intelligence") with real copy.

Login / register:
- E-mail + password first. "Google ile giriş" needs a Supabase Google provider set up
  first (later decision); remove "Apple" (needs a paid Apple developer account).
- "Şifremi unuttum" uses Supabase password reset (needs the reset redirect URL configured).
- "Kullanım Koşulları & Gizlilik Politikası" needs real texts (including a KVKK
  aydınlatma metni) before it is shown.
- "Ad Soyad" may be stored in Supabase user metadata; the Pikwise backend does not store names.
- Remove claims the product does not support (fiyat & stok takibi, güvenli alışveriş,
  alışverişleriniz; Pikwise does not sell products).

Dashboard (`/advisor`):
- Cards must follow the API: left to right rank 1, 2, 3 with the score (for example
  "70,03 / 100"). Do not invent card titles such as "En Yüksek Performans" or "En
  Ekonomik Alternatif". The only extra badge allowed is **"En iyi fiyat/performans"** on
  the product with `valueAnalysis.bestValueProductId`.
- The AI intro text must use real numbers from `recommendation.summary`, for example
  "25 laptop arasından kriterlerine uyan 17 tanesini puanladım; en uygun 3'ü:". Never
  numbers like "54 mağaza".
- "Yapay Zekâ Yorumu" shows `explanations[].explanation` exactly; the frontend writes no
  comment itself.
- The right-hand "Filtreleme Özeti" becomes "Anladığımız kriterler": editable chips,
  the `unsupported` list (for example "Batarya henüz değerlendirilmiyor") and the
  "Yeniden sırala" button (R3).
- Remove "Piyasa Durumu", the purchase-redirect banner, "Alarm" buttons and the sidebar
  items "Fiyat Alarmları", "Takip Listem", "Geçmiş" and the categories; "Keşfet" appears
  twice. Keep: AI Asistanım (`/advisor`), Laptoplar (`/products`), Karşılaştır
  (`/compare`), Favorilerim, Ayarlar/Çıkış.
- Keep the heart (favorites API exists: `POST/DELETE /api/favorites/{productId}`).
- Add what is missing: the value comment under the cards, the AI advisor section at the
  bottom (disabled, "Yakında"), the loading screen, "Demo fiyat" labels and the Open
  Icecat attribution.
- Spec line per card from `specification` (CPU, GPU, RAM, storage, weight, screen,
  refresh rate); null fields as "Bilgi yok". A "Neden bu puan?" toggle may list
  `components` (criterion and contribution).

## 11. Frontend tasks (Sessions 16-17, extended)

- [ ] Create the frontend project; API base URL and Supabase settings from environment config.
- [ ] Shared, token-aware API client; types mirroring docs/API.md.
- [ ] Landing page with Giriş yap / Kayıt ol (R1).
- [ ] Login, registration, logout, session persistence, `GET /api/auth/me` after login (R2).
- [ ] Protected routes with redirect to `/login`.
- [ ] Needs screen: prompt box with counter, full-screen loading for both calls, results under the prompt (R3).
- [ ] Criteria chips and `unsupported` notes; editable chips with "Yeniden sırala" (explanation call only).
- [ ] Top 3 cards with facts, "Demo fiyat", score and AI comment; value comment.
- [ ] Error states from section 5, including the 502/503 fallback and the 429 message.
- [ ] AI advisor section at the bottom, disabled with "Yakında" until the backend endpoint exists (R4).
- [ ] Brand colours, logo variants and favicon from section 9.
- [ ] Product list, detail, filtering, sorting, pagination; compare page.
- [ ] Footer with Open Icecat attribution.
- [ ] Responsive base layout.
- [ ] Keep API contracts aligned with docs/API.md.

## Exit criteria
- A visitor understands Pikwise on the landing page and can register or log in.
- A signed-in user types a need, waits on one loading screen, and sees the interpreted
  criteria, the Top 3 with AI comments and the value comment, plus the advisor section.
- The user can edit a criterion and re-rank without retyping the need.
- The interface uses the Pikwise brand (section 9).
- AI failures never hide the deterministic ranking.
