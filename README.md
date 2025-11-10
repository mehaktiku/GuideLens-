---
# GuideLens: City Travel Curator Guide
---

<img width="975" height="975" alt="image" src="https://github.com/user-attachments/assets/57d6b61e-4eeb-40c1-9dc5-a40f1bbe3228" />

# Overview
- Curated, **offline-first** guide initially focused on **Cincinnati**, designed to scale across **Ohio** (Columbus, Cleveland, Dayton).
- Users can **search**, **filter by Category & Neighborhood**, **sort**, and **paginate** curated cards showing **Best Offer** and **Tip**.
- UI includes a full-bleed **hero**, glass **filter panel**, **city tiles**, **category tiles**, and responsive **pagination**.

# Introduction
- **Goal:** Fast discovery by **Category**, **Neighborhood**, and **City** without a live backend.
- **Current Scope:** **Cincinnati** data live; other Ohio cities visible in UI with “Coming soon.”
- **Design:** Image-driven pages, near-edge layout, accessible forms/labels.

# Tech Highlights
- **Offline-first:** Local JSON under `JsonData/` loaded at startup.
- **Typed Query:** `RecommendationQuery { Q, Category, Neighborhood, City, SortBy, Page, PageSize }`.
- **Paging:** `PagedResult<T> { Items, Page, PageSize, TotalCount, TotalPages }`.
- **Service Layer:** `RecommendationService` (DI singleton) handles load/filter/sort/page.
- **Razor Pages:** `Index` (filters + results), `Cities` (city tiles), `City` (category tiles).
- **Images:** `wwwroot/img/Hero-img.jpg`, `wwwroot/img/cities/{city}.*`, `wwwroot/img/cat/{category}.*`.
- **Planned:** **OpenAPI** read endpoint, **XML export + XSD**, **Nominatim** deep links, **Open-Meteo** sunset hints.

# Storyboard
## Screen 1 – Home (default city)
- **Hero** over `Hero-img`; **Filter Bar** (City, Search, Category, Neighborhood, Sort, Apply).
- **Cards:** Name, **Category • Neighborhood**, **Best Offer**, **Tip**.
- **Pagination:** Prev / 1..N / Next with querystring persistence.

## Screen 2 – Filtered (Food)
- **Example:** **Graeter’s** → **Best Offer:** *Black Raspberry Chocolate Chip*.

## Screen 3 – Filtered (Museums)
- **Example:** **Cincinnati Art Museum** → **Best Offer:** *Rookwood Pottery collection*.

## Screen 4 – Filtered (Fall Photos)
- **Example:** **Ault Park** → **Best Offer:** *Pavilion fountain shot*.
- **Planned:** Photo Time Hint via **Open-Meteo**.

## Screen 5 – Search
- **Action:** Typing **“Eden”** narrows to **Eden Park**, etc.

## Screen 6 – Cities & Explore
- **Cities Page:** Large tiles for current live city and others as *Coming soon*.
- **City Explore:** Category tiles (Things to do, Museums, Food, Parks, Fall Photos, Landmarks; Itineraries/Travel Tips/Tourist Map = coming soon).

## Screen 7 – Export & Enrich (Planned)
- **Export:** Results → **XML** validated by **XSD**.
- **Enrich:** **Open Map** (OSM/Nominatim) + **Sunset** (Open-Meteo).

# Projects (Tasks)
## Phase 1 – Setup & Data
- Create Razor Pages solution; model `Recommendation.cs` with `Name, Category, Neighborhood, TheBestOffer, NoteTip`.
- Seed `JsonData/CincinnatiData.json` (≥ 25 curated entries).
- Implement `DataLoader.LoadAll()` → `List<Recommendation>`.

## Phase 2 – Filtering & Search
- `RecommendationService` holds in-memory list; exposes filters and search.
- **Filter:** Category/Neighborhood.
- **Search:** Case-insensitive substring on `Name`.
- **Sort:** `name`, `area` (Neighborhood), `category`.

## Phase 3 – Interface Design
- Home = Hero + glass filter panel + results cards.
- Bind query via `BindProperty(SupportsGet = true)` to `RecommendationQuery`.
- Implement `Page` + `PageSize` with URL routing helpers.

## Phase 4 – Multi-City UI
- Add `City` to query (default current live city).
- `/Cities` shows city tiles using `wwwroot/img/cities/{slug}.*`.
- `/City/{slug}` shows category tiles using `wwwroot/img/cat/{key}.*` with optional per-city overrides.

## Phase 5 – Testing & Polish
- **Smoke:** App loads; ≥ 25 items; filters/search/sort/paging work together.
- **UX:** Accessible labels, contrast, keyboard flow, focus states.
- **Perf:** Images compressed (WebP/JPG), cache-busted CSS, minimal layout shift.

# Requirements
## 1) Filter by Category
- **Dependencies:** Local JSON loaded in memory.
- **Assumptions:** ≥ 25 entries with valid categories.
- **Examples:** Food / Museums / Fall Photos return only matching items.

## 2) Search by Name
- **Dependencies:** Dataset in memory.
- **Assumptions:** Case-insensitive substring on `Name`.
- **Examples:** “Eden” → Eden Park; “zzzznotfound” → empty.

## 3) View Best Offer & Tip
- **Dependencies:** JSON includes `TheBestOffer`, `NoteTip`.
- **Assumptions:** Card displays both clearly.

## 4) Open Map Link (Planned)
- **Dependencies:** Construct OSM search URL or use Nominatim for coordinates.
- **Assumptions:** Link opens in default maps.

## 5) Photo Time Hint (Planned)
- **Dependencies:** Open-Meteo; known coordinates (dataset or Nominatim).
- **Assumptions:** Graceful offline fallback.

# Data Sources
## Primary (Offline, Curated)
- `JsonData/CincinnatiData.json` with `Name`, `Category`, `Neighborhood`, `TheBestOffer`, `NoteTip`.

## Open/Free APIs (Planned)
- **Nominatim / OpenStreetMap:** Place search → lat/lon → map link.
- **Open-Meteo:** Daily/hourly forecasts including sunrise/sunset.

## Optional Enrichment
- **OpenTripMap:** Nearby POIs.
- **Wikipedia:** Summaries for landmarks.
- **Cincinnati Open Data:** Parks/facilities datasets.

# Architecture & Files
- **Models:** `Recommendation.cs`, `RecommendationQuery.cs`, `PagedResult.cs`.
- **Data:** `DataLoader.cs` loads JSON from `JsonData/...`.
- **Services:** `RecommendationService.cs` (DI singleton) for filter/sort/paging.
- **Pages:** `Index` (filters + results), `Cities` (city tiles), `City` (category tiles).
- **Images:** `wwwroot/img/Hero-img.jpg`, `wwwroot/img/cities/{slug}.*`, `wwwroot/img/cat/{key}.*`.
- **Styles:** `wwwroot/css/site.css` with `.gl-hero-full`, `.gl-search-panel`, `.option-card`, `.gl-edge`.

# Run & Build
- **Run:** `dotnet run` then open `https://localhost:{port}`.
- **Cache Busting:** `_Layout.cshtml` references `site.css` with `asp-append-version="true"`.
- **Static Files:** `Program.cs` includes `app.UseStaticFiles();`.

# Team Composition
- **Data Model & Persistence:** Models, JSON dataset, XSD (planned).
- **Filtering & Search:** Service, LINQ filters/search, sorting, paging, XML helpers (planned).
- **UI/UX:** Layout, hero, tiles, accessibility, screenshots.
- **Detail & Integrations:** Open Map, Photo Time Hint, OpenAPI, testing.
