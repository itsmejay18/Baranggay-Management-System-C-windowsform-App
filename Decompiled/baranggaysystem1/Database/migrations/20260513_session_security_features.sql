-- Migration: Session security, password reset, and sync tracking features
-- Date: 2026-05-13

-- Add must_change_password flag to user_account
ALTER TABLE user_account ADD COLUMN IF NOT EXISTS must_change_password TINYINT NOT NULL DEFAULT 0;

-- Password reset tracking table
CREATE TABLE IF NOT EXISTS password_reset (
    reset_id        INT AUTO_INCREMENT PRIMARY KEY,
    user_id         INT NOT NULL,
    reset_token     VARCHAR(128) NOT NULL,
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    expires_at      DATETIME NOT NULL,
    used_at         DATETIME NULL,
    is_used         TINYINT NOT NULL DEFAULT 0,
    initiated_by    INT NULL,
    reset_method    VARCHAR(30) NOT NULL DEFAULT 'ADMIN',
    INDEX idx_reset_token (reset_token),
    INDEX idx_reset_user (user_id)
);

-- Security questions table for self-service password reset
CREATE TABLE IF NOT EXISTS security_question (
    sq_id           INT AUTO_INCREMENT PRIMARY KEY,
    user_id         INT NOT NULL UNIQUE,
    question_1      VARCHAR(255) NOT NULL,
    answer_1_hash   VARCHAR(255) NOT NULL,
    question_2      VARCHAR(255) NULL,
    answer_2_hash   VARCHAR(255) NULL,
    updated_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_sq_user (user_id)
);

-- Offline sync queue table (for tracking pending changes)
CREATE TABLE IF NOT EXISTS offline_sync_queue (
    sync_id         BIGINT AUTO_INCREMENT PRIMARY KEY,
    module          VARCHAR(60) NULL,
    sql_statement   LONGTEXT NOT NULL,
    parameters_json LONGTEXT NULL,
    sync_status     VARCHAR(20) NOT NULL DEFAULT 'pending',
    created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    synced_at       DATETIME NULL,
    error_message   TEXT NULL,
    INDEX idx_sync_status (sync_status),
    INDEX idx_sync_created (created_at)
);

-- Session timeout configuration (stored in system_config, no separate table needed)
-- These are managed via SystemConfigService:
--   session_timeout_minutes (default: 15)
--   session_timeout_enabled (default: true)
--   session_warning_minutes (default: 2)

-- Notification settings (stored in system_config, no separate table needed)
-- These are managed via NotificationSettingsService:
--   smtp_host, smtp_port, smtp_from_email, smtp_from_name
--   smtp_username, smtp_password, smtp_use_ssl, smtp_enabled
--   sms_api_url, sms_api_token, sms_sender_name, sms_enabled
--   renewal_reminder_enabled, renewal_reminder_days
--   blotter_reminder_enabled, blotter_reminder_age_days
