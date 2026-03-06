-- @manual
-- AI Blotter & Case Assistant migration
-- Run in the target schema (barangay_db)

DELIMITER $$

DROP PROCEDURE IF EXISTS add_column_if_missing $$
CREATE PROCEDURE add_column_if_missing(
    IN p_table_name VARCHAR(64),
    IN p_column_name VARCHAR(64),
    IN p_column_definition TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.columns
        WHERE table_schema = DATABASE()
          AND table_name = p_table_name
          AND column_name = p_column_name
    ) THEN
        SET @sql_stmt = CONCAT('ALTER TABLE `', p_table_name, '` ADD COLUMN ', p_column_definition);
        PREPARE stmt FROM @sql_stmt;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END $$

DROP PROCEDURE IF EXISTS add_index_if_missing $$
CREATE PROCEDURE add_index_if_missing(
    IN p_table_name VARCHAR(64),
    IN p_index_name VARCHAR(64),
    IN p_index_ddl TEXT
)
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM information_schema.statistics
        WHERE table_schema = DATABASE()
          AND table_name = p_table_name
          AND index_name = p_index_name
    ) THEN
        SET @sql_stmt = CONCAT('ALTER TABLE `', p_table_name, '` ADD ', p_index_ddl);
        PREPARE stmt FROM @sql_stmt;
        EXECUTE stmt;
        DEALLOCATE PREPARE stmt;
    END IF;
END $$

DELIMITER ;

CALL add_column_if_missing('blotter_records', 'respondent_resident_id', '`respondent_resident_id` INT NULL AFTER `complainant_id`');
CALL add_column_if_missing('blotter_records', 'ai_summary', '`ai_summary` TEXT NULL');
CALL add_column_if_missing('blotter_records', 'ai_key_points', '`ai_key_points` TEXT NULL');
CALL add_column_if_missing('blotter_records', 'ai_category', '`ai_category` VARCHAR(50) NULL');
CALL add_column_if_missing('blotter_records', 'ai_category_confidence', '`ai_category_confidence` DECIMAL(4,3) NULL');
CALL add_column_if_missing('blotter_records', 'ai_risk_level', '`ai_risk_level` ENUM(''Low'',''Medium'',''High'') NULL');
CALL add_column_if_missing('blotter_records', 'ai_risk_score', '`ai_risk_score` INT NULL');
CALL add_column_if_missing('blotter_records', 'ai_risk_reasons', '`ai_risk_reasons` TEXT NULL');
CALL add_column_if_missing('blotter_records', 'ai_entities', '`ai_entities` TEXT NULL');
CALL add_column_if_missing('blotter_records', 'ai_recommended_next_action', '`ai_recommended_next_action` VARCHAR(255) NULL');
CALL add_column_if_missing('blotter_records', 'ai_model', '`ai_model` VARCHAR(100) NULL');
CALL add_column_if_missing('blotter_records', 'ai_processed_at', '`ai_processed_at` DATETIME NULL');

CALL add_index_if_missing('blotter_records', 'idx_blotter_respondent_resident_id', 'INDEX `idx_blotter_respondent_resident_id` (`respondent_resident_id`)');

DROP PROCEDURE IF EXISTS add_column_if_missing;
DROP PROCEDURE IF EXISTS add_index_if_missing;
