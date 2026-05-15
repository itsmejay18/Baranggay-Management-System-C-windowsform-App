-- Migration: Add extended resident fields for enhanced form
-- Date: 2026-05-15
-- Description: Adds occupation, education, nationality, religion, blood type,
--              email, government IDs, place of birth, address details, and residency date.

ALTER TABLE resident
    ADD COLUMN IF NOT EXISTS occupation VARCHAR(100) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS educational_attainment VARCHAR(50) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS nationality VARCHAR(50) DEFAULT 'Filipino',
    ADD COLUMN IF NOT EXISTS religion VARCHAR(50) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS blood_type VARCHAR(5) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS email_address VARCHAR(150) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS place_of_birth VARCHAR(200) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS house_no VARCHAR(20) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS street VARCHAR(150) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS philhealth_no VARCHAR(30) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS sss_no VARCHAR(30) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS tin_no VARCHAR(30) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS voters_id_no VARCHAR(30) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS date_of_residency DATE DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS household_relationship VARCHAR(30) DEFAULT NULL,
    ADD COLUMN IF NOT EXISTS photo_path VARCHAR(500) DEFAULT NULL;
