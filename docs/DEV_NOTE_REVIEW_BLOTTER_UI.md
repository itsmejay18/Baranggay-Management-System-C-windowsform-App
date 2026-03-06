# DEV NOTE: Review Blotter UI Refactor

## What Changed
- Refactored `Review Blotter` screen in `Forms/BlotterForm.cs` to a clearer, grid-based layout.
- Rebuilt review header into a **Case Summary** card:
  - Title line (`Case: ...`)
  - Read-only metadata line (case id, filed date, status)
  - Read-only respondent + last-updated line
  - Status dropdown + prominent status badge on the right
  - Action buttons with full labels (no truncation): `AI Panel`, `Schedule Mediation`, `Update Status`, `Print`, `Close`
- Reworked `Overview` tab into two columns:
  - Left: `Incident` + `Details`
  - Right: `Quick info (read-only)` + `Case actions`
- Added helper text/placeholders for incident fields and details.
- Clarified editable vs read-only fields using label text and read-only field styling.
- Added inline validation UX using `ErrorProvider` + in-form validation message label.
- Added real-time validation refresh on input/status changes.
- Updated save flow (`Controllers/BlotterForm.Controller.cs`) to run inline validation first.
- Updated blotter save validation signature/rules in `helper/ValidationService.cs`:
  - `incidentLocation` now validated (<= 120 chars)
  - `incidentDetails` is no longer strictly required
  - existing status/resolution rules preserved
- Improved accessibility wiring:
  - Accessible names/descriptions for key fields/tabs
  - Explicit tab order for header and overview controls
  - Focus/accessibility enhancement call for review layout

## Assumptions
- Framework is **WinForms** (`Form`, `TableLayoutPanel`, `SplitContainer`, `TabControl`, Designer partial classes).
- Existing business behavior for status updates and timeline logging must remain unchanged.
- Review mode remains in the same form (`_blotterIdForAnalysis.HasValue`) and should not affect timeline/witnesses/attachments behavior.

## Manual Test Steps
1. Open Residents screen and open a blotter record in **Review Blotter** mode.
2. Verify header:
   - Case summary is readable and visually separated.
   - Status dropdown and badge are visible.
   - Buttons are not truncated (`AI Panel`, `Schedule Mediation`, `Update Status`, `Print`, `Close`).
3. Verify Overview layout:
   - Left column: Incident + Details cards.
   - Right column: Quick info (read-only) + Case actions.
4. Validation checks:
   - Clear incident type then attempt save -> inline error + warning dialog.
   - Set incident date to future -> validation error.
   - Enter >120 chars in location -> validation error.
   - Change status to non-ongoing with empty resolution -> validation error.
5. Accessibility checks:
   - Use `Tab`/`Shift+Tab` through header controls and overview inputs.
   - Verify visible focus movement and logical order.
6. Regression checks:
   - Open `Timeline`, `Witnesses`, and `Attachments` tabs.
   - Confirm they still load and respond as before.

## Tests
- No separate automated UI test project was found in this repository.
- Validation and navigation were verified through build + manual workflow checks.
