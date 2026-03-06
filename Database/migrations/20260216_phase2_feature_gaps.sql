-- Phase 2 feature gaps:
-- 1) Attachments for residents/cases/certificates
-- 2) Outbound SMS/email notification queue
-- 3) Household transfer history timeline
-- 4) Clearance renewal tracking
-- 5) Hotspot map support (purok coordinates)

SET FOREIGN_KEY_CHECKS = 0;

CREATE TABLE IF NOT EXISTS record_attachment (
    attachment_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    entity_type ENUM('RESIDENT','CASE','CERTIFICATE') NOT NULL,
    entity_id INT NOT NULL,
    file_name VARCHAR(255) NOT NULL,
    file_ext VARCHAR(20) NULL,
    mime_type VARCHAR(120) NULL,
    file_size_bytes BIGINT NOT NULL DEFAULT 0,
    file_hash CHAR(64) NULL,
    file_blob LONGBLOB NOT NULL,
    notes VARCHAR(255) NULL,
    uploaded_by_user_id INT NULL,
    uploaded_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_attachment_entity (entity_type, entity_id, uploaded_at),
    INDEX idx_attachment_hash (file_hash),
    CONSTRAINT fk_record_attachment_uploaded_by
        FOREIGN KEY (uploaded_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS outbound_notification (
    notification_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    dedupe_key VARCHAR(160) NULL,
    channel ENUM('SMS','EMAIL') NOT NULL,
    recipient VARCHAR(200) NOT NULL,
    subject VARCHAR(180) NULL,
    message TEXT NOT NULL,
    status ENUM('PENDING','SENT','FAILED','SKIPPED') NOT NULL DEFAULT 'PENDING',
    source_module VARCHAR(40) NULL,
    source_record_id INT NULL,
    template_key VARCHAR(80) NULL,
    scheduled_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sent_at DATETIME NULL,
    attempts INT NOT NULL DEFAULT 0,
    last_error VARCHAR(500) NULL,
    created_by_user_id INT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    UNIQUE KEY ux_outbound_notification_dedupe (dedupe_key),
    INDEX idx_outbound_notification_status (status, scheduled_at),
    INDEX idx_outbound_notification_source (source_module, source_record_id),
    INDEX idx_outbound_notification_channel (channel, status),
    CONSTRAINT fk_outbound_notification_user
        FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS outbound_notification_attempt (
    attempt_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    notification_id BIGINT NOT NULL,
    attempt_no INT NOT NULL,
    attempted_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    success TINYINT(1) NOT NULL DEFAULT 0,
    response_code VARCHAR(64) NULL,
    response_message VARCHAR(500) NULL,
    INDEX idx_notification_attempt_notification (notification_id, attempted_at),
    CONSTRAINT fk_notification_attempt_notification
        FOREIGN KEY (notification_id) REFERENCES outbound_notification(notification_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS resident_transfer_history (
    transfer_id BIGINT AUTO_INCREMENT PRIMARY KEY,
    resident_id INT NOT NULL,
    old_purok_id INT NULL,
    old_household_id INT NULL,
    old_address VARCHAR(255) NULL,
    new_purok_id INT NULL,
    new_household_id INT NULL,
    new_address VARCHAR(255) NULL,
    transfer_reason VARCHAR(255) NULL,
    transferred_by_user_id INT NULL,
    transferred_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_transfer_history_resident (resident_id, transferred_at),
    INDEX idx_transfer_history_old_location (old_purok_id, old_household_id),
    INDEX idx_transfer_history_new_location (new_purok_id, new_household_id),
    CONSTRAINT fk_transfer_history_resident
        FOREIGN KEY (resident_id) REFERENCES resident(resident_id) ON DELETE CASCADE,
    CONSTRAINT fk_transfer_history_old_purok
        FOREIGN KEY (old_purok_id) REFERENCES purok_sitio(purok_id) ON DELETE SET NULL,
    CONSTRAINT fk_transfer_history_new_purok
        FOREIGN KEY (new_purok_id) REFERENCES purok_sitio(purok_id) ON DELETE SET NULL,
    CONSTRAINT fk_transfer_history_old_household
        FOREIGN KEY (old_household_id) REFERENCES household(household_id) ON DELETE SET NULL,
    CONSTRAINT fk_transfer_history_new_household
        FOREIGN KEY (new_household_id) REFERENCES household(household_id) ON DELETE SET NULL,
    CONSTRAINT fk_transfer_history_user
        FOREIGN KEY (transferred_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
);

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1
               FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'purok_sitio'
                 AND COLUMN_NAME = 'latitude'),
        'SELECT 1',
        'ALTER TABLE purok_sitio ADD COLUMN latitude DECIMAL(10,8) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1
               FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'purok_sitio'
                 AND COLUMN_NAME = 'longitude'),
        'SELECT 1',
        'ALTER TABLE purok_sitio ADD COLUMN longitude DECIMAL(11,8) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1
               FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'document_type'
                 AND COLUMN_NAME = 'renewal_reminder_days'),
        'SELECT 1',
        'ALTER TABLE document_type ADD COLUMN renewal_reminder_days INT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1
               FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'document_request'
                 AND COLUMN_NAME = 'expires_at'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN expires_at DATETIME NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1
               FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'document_request'
                 AND COLUMN_NAME = 'renewed_from_request_id'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN renewed_from_request_id INT NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1
               FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'document_request'
                 AND COLUMN_NAME = 'renewal_notified_at'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN renewal_notified_at DATETIME NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1
               FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'document_request'
                 AND COLUMN_NAME = 'release_notified_at'),
        'SELECT 1',
        'ALTER TABLE document_request ADD COLUMN release_notified_at DATETIME NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'document_request'
                 AND INDEX_NAME = 'idx_document_request_expires_at'),
        'SELECT 1',
        'CREATE INDEX idx_document_request_expires_at ON document_request(expires_at)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'document_request'
                 AND INDEX_NAME = 'idx_document_request_renewed_from'),
        'SELECT 1',
        'CREATE INDEX idx_document_request_renewed_from ON document_request(renewed_from_request_id)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'case_record'
                 AND INDEX_NAME = 'idx_case_record_date_status'),
        'SELECT 1',
        'CREATE INDEX idx_case_record_date_status ON case_record(date_filed, status, complainant_id)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'purok_sitio'
                 AND INDEX_NAME = 'idx_purok_coordinates'),
        'SELECT 1',
        'CREATE INDEX idx_purok_coordinates ON purok_sitio(latitude, longitude)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Default renewal settings for Barangay Clearance.
UPDATE document_type
SET validity_days = COALESCE(validity_days, 365),
    renewal_reminder_days = COALESCE(renewal_reminder_days, 30)
WHERE UPPER(code) = 'BC' OR UPPER(name) = 'BARANGAY CLEARANCE';

-- Add new permissions for existing role rows.
INSERT INTO role_permission (role_id, permission_key, is_allowed)
SELECT
    r.role_id,
    p.permission_key,
    CASE
        WHEN r.name = 'Admin' THEN 1
        WHEN r.name = 'Staff' AND p.permission_key IN (
            'attachments.manage',
            'reports.view_hotspot'
        ) THEN 1
        ELSE 0
    END AS is_allowed
FROM role r
INNER JOIN (
    SELECT 'attachments.manage' AS permission_key
    UNION ALL SELECT 'notifications.dispatch'
    UNION ALL SELECT 'reports.view_hotspot'
) p ON 1 = 1
WHERE r.name IN ('Admin', 'Staff')
ON DUPLICATE KEY UPDATE
    is_allowed = VALUES(is_allowed),
    updated_at = CURRENT_TIMESTAMP;

SET FOREIGN_KEY_CHECKS = 1;
