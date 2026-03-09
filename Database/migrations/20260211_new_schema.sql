-- Use the currently connected database from DBConnection.
-- Do not hardcode CREATE DATABASE / USE here so installer-configured DB names work.

SET FOREIGN_KEY_CHECKS = 0;

-- =========================================
-- 1. CORE MASTER DATA
-- =========================================

CREATE TABLE IF NOT EXISTS barangay (
    barangay_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    city_municipality VARCHAR(150),
    province VARCHAR(150),
    region VARCHAR(150),
    address_line VARCHAR(255),
    contact_no VARCHAR(50),
    email VARCHAR(150),
    logo_url VARCHAR(255),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS purok_sitio (
    purok_id INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    name VARCHAR(150) NOT NULL,
    type ENUM('PUROK','SITIO') DEFAULT 'PUROK',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE
);

-- =========================================
-- 2. RESIDENTS & HOUSEHOLDS
-- =========================================

CREATE TABLE IF NOT EXISTS household (
    household_id INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    purok_id INT NOT NULL,
    house_no VARCHAR(50),
    street VARCHAR(150),
    subdivision VARCHAR(150),
    address_note VARCHAR(255),
    latitude DECIMAL(10,8),
    longitude DECIMAL(11,8),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,
    FOREIGN KEY (purok_id) REFERENCES purok_sitio(purok_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS resident (
    resident_id INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    household_id INT NULL,
    purok_id INT NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    suffix VARCHAR(20),
    sex ENUM('M','F') NOT NULL,
    birth_date DATE NOT NULL,
    birth_place VARCHAR(150),
    civil_status ENUM('Single','Married','Widowed','Separated'),
    citizenship VARCHAR(100) DEFAULT 'Filipino',
    religion VARCHAR(100),
    contact_no VARCHAR(50),
    email VARCHAR(150),
    occupation VARCHAR(150),
    employer VARCHAR(150),
    education_level VARCHAR(100),
    is_pwd BOOLEAN DEFAULT FALSE,
    pwd_id_no VARCHAR(100),
    is_senior BOOLEAN DEFAULT FALSE,
    is_4ps_beneficiary BOOLEAN DEFAULT FALSE,
    is_registered_voter BOOLEAN DEFAULT FALSE,
    voter_precinct_no VARCHAR(50),
    status ENUM('ACTIVE','DECEASED','MOVED_OUT') DEFAULT 'ACTIVE',
    date_registered DATE DEFAULT (CURRENT_DATE),
    photo_url VARCHAR(255),
    photo LONGBLOB NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,
    FOREIGN KEY (household_id) REFERENCES household(household_id) ON DELETE SET NULL,
    FOREIGN KEY (purok_id) REFERENCES purok_sitio(purok_id) ON DELETE CASCADE
);

-- =========================================
-- 3. USERS, ROLES, OFFICIALS
-- =========================================

CREATE TABLE IF NOT EXISTS role (
    role_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE,
    description VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS user_account (
    user_id INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    username VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    resident_id INT NULL,
    full_name VARCHAR(150),
    first_name VARCHAR(100),
    middle_name VARCHAR(100),
    last_name VARCHAR(100),
    contact_no VARCHAR(50),
    email VARCHAR(150),
    position VARCHAR(100),
    department VARCHAR(100),
    last_project VARCHAR(255),
    photo_url VARCHAR(255),
    is_active BOOLEAN DEFAULT TRUE,
    last_login_at DATETIME,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,
    FOREIGN KEY (resident_id) REFERENCES resident(resident_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS user_role (
    user_role_id INT AUTO_INCREMENT PRIMARY KEY,
    user_id INT NOT NULL,
    role_id INT NOT NULL,
    FOREIGN KEY (user_id) REFERENCES user_account(user_id) ON DELETE CASCADE,
    FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS official_term (
    term_id INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    term_start DATE,
    term_end DATE,
    notes VARCHAR(255),
    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS barangay_official (
    official_id INT AUTO_INCREMENT PRIMARY KEY,
    term_id INT NOT NULL,
    resident_id INT NOT NULL,
    position VARCHAR(150),
    committee VARCHAR(150),
    status ENUM('ACTIVE','INACTIVE') DEFAULT 'ACTIVE',
    FOREIGN KEY (term_id) REFERENCES official_term(term_id) ON DELETE CASCADE,
    FOREIGN KEY (resident_id) REFERENCES resident(resident_id)
);

-- =========================================
-- 4. DOCUMENTS & CERTIFICATES
-- =========================================

CREATE TABLE IF NOT EXISTS document_type (
    doc_type_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(150) NOT NULL,
    code VARCHAR(50) UNIQUE,
    template_path VARCHAR(255),
    template_html TEXT,
    fee_default DECIMAL(10,2) DEFAULT 0,
    validity_days INT,
    requires_approval BOOLEAN DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS document_request (
    doc_request_id INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    doc_type_id INT NOT NULL,
    resident_id INT NOT NULL,
    purpose VARCHAR(255),
    status ENUM('DRAFT','SUBMITTED','APPROVED','RELEASED','REJECTED','CANCELLED') DEFAULT 'SUBMITTED',
    requested_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    approved_at DATETIME,
    released_at DATETIME,
    requested_by_user_id INT,
    approved_by_user_id INT,
    released_by_user_id INT,
    remarks TEXT,
    document_no VARCHAR(50),
    fee DECIMAL(10,2) DEFAULT 0,
    or_number VARCHAR(100),
    business_name VARCHAR(255),
    business_nature VARCHAR(255),
    print_count INT NOT NULL DEFAULT 0,
    last_printed_at DATETIME,
    verification_token VARCHAR(32),
    verification_token_created_at DATETIME,
    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,
    FOREIGN KEY (doc_type_id) REFERENCES document_type(doc_type_id),
    FOREIGN KEY (resident_id) REFERENCES resident(resident_id),
    FOREIGN KEY (requested_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL,
    FOREIGN KEY (approved_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL,
    FOREIGN KEY (released_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
);

SET @sql := (
    SELECT IF(
        EXISTS(
            SELECT 1
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'document_request'
              AND COLUMN_NAME = 'verification_token'
        )
        AND NOT EXISTS(
            SELECT 1
            FROM INFORMATION_SCHEMA.STATISTICS
            WHERE TABLE_SCHEMA = DATABASE()
              AND TABLE_NAME = 'document_request'
              AND INDEX_NAME = 'ux_document_request_verification_token'
        ),
        'CREATE UNIQUE INDEX ux_document_request_verification_token ON document_request(verification_token)',
        'SELECT 1'
    )
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

CREATE TABLE IF NOT EXISTS document_payment (
    payment_id INT AUTO_INCREMENT PRIMARY KEY,
    doc_request_id INT NOT NULL,
    amount DECIMAL(10,2),
    or_no VARCHAR(100),
    payment_method ENUM('Cash','GCash','Bank'),
    paid_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    received_by_user_id INT,
    FOREIGN KEY (doc_request_id) REFERENCES document_request(doc_request_id) ON DELETE CASCADE,
    FOREIGN KEY (received_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS document_release_log (
    release_log_id INT AUTO_INCREMENT PRIMARY KEY,
    doc_request_id INT NOT NULL,
    action ENUM('PRINTED','RELEASED','REPRINTED'),
    user_id INT,
    timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    notes VARCHAR(255),
    FOREIGN KEY (doc_request_id) REFERENCES document_request(doc_request_id) ON DELETE CASCADE,
    FOREIGN KEY (user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
);

-- =========================================
-- 5. CASE / BLOTTER
-- =========================================

CREATE TABLE IF NOT EXISTS case_type (
    case_type_id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(150) NOT NULL UNIQUE
);

CREATE TABLE IF NOT EXISTS case_record (
    case_id INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id INT NOT NULL,
    case_type_id INT NOT NULL,
    case_no VARCHAR(50),
    date_filed DATE,
    incident_date DATE,
    incident_location VARCHAR(255),
    summary TEXT,
    status ENUM('OPEN','ONGOING','SETTLED','REFERRED','CLOSED') DEFAULT 'OPEN',
    handled_by_user_id INT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    complainant_id INT NULL,
    respondent_resident_id INT NULL,
    respondent_name VARCHAR(255),
    incident_type VARCHAR(100),
    incident_time TIME NULL,
    witness_names TEXT,
    action_taken TEXT,
    resolution_details TEXT,
    referral_destination VARCHAR(255),
    closure_notes TEXT,
    closed_at DATETIME,
    closed_by_user_id INT,
    incident_details TEXT,
    recorded_by INT NULL,
    ai_summary TEXT,
    ai_key_points TEXT,
    ai_category VARCHAR(150),
    ai_category_confidence DECIMAL(5,4),
    ai_risk_level VARCHAR(20),
    ai_risk_score INT,
    ai_risk_reasons TEXT,
    ai_entities TEXT,
    ai_recommended_next_action TEXT,
    ai_model VARCHAR(100),
    ai_processed_at DATETIME,
    FOREIGN KEY (barangay_id) REFERENCES barangay(barangay_id) ON DELETE CASCADE,
    FOREIGN KEY (case_type_id) REFERENCES case_type(case_type_id),
    FOREIGN KEY (handled_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS case_timeline (
    timeline_id INT AUTO_INCREMENT PRIMARY KEY,
    case_id INT NOT NULL,
    event_type VARCHAR(50) NOT NULL,
    event_title VARCHAR(150) NOT NULL,
    event_details TEXT,
    from_status VARCHAR(30),
    to_status VARCHAR(30),
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INT,
    INDEX idx_case_timeline_case (case_id),
    INDEX idx_case_timeline_created_at (created_at),
    FOREIGN KEY (case_id) REFERENCES case_record(case_id) ON DELETE CASCADE,
    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS case_party (
    case_party_id INT AUTO_INCREMENT PRIMARY KEY,
    case_id INT NOT NULL,
    resident_id INT,
    full_name VARCHAR(150),
    address VARCHAR(255),
    contact_no VARCHAR(50),
    party_role ENUM('COMPLAINANT','RESPONDENT','WITNESS'),
    FOREIGN KEY (case_id) REFERENCES case_record(case_id) ON DELETE CASCADE,
    FOREIGN KEY (resident_id) REFERENCES resident(resident_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS case_hearing (
    hearing_id INT AUTO_INCREMENT PRIMARY KEY,
    case_id INT NOT NULL,
    schedule_at DATETIME,
    venue VARCHAR(150),
    status ENUM('SCHEDULED','DONE','RESET','CANCELLED') DEFAULT 'SCHEDULED',
    minutes TEXT,
    result TEXT,
    created_by_user_id INT,
    FOREIGN KEY (case_id) REFERENCES case_record(case_id) ON DELETE CASCADE,
    FOREIGN KEY (created_by_user_id) REFERENCES user_account(user_id)
);

CREATE TABLE IF NOT EXISTS case_resolution (
    resolution_id INT AUTO_INCREMENT PRIMARY KEY,
    case_id INT NOT NULL,
    resolution_date DATE,
    outcome VARCHAR(150),
    details TEXT,
    signed_by_official_id INT,
    FOREIGN KEY (case_id) REFERENCES case_record(case_id) ON DELETE CASCADE,
    FOREIGN KEY (signed_by_official_id) REFERENCES barangay_official(official_id)
);

CREATE TABLE IF NOT EXISTS case_attachment (
    attachment_id INT AUTO_INCREMENT PRIMARY KEY,
    case_id INT NOT NULL,
    file_url VARCHAR(255),
    file_name VARCHAR(150),
    uploaded_by_user_id INT,
    uploaded_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (case_id) REFERENCES case_record(case_id) ON DELETE CASCADE,
    FOREIGN KEY (uploaded_by_user_id) REFERENCES user_account(user_id)
);

-- =========================================
-- Legacy support tables used by the app
-- =========================================

CREATE TABLE IF NOT EXISTS certificate_audit (
    audit_id INT AUTO_INCREMENT PRIMARY KEY,
    certificate_id INT NOT NULL,
    action VARCHAR(50) NOT NULL,
    action_by INT NULL,
    action_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    notes VARCHAR(255) NULL,
    INDEX idx_audit_cert (certificate_id)
);

CREATE TABLE IF NOT EXISTS activity_log (
    log_id INT AUTO_INCREMENT PRIMARY KEY,
    resident_id INT NOT NULL,
    module VARCHAR(40) NOT NULL,
    action VARCHAR(50) NOT NULL,
    details VARCHAR(255) NULL,
    action_by INT NULL,
    action_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_activity_resident (resident_id),
    INDEX idx_activity_module (module)
);

-- =========================================
-- Seed defaults
-- =========================================

INSERT INTO barangay (barangay_id, name)
VALUES (1, 'Default Barangay')
ON DUPLICATE KEY UPDATE name = VALUES(name);

INSERT INTO purok_sitio (purok_id, barangay_id, name, type)
VALUES (1, 1, 'Default Purok', 'PUROK')
ON DUPLICATE KEY UPDATE name = VALUES(name);

INSERT INTO role (name, description)
VALUES ('Admin', 'System administrator'),
       ('Staff', 'Staff account')
ON DUPLICATE KEY UPDATE description = VALUES(description);

INSERT INTO document_type (name, code, requires_approval)
VALUES ('Barangay Clearance', 'BC', 1),
       ('Certificate of Residency', 'CR', 1),
       ('Indigency', 'IND', 1),
       ('Business Clearance', 'BUS', 1)
ON DUPLICATE KEY UPDATE code = VALUES(code);

INSERT INTO case_type (name)
VALUES ('General')
ON DUPLICATE KEY UPDATE name = VALUES(name);

SET FOREIGN_KEY_CHECKS = 1;
