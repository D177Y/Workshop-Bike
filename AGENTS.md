# AGENTS

## Frontend Definition Of Done (Blazor + IIS)

Use this checklist for any visual/UI change before handing back for testing.

1. Confirm IIS serving path and binding first (`workshop.local` -> `_publish/workshop`).
2. Publish to IIS folder, then restart IIS before visual verification:
   - `dotnet publish .\Workshop\Workshop.csproj -c Release -o .\_publish\workshop /p:UseAppHost=false`
   - If locked, stop `Workshop` app pool, publish, start app pool.
   - `iisreset`
3. Verify compiled CSS/markup in publish output (`_publish/workshop/wwwroot/Workshop.styles.css`), not only source files.
4. In `.razor.css`, use `::deep` selector syntax for deep targeting.
5. Guard against global form outlines on page scope when needed:
   - `.valid.modified:not([type="checkbox"])`
   - `.invalid`
6. Scope vertical spacing rules to top-level form sections; avoid generic sibling rules that break two-column alignment.
7. For paired two-column fields, verify:
   - Label baselines align left/right.
   - Input/select heights align left/right.
   - Password row aligns even with helper text/validation.
8. If deploy/caching is uncertain, apply a temporary high-contrast visual test, verify in browser, then revert and redeploy.
9. Final check: hard-refresh behavior confirmed (`Ctrl+F5`) and report exact files changed.

