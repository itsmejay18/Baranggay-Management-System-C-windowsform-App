-- Performance indexes for common filters/searches.
-- Safe to run multiple times (checks before adding).

SET FOREIGN_KEY_CHECKS = 0;

-- resident name search
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'resident' AND INDEX_NAME = 'idx_resident_name'),
        'SELECT 1',
        'CREATE INDEX idx_resident_name ON resident (last_name, first_name, middle_name)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- resident filters
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'resident' AND INDEX_NAME = 'idx_resident_barangay'),
        'SELECT 1',
        'CREATE INDEX idx_resident_barangay ON resident (barangay_id)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'resident' AND INDEX_NAME = 'idx_resident_purok'),
        'SELECT 1',
        'CREATE INDEX idx_resident_purok ON resident (purok_id)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'resident' AND INDEX_NAME = 'idx_resident_status'),
        'SELECT 1',
        'CREATE INDEX idx_resident_status ON resident (status)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- household filters
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'household' AND INDEX_NAME = 'idx_household_barangay'),
        'SELECT 1',
        'CREATE INDEX idx_household_barangay ON household (barangay_id)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'household' AND INDEX_NAME = 'idx_household_purok'),
        'SELECT 1',
        'CREATE INDEX idx_household_purok ON household (purok_id)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- purok/sitio lookup
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'purok_sitio' AND INDEX_NAME = 'idx_purok_barangay'),
        'SELECT 1',
        'CREATE INDEX idx_purok_barangay ON purok_sitio (barangay_id)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- document_request filters
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND INDEX_NAME = 'idx_doc_request_resident'),
        'SELECT 1',
        'CREATE INDEX idx_doc_request_resident ON document_request (resident_id)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND INDEX_NAME = 'idx_doc_request_status'),
        'SELECT 1',
        'CREATE INDEX idx_doc_request_status ON document_request (status)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND INDEX_NAME = 'idx_doc_request_requested_at'),
        'SELECT 1',
        'CREATE INDEX idx_doc_request_requested_at ON document_request (requested_at)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_request' AND INDEX_NAME = 'idx_doc_request_resident_status'),
        'SELECT 1',
        'CREATE INDEX idx_doc_request_resident_status ON document_request (resident_id, status)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- document_payment
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_payment' AND INDEX_NAME = 'idx_doc_payment_request'),
        'SELECT 1',
        'CREATE INDEX idx_doc_payment_request ON document_payment (doc_request_id)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'document_payment' AND INDEX_NAME = 'idx_doc_payment_paid_at'),
        'SELECT 1',
        'CREATE INDEX idx_doc_payment_paid_at ON document_payment (paid_at)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- case_record filters
SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND INDEX_NAME = 'idx_case_record_barangay'),
        'SELECT 1',
        'CREATE INDEX idx_case_record_barangay ON case_record (barangay_id)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND INDEX_NAME = 'idx_case_record_status'),
        'SELECT 1',
        'CREATE INDEX idx_case_record_status ON case_record (status)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := (
    SELECT IF(
        EXISTS(SELECT 1 FROM INFORMATION_SCHEMA.STATISTICS
               WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'case_record' AND INDEX_NAME = 'idx_case_record_incident_date'),
        'SELECT 1',
        'CREATE INDEX idx_case_record_incident_date ON case_record (incident_date)'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET FOREIGN_KEY_CHECKS = 1;
