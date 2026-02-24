# Frontend Debug Runbook (IIS + Blazor CSS Isolation)

Use this when a UI change looks correct in source but not in `workshop.local`.

## 1) Confirm where IIS is serving from
- Check site binding and physical path first.
- In this project, `workshop.local` serves from `_publish/workshop`, not source folders.

## 2) Always deploy source changes before visual verification
- Required sequence:
1. `dotnet publish .\Workshop\Workshop.csproj -c Release -o .\_publish\workshop /p:UseAppHost=false`
2. If files are locked, stop `Workshop` app pool, publish, start pool.
3. `iisreset`

## 3) Validate compiled CSS/markup, not only source files
- Verify expected selectors/strings in `_publish/workshop/wwwroot/Workshop.styles.css`.
- If compiled output does not contain expected rules, fix source selector syntax before further styling tweaks.

## 4) Blazor CSS isolation rule
- In `.razor.css`, use `::deep ...` for child component/internal element targeting.
- Do not use `:deep(...)` in this codebase; it can remain untransformed and be ignored by browser.

## 5) Prevent hidden global style interference
- Global `app.css` rules (`.valid.modified`, `.invalid`) can distort form control outlines/sizing.
- Override locally on page scope when needed:
  - `.page-scope ::deep .valid.modified:not([type="checkbox"]), .page-scope ::deep .invalid { outline: none; }`

## 6) Spacing rules: scope to form sections only
- Avoid generic sibling selectors like `.mk-field + .mk-field` unless strictly needed.
- Prefer top-level-only spacing to avoid pushing right-hand column down:
  - `.form > .section + .section { margin-top: ... }`

## 7) Two-column form alignment baseline
- On row container: `align-items: start`
- On each field block: `align-content: start`
- Normalize control heights with fixed `height` (not only `min-height`) for strict alignment.

## 8) Fast visual deployment check
- Temporarily set a high-contrast style (for example button red) to confirm deploy path/caching.
- Revert immediately after confirmation and redeploy.

## 9) Verification checklist before handing off
1. Left/right label baselines match in each two-column row.
2. Left/right input heights match in each two-column row.
3. Password row aligns despite helper text/validation text.
4. Border color, radius, and typography match reference page.
5. Published output and IIS restart completed.

