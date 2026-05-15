-- Migration: Emergency Contacts directory
-- Date: 2026-05-15

CREATE TABLE IF NOT EXISTS emergency_contact (
    contact_id      INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id     INT NOT NULL DEFAULT 1,
    category        VARCHAR(40) NOT NULL DEFAULT 'OTHER',
    agency_name     VARCHAR(200) NOT NULL,
    contact_person  VARCHAR(150) NULL,
    phone_primary   VARCHAR(50) NOT NULL,
    phone_secondary VARCHAR(50) NULL,
    email           VARCHAR(150) NULL,
    address         VARCHAR(300) NULL,
    notes           TEXT NULL,
    is_priority     TINYINT NOT NULL DEFAULT 0,
    is_active       TINYINT NOT NULL DEFAULT 1,
    created_by_user_id INT NULL,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_ec_category (category),
    INDEX idx_ec_priority (is_priority, is_active)
);

INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'POLICE', 'Philippine National Police (PNP) Emergency', '911', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='POLICE' AND agency_name='Philippine National Police (PNP) Emergency');

INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'FIRE', 'Bureau of Fire Protection (BFP)', '160', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='FIRE' AND agency_name='Bureau of Fire Protection (BFP)');

INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'MEDICAL', 'Red Cross 143 Emergency Hotline', '143', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='MEDICAL' AND agency_name='Red Cross 143 Emergency Hotline');

INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'DISASTER', 'NDRRMC Emergency Operations Center', '(02) 8911-1406', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='DISASTER' AND agency_name='NDRRMC Emergency Operations Center');
