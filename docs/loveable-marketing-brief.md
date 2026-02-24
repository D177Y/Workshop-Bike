# VeloLabs Marketing Website Brief (for Loveable)

## Project Goal
Design and implement a modern, professional **marketing website surface** for VeloLabs (Calendly-inspired style direction), focused on conversion before signup.

This is a redesign of only these public marketing routes:
- `/` (Homepage)
- `/pricing`
- `/features`
- `/how-it-works`

## Critical Scope Constraints
1. Do **not** restyle the main authenticated product app.
2. Do **not** modify app workflows/pages outside the 4 marketing routes above.
3. Keep the project in **Blazor + Syncfusion**.
4. Do not migrate framework, routing model, or auth system.
5. Keep existing backend/business logic intact.

## Tech Constraints (Must Follow)
1. Framework: ASP.NET Core Blazor (server interactive mode).
2. UI library: Syncfusion Blazor remains in place.
3. Theme strategy:
   - Preferred Syncfusion base theme: **Fluent 2** (or Tailwind3 if justified).
   - Theme changes should be scoped to marketing pages where possible.
4. Reusable components required for marketing UI:
   - `Container`
   - `Section`
   - `PrimaryButton`
   - `PricingCard`
   - `FeatureRow`
   - `FAQAccordion`
   - `TestimonialCard`
   - `Badge`
5. Styling approach:
   - Shared token file with CSS variables (brand, text, spacing, radius, shadow).
   - Minimal global CSS impact.
   - Prefer component-level/route-level styles.
6. Accessibility:
   - Keyboard accessible nav and CTAs
   - Visible focus styles
   - Good heading hierarchy and color contrast
7. Performance:
   - No heavy hero images/video.
   - Use lightweight placeholders/mock panels where needed.

## Brand + Product Context
Product: **VeloLabs** — workshop scheduling software for bike retailers.

Core capabilities to reflect in content:
- Multi-store setup and access
- Mechanic management and capacity-aware booking
- Service packages and individual services
- Quote -> booking workflow
- Job cards and status tracking
- Customer comms/email updates
- Timetastic integration (time-off sync)

## Design Direction (Inspiration, Not Copy)
Reference vibe: Calendly.com

Desired visual characteristics:
- clean, modern, conversion-first
- substantial whitespace
- strong typographic hierarchy
- minimal borders, soft shadows
- blue accent
- rounded, clear CTAs

Do not copy Calendly wording, layout, icons, or assets.

## Information Architecture

### 1) Homepage (`/`)
Sections in this order:
1. Minimal top nav
   - Left: VeloLabs logo/name
   - Right: Product, Pricing, Resources, Sign in, CTA (Start free trial)
2. Hero
   - Desktop 2-column, mobile 1-column
   - Left: headline, subhead, CTA row
   - Right: product preview mock panel
3. Social proof strip
   - Trusted by line + location pills
4. 3 key benefits cards
   - 1-minute setup
   - Real-time capacity
   - Workshop-first scheduling
5. 2–3 feature deep-dives (alternating content/panel)
6. Testimonials (2–3 cards)
7. Final CTA band

### 2) Pricing (`/pricing`)
Sections:
1. Pricing hero (headline + subhead)
2. Billing toggle (Monthly/Annual)
3. 3 pricing cards
   - Starter
   - Growth (**Most popular**)
   - Multi-store
4. Feature comparison table (8–12 features)
5. FAQ accordion (6–8 items)
6. Final CTA

### 3) Features (`/features`)
Sections:
1. Hero intro
2. Feature category cards
3. 2–3 deep dive feature rows
4. CTA to how-it-works/signup

### 4) How It Works (`/how-it-works`)
Sections:
1. Hero intro
2. Step-by-step operational flow:
   - Stores/access setup
   - Mechanics/capacity
   - Service packages/services
   - Intake/quotes/booking
   - Job card execution
   - Integrations (Timetastic)
3. Persona value blocks (service desk / mechanics / managers)
4. CTA row

## Copy Guidelines
Tone: confident, practical, workshop-operations focused.

Rules:
- short sentences
- benefit-led, not fluffy
- no generic startup jargon
- no fake enterprise claims
- no copied Calendly text

## CSS Token Targets
Define variables in one place (example values):
- `--brand: #1a73e8`
- `--text: #0f172a`
- `--muted: #475569`
- `--bg: #ffffff`
- `--bg-soft: #f8fafc`
- `--border: rgba(15, 23, 42, 0.08)`
- `--shadow: 0 10px 30px rgba(2, 6, 23, 0.08)`
- `--radius-lg: 16px`

## Responsive Behavior
- Mobile-first
- Nav collapses gracefully
- Hero stacks cleanly
- Pricing cards stack on smaller widths
- Tables horizontally scroll when needed

## Acceptance Criteria
1. Only `/`, `/pricing`, `/features`, `/how-it-works` are visually redesigned.
2. Existing app pages (authenticated product) remain unchanged.
3. Build passes with no errors.
4. Marketing pages are responsive and accessible.
5. Reusable marketing components are used (not giant monolithic Razor files).
6. Syncfusion remains intact; no framework migration.

## Deliverables Expected from Loveable
1. Updated Razor markup for the 4 target pages.
2. Reusable marketing components.
3. Scoped CSS + token files.
4. Clear change list with file paths.
5. Notes on how to tune brand color/radius/spacing quickly.

## Out of Scope
- Product app redesign
- Auth flow rewrites
- Database/model/business logic changes
- Replacing Syncfusion with another UI library
