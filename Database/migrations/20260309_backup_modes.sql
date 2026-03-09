SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1
               FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'backup_run'
                 AND COLUMN_NAME = 'backup_type'),
        'SELECT 1',
        "ALTER TABLE backup_run ADD COLUMN backup_type ENUM('FULL','INCREMENTAL','DIFFERENTIAL') NOT NULL DEFAULT 'FULL' AFTER status"
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1
               FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'backup_run'
                 AND COLUMN_NAME = 'base_started_at'),
        'SELECT 1',
        'ALTER TABLE backup_run ADD COLUMN base_started_at DATETIME NULL AFTER backup_type'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1
               FROM INFORMATION_SCHEMA.COLUMNS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'backup_run'
                 AND COLUMN_NAME = 'base_backup_run_id'),
        'SELECT 1',
        'ALTER TABLE backup_run ADD COLUMN base_backup_run_id INT NULL AFTER base_started_at'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1
               FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE()
                 AND TABLE_NAME = 'backup_run'
                 AND INDEX_NAME = 'idx_backup_run_type_started_at'),
        'SELECT 1',
        'CREATE INDEX idx_backup_run_type_started_at ON backup_run(backup_type, started_at)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
