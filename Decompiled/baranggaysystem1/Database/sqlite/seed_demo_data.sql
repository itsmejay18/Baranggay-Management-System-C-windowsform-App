-- Barangay Management System - SQLite Demo Seed Data
-- Marks seed migrations as applied so the system doesn't re-run them.

INSERT OR IGNORE INTO schema_migrations (migration_name) VALUES ('20260309_seed_30_records_30_transactions_reports.sql');
INSERT OR IGNORE INTO schema_migrations (migration_name) VALUES ('20260428_ph_public_reference_seed.sql');

-- Create ph_psgc_area table for Philippine geographic reference
CREATE TABLE IF NOT EXISTS ph_psgc_area (
    psgc_code TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    geographic_level TEXT NOT NULL,
    parent_code TEXT,
    region_code TEXT,
    province_code TEXT,
    municipality_code TEXT
);
