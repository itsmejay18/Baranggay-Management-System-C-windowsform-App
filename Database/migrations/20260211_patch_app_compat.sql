-- Patch schema to include columns used by the app but not present in the base schema.
-- Safe to run multiple times (checks before adding).

SET FOREIGN_KEY_CHECKS = 0;

-- resident: add photo (BLOB) if missing
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'resident' AND COLUMN_NAME = 'photo'),
        'SELECT 1',
        'ALTER TABLE resident ADD COLUMN photo LONGBLOB NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- document_request: add app fields
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND COLUMN_NAME = 'document_no'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN document_no VARCHAR(50) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND COLUMN_NAME = 'fee'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN fee DECIMAL(10,2) DEFAULT 0'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND COLUMN_NAME = 'or_number'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN or_number VARCHAR(100) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND COLUMN_NAME = 'business_name'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN business_name VARCHAR(255) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND COLUMN_NAME = 'business_nature'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN business_nature VARCHAR(255) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND COLUMN_NAME = 'print_count'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN print_count INT NOT NULL DEFAULT 0'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND COLUMN_NAME = 'last_printed_at'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN last_printed_at DATETIME NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND COLUMN_NAME = 'verification_token'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN verification_token VARCHAR(32) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND COLUMN_NAME = 'verification_token_created_at'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN verification_token_created_at DATETIME NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND INDEX_NAME = 'ux_document_request_verification_token'),
        'SELECT 1',
        'CREATE UNIQUE INDEX ux_document_request_verification_token ON document_request(verification_token)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- case_record: add extended fields used by the app
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'complainant_id'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN complainant_id INT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'respondent_resident_id'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN respondent_resident_id INT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'respondent_name'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN respondent_name VARCHAR(255) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'incident_type'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN incident_type VARCHAR(100) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'incident_time'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN incident_time TIME NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'witness_names'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN witness_names TEXT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'action_taken'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN action_taken TEXT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'resolution_details'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN resolution_details TEXT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'referral_destination'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN referral_destination VARCHAR(255) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'closure_notes'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN closure_notes TEXT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'closed_at'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN closed_at DATETIME NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'closed_by_user_id'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN closed_by_user_id INT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'incident_details'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN incident_details TEXT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'recorded_by'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN recorded_by INT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- AI columns
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'ai_summary'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN ai_summary TEXT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'ai_key_points'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN ai_key_points TEXT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'ai_category'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN ai_category VARCHAR(150) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'ai_category_confidence'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN ai_category_confidence DECIMAL(5,4) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'ai_risk_level'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN ai_risk_level VARCHAR(20) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'ai_risk_score'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN ai_risk_score INT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'ai_risk_reasons'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN ai_risk_reasons TEXT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'ai_entities'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN ai_entities TEXT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'ai_recommended_next_action'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN ai_recommended_next_action TEXT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'ai_model'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN ai_model VARCHAR(100) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND COLUMN_NAME = 'ai_processed_at'),
        'SELECT 1',
        'ALTER TABLE case_record ADD COLUMN ai_processed_at DATETIME NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- case_timeline: per-case timeline entries (status/events)
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_timeline'),
        'SELECT 1',
        'CREATE TABLE case_timeline (
            timeline_id INT AUTO_INCREMENT PRIMARY KEY,
            case_id INT NOT NULL,
            event_type VARCHAR(50) NOT NULL,
            event_title VARCHAR(150) NOT NULL,
            event_details TEXT NULL,
            from_status VARCHAR(30) NULL,
            to_status VARCHAR(30) NULL,
            created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            created_by_user_id INT NULL,
            INDEX idx_case_timeline_case (case_id),
            INDEX idx_case_timeline_created_at (created_at),
            FOREIGN KEY (case_id) REFERENCES case_record(case_id) ON DELETE CASCADE,
            FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
        )'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;
