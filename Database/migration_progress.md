# Database Migration Progress

Date: 2026-02-11

Goal: Migrate from the current schema documented in `Database/mysqldatabasecopy.txt` to the new `barangay_system` schema provided by the user, and update the app code to match.

Chosen approach: Phased refactor to the new schema, with data migration scripts. No compatibility views unless we hit a blocker. Added light schema extensions to preserve existing UI fields (resident photo blob, user profile fields, document request extras, case_record extensions).

## Pending Decisions
- [x] Confirm if this is a single-barangay deployment or multi-barangay (assumed single barangay).
- [x] Confirm default `barangay_id` and default `purok_id` to use for migrated residents (set to 1).
- [x] Confirm role mapping (current `users.role` enum to `role` + `user_role`).
- [x] Confirm how to map `certificates` to `document_type` entries (seeded known types).
- [x] Confirm how to map `blotter_records` to `case_type` entries and default case status (seeded `General`).

## Phase 0: Prep
- [ ] Back up the existing database.
- [x] Create the new database schema (`barangay_system`) in MySQL.
- [x] Add a migration SQL script to map old tables to new tables.
- [x] Execute data migration script (INSERT IGNORE).
- [x] Add patch scripts for app-compat columns (`20260211_patch_user_account.sql`, `20260211_patch_app_compat.sql`).
- [x] Add schema SQL script for the new database.
- [x] Update local connection string to point at `barangay_system`.
- [x] Add startup schema guard to auto-apply required columns/tables.

## Phase 1: Core Infrastructure
- [x] Add constants or helpers for defaults (`SchemaDefaults`).
- [x] Update `Database/DBConnection.cs` for new database name.
- [x] Audit and update core SQL queries for old table names and old columns.

## Phase 2: Auth + Users
- [x] Update login to query `user_account` + `user_role` + `role`.
- [x] Update register flow to insert into `user_account` and `user_role`.
- [x] Update Users list and UpdateUser to use new table/column names.
- [x] Store staff photos in `user_account.photo_url` (file path).
- [x] Add DB-backed role-permission matrix (`role_permission`) with Admin/Staff defaults.

## Phase 3: Residents + Households
- [x] Update residents CRUD to use `resident` (with default barangay/purok).
- [x] Update resident search, filters, and grids to use new column names.
- [x] Ensure barangay, purok, household relationships are supported in UI.

## Phase 4: Certificates -> Documents
- [x] Create base `document_type` data for existing certificate types.
- [x] Update certificate flows to use `document_request` (with legacy fields kept on the table).
- [x] Update dashboards and counts to the new statuses.
- [x] Expand certificate detail panel (validity, printing, payment info).
- [x] Record certificate payments on issue (default Cash) and surface payment info for legacy rows.
- [x] Add payment method selection when issuing certificates.

## Phase 5: Blotter -> Cases
- [x] Map `blotter_records` to `case_record` (extended columns on `case_record`).
- [x] Update blotter UI queries and insert/update flows to the new schema.

## Phase 6: Cleanup
- [ ] Remove old schema bootstrap code (`Ensure*Schema` methods).
- [ ] Remove obsolete tables from the database.
- [ ] Run through critical workflows to confirm behavior.

## Phase 7: Performance
- [x] Add index migration script for common filters/searches (`20260211_add_indexes.sql`).

## Execution Notes
- Schema script executed on 2026-02-11.
- Patch scripts executed on 2026-02-11.
- Data migration executed on 2026-02-11 (INSERT IGNORE to avoid overwriting existing rows).
- Build validated on 2026-02-11 using `dotnet build -o bin\\Debug\\buildtmp` because the app was running and locking the default output.
- Role-permission matrix migration added on 2026-02-15 (`20260215_role_permission_matrix.sql`) and mirrored in startup schema guard/bootstrap.
