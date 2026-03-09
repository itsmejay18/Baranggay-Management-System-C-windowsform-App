CREATE TABLE IF NOT EXISTS backup_run (
    backup_run_id INT AUTO_INCREMENT PRIMARY KEY,
    started_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ended_at DATETIME NULL,
    status ENUM('RUNNING','SUCCESS','FAILED') NOT NULL DEFAULT 'RUNNING',
    backup_type ENUM('FULL','INCREMENTAL','DIFFERENTIAL') NOT NULL DEFAULT 'FULL',
    base_started_at DATETIME NULL,
    base_backup_run_id INT NULL,
    file_path VARCHAR(500) NULL,
    file_size_bytes BIGINT NULL,
    error_message TEXT NULL,
    created_by_user_id INT NULL,
    INDEX idx_backup_run_started_at (started_at),
    INDEX idx_backup_run_status (status),
    INDEX idx_backup_run_type_started_at (backup_type, started_at),
    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
);

