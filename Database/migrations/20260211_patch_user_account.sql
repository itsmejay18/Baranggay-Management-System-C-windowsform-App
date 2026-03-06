-- Patch existing user_account table to include columns used by the app.
-- Safe to run multiple times (checks before adding).

SET FOREIGN_KEY_CHECKS = 0;

SET @tbl := 'user_account';

-- first_name
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tbl AND COLUMN_NAME = 'first_name'),
        'SELECT 1',
        'ALTER TABLE user_account ADD COLUMN first_name VARCHAR(100) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- middle_name
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tbl AND COLUMN_NAME = 'middle_name'),
        'SELECT 1',
        'ALTER TABLE user_account ADD COLUMN middle_name VARCHAR(100) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- last_name
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tbl AND COLUMN_NAME = 'last_name'),
        'SELECT 1',
        'ALTER TABLE user_account ADD COLUMN last_name VARCHAR(100) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- position
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tbl AND COLUMN_NAME = 'position'),
        'SELECT 1',
        'ALTER TABLE user_account ADD COLUMN position VARCHAR(100) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- department
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tbl AND COLUMN_NAME = 'department'),
        'SELECT 1',
        'ALTER TABLE user_account ADD COLUMN department VARCHAR(100) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- last_project
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tbl AND COLUMN_NAME = 'last_project'),
        'SELECT 1',
        'ALTER TABLE user_account ADD COLUMN last_project VARCHAR(255) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- photo_url
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @tbl AND COLUMN_NAME = 'photo_url'),
        'SELECT 1',
        'ALTER TABLE user_account ADD COLUMN photo_url VARCHAR(255) NULL'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;
