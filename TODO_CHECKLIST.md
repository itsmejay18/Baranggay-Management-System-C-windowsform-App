# Barangay System TODO Checklist

Use this file to track progress. Mark a task done by changing `[ ]` to `[x]`.

## 1. Core System Logic (High Priority)

- [x] Enforce certificate workflow transitions (`SUBMITTED -> APPROVED -> RELEASED -> ARCHIVED`).
- [x] Enforce blotter case workflow transitions (`ONGOING -> SETTLED/REFERRED -> CLOSED`).
- [x] Block invalid state changes without a reason and permission check.
- [x] Add central validation service used by all controllers before save/update.
- [x] Add duplicate resident detection (name + birthdate + address match rules).
- [x] Add household consistency checks (head/member relationships and active status).

## 2. Accountability and Security (High Priority)

- [x] Add full audit trail table for create/update/delete actions.
- [x] Log `who`, `when`, `module`, `record_id`, `before_json`, `after_json`.
- [x] Add role-permission matrix in DB (not only UI button enable/disable).
- [x] Enforce permissions server-side for every critical action.
- [x] Add soft-delete support for critical records with restore option.

## 3. Operations Automation (Medium Priority)

- [x] Add SLA rules for certificates and blotter cases.
- [x] Auto-flag overdue items into Action Center.
- [x] Add reminder engine (hearing date, pickup date, unresolved case reminders).
- [x] Add notification states (`new`, `read`, `archived`) and mark-all-read action.
- [x] Add recurring scheduler job for daily checks and alerts.

## 4. Certificate and Blotter Completeness (Medium Priority)

- [x] Add certificate numbering rules by year/type.
- [x] Add OR/fee validation before issuing certificates.
- [x] Add certificate verification token/QR for printed output.
- [x] Add blotter case timeline entries (every status/event is logged).
- [x] Add mediation schedule, outcomes, referral destination, and case closure notes.
- [x] Add repeat-respondent flagging logic.

## 5. Reporting and Insights (Medium Priority)

- [x] Build real reports dashboard (monthly residents/certificates/blotter trends).
- [x] Add service-time metrics (request to approval, approval to release).
- [x] Add staff performance metrics by completed and overdue actions.
- [x] Add export to PDF and Excel for major reports.
- [x] Add filter presets by date range, purok, and status.

## 6. Data Safety and Maintenance (High Priority)

- [x] Add scheduled backup job and backup status monitor.
- [x] Version DB migrations and run migrations so itll reflect in mysql display current schema version in app .
- [x] Add startup health checks (DB connectivity, required tables/columns, pending migrations).
- [x] Add error logging sink with daily log rotation.

## 7. UI/UX and Productivity (Low Priority)

- [x] Add quick actions row (`+ Resident`, `+ Certificate`, `+ Blotter`, `Refresh`).
- [x] Add global search (resident, case, certificate, user).
- [x] Add keyboard shortcuts for common actions.
- [x] Add empty-state guidance with clear next actions.
- [x] Add consistent loading states/spinners for slow operations.
- [x] Scan all forms and organize all buttons and designs make sure no overridens and bad ui if using datagridview change it and use a more modern layout.
## 8. Completed

- [x] Keep top navigation/ribbon fixed while modules open in content area.
- [x] Remove subheaders under ribbon title.

## Next Recommended Task

- [x] Next: **Address remaining build warnings (nullability and marshal-by-reference field access) for stability**.

## 10. Phase 2 - UI/UX Polish and Consistency (High Priority - Current Focus)

- [x] Simplify top navigation list (keep only primary tabs, move secondary actions to page content).
- [x] Standardize button sizing, spacing, and visual states across all forms.
- [x] Create shared style helper for cards, section headers, and grid containers.
- [x] Improve small-window behavior with responsive docking rules.
- [x] Add unified empty/error/offline states with clear retry actions.
- [x] Add accessibility pass (tab order, focus highlight, contrast, keyboard-only flow).

## 11. Phase 2 - Feature Gaps (High Priority)

- [x] Add document attachment module for residents, cases, and certificates.
- [x] Add SMS/email notification integration for reminders and releases.
- [x] Add household transfer/history tracking (old address to new address timeline).
- [x] Add barangay clearance renewal tracking with expiry reminders.
- [x] Add incident hotspot map view by purok/date for blotter analytics.

## 12. Phase 3 - UI/UX Hardening (From Latest Code Scan)

- [x] Fix hidden/cut dashboard sections by preventing zero-height feature panels in responsive modes.
- [x] Move `UpdateUser` save/cancel actions into a stable bottom action row (no hidden buttons).
- [x] Ensure empty-state messages wrap properly (no clipped guidance text in cards).
- [x] Rebalance reports split-panel minimum sizing so tabs never collapse to zero height.
- [ ] Add the same responsive height guards to other high-density forms (`UsersListForm`, `Certification`, `BlotterForm`).
- [ ] Add per-form minimum window size + auto-scroll fallback for all CRUD forms.
- [ ] Add a reusable `FormLayoutGuard` helper to centralize resize logic and prevent duplicated docking code.
- [ ] Add compact mode rules for <=`1100x700` windows (smaller paddings, button groups, card spacing).
- [ ] Add visual hierarchy pass: section subtitles + helper text for all blank/first-time states.
- [ ] Add confirmation UX polish: destructive action copy, button color consistency, undo/restore hints.
- [ ] Add keyboard navigation improvements: shortcut hints visible in tooltips and command labels.
- [ ] Add automated UI visual check to release checklist (`--ui-test` pass required before shipping).

## 13. Residents Module UI/UX Stabilization (Current)

- [x] Add a visible `Select Resident` action in profile view (no hidden left-grid dependency).
- [x] Make resident jump selection work across all pages (not only current page rows).
- [x] Restore a stable left resident list pane (search + paging + click-to-load) with split layout guards.
- [x] Rename/remove legacy controls (`button1/button2/button3`) and replace with semantic command names.
- [ ] Move resident list data source to a dedicated service/model (stop relying on hidden `dgvResidents` as backing state).
- [ ] Add per-module action bars with overflow handling to prevent clipped/hidden buttons on small windows.
- [ ] Run a full small-window QA pass (`<= 1280x720`) for Residents, Certificates, and Blotter views.

## Next Recommended Task (Phase 2)

- [ ] Next: **Run end-to-end QA for new Phase 2 features (attachments, reminders, transfers, renewals, hotspot map).**
