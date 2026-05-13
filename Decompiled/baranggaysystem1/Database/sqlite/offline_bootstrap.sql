-- Barangay Management System - SQLite Bootstrap Schema
-- This file creates the core database schema for offline/SQLite mode.

CREATE TABLE IF NOT EXISTS barangay (
    barangay_id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS purok_sitio (
    purok_id INTEGER PRIMARY KEY,
    barangay_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    type TEXT NOT NULL DEFAULT 'PUROK',
    is_active INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_purok_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS role (
    role_id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    description TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS user_account (
    user_id INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id INTEGER NOT NULL DEFAULT 1,
    username TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    full_name TEXT,
    first_name TEXT,
    middle_name TEXT,
    last_name TEXT,
    email TEXT,
    contact_no TEXT,
    position TEXT,
    department TEXT,
    photo_url TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    last_login_at TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_user_account_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS user_role (
    user_role_id INTEGER PRIMARY KEY AUTOINCREMENT,
    user_id INTEGER NOT NULL,
    role_id INTEGER NOT NULL,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_user_role_user FOREIGN KEY (user_id) REFERENCES user_account (user_id) ON DELETE CASCADE,
    CONSTRAINT fk_user_role_role FOREIGN KEY (role_id) REFERENCES role (role_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS role_permission (
    role_permission_id INTEGER PRIMARY KEY AUTOINCREMENT,
    role_id INTEGER NOT NULL,
    permission_key TEXT NOT NULL,
    is_allowed INTEGER NOT NULL DEFAULT 0,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_role_permission_role FOREIGN KEY (role_id) REFERENCES role (role_id) ON DELETE CASCADE,
    CONSTRAINT ux_role_permission UNIQUE (role_id, permission_key)
);


CREATE TABLE IF NOT EXISTS household (
    household_id INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id INTEGER NOT NULL,
    purok_id INTEGER NOT NULL,
    house_no TEXT,
    street TEXT,
    subdivision TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_household_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT fk_household_purok FOREIGN KEY (purok_id) REFERENCES purok_sitio (purok_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS resident (
    resident_id INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id INTEGER NOT NULL,
    purok_id INTEGER NOT NULL DEFAULT 1,
    household_id INTEGER,
    first_name TEXT NOT NULL,
    middle_name TEXT,
    last_name TEXT NOT NULL,
    sex TEXT NOT NULL DEFAULT 'M',
    birth_date TEXT,
    civil_status TEXT,
    contact_no TEXT,
    status TEXT NOT NULL DEFAULT 'ACTIVE',
    photo BLOB,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    is_senior INTEGER NOT NULL DEFAULT 0,
    is_pwd INTEGER NOT NULL DEFAULT 0,
    is_4ps_beneficiary INTEGER NOT NULL DEFAULT 0,
    is_registered_voter INTEGER NOT NULL DEFAULT 0,
    is_head_of_family INTEGER NOT NULL DEFAULT 0,
    is_solo_parent INTEGER NOT NULL DEFAULT 0,
    is_youth INTEGER NOT NULL DEFAULT 0,
    is_indigent INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_resident_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT fk_resident_purok FOREIGN KEY (purok_id) REFERENCES purok_sitio (purok_id) ON DELETE CASCADE,
    CONSTRAINT fk_resident_household FOREIGN KEY (household_id) REFERENCES household (household_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS document_type (
    document_type_id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    code TEXT,
    requires_approval INTEGER NOT NULL DEFAULT 1,
    validity_days INTEGER DEFAULT 365,
    renewal_reminder_days INTEGER DEFAULT 30,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS certificate (
    certificate_id INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id INTEGER NOT NULL,
    resident_id INTEGER NOT NULL,
    document_type_id INTEGER NOT NULL,
    or_number TEXT,
    purpose TEXT,
    status TEXT NOT NULL DEFAULT 'PENDING',
    issued_at TEXT,
    expires_at TEXT,
    created_by_user_id INTEGER,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_certificate_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT fk_certificate_resident FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE CASCADE,
    CONSTRAINT fk_certificate_doctype FOREIGN KEY (document_type_id) REFERENCES document_type (document_type_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS case_type (
    case_type_id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);

CREATE TABLE IF NOT EXISTS case_record (
    case_id INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id INTEGER NOT NULL,
    case_type_id INTEGER,
    complainant_id INTEGER,
    respondent_id INTEGER,
    case_number TEXT,
    incident_date TEXT,
    incident_location TEXT,
    narrative TEXT,
    status TEXT NOT NULL DEFAULT 'OPEN',
    resolution TEXT,
    resolved_at TEXT,
    created_by_user_id INTEGER,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_case_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT fk_case_type FOREIGN KEY (case_type_id) REFERENCES case_type (case_type_id) ON DELETE SET NULL,
    CONSTRAINT fk_case_complainant FOREIGN KEY (complainant_id) REFERENCES resident (resident_id) ON DELETE SET NULL,
    CONSTRAINT fk_case_respondent FOREIGN KEY (respondent_id) REFERENCES resident (resident_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS resident_transfer_history (
    transfer_id INTEGER PRIMARY KEY AUTOINCREMENT,
    resident_id INTEGER NOT NULL,
    old_purok_id INTEGER,
    old_household_id INTEGER,
    old_address TEXT,
    new_purok_id INTEGER,
    new_household_id INTEGER,
    new_address TEXT,
    transfer_reason TEXT,
    transferred_by_user_id INTEGER,
    transferred_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_transfer_resident FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS audit_trail (
    audit_id INTEGER PRIMARY KEY AUTOINCREMENT,
    module TEXT,
    entity_type TEXT,
    entity_id INTEGER,
    action TEXT,
    before_data TEXT,
    after_data TEXT,
    remarks TEXT,
    user_id INTEGER,
    username TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);

CREATE TABLE IF NOT EXISTS projects (
    project_id INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id INTEGER NOT NULL,
    title TEXT NOT NULL,
    description TEXT,
    category TEXT,
    status TEXT NOT NULL DEFAULT 'PLANNED',
    start_date TEXT,
    end_date TEXT,
    budget REAL NOT NULL DEFAULT 0.00,
    record_type TEXT NOT NULL DEFAULT 'Project',
    attendance_target INTEGER NOT NULL DEFAULT 0,
    attendance_count INTEGER NOT NULL DEFAULT 0,
    last_activity_date TEXT,
    outcome_status TEXT NOT NULL DEFAULT 'Pending',
    outcome_summary TEXT,
    created_by_user_id INTEGER,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_projects_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS collection_entry (
    collection_id INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id INTEGER NOT NULL,
    resident_id INTEGER,
    collection_type TEXT NOT NULL,
    amount REAL NOT NULL DEFAULT 0.00,
    or_number TEXT,
    payment_date TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    remarks TEXT,
    created_by_user_id INTEGER,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_collection_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT fk_collection_resident FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS announcement (
    announcement_id INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id INTEGER NOT NULL,
    title TEXT NOT NULL,
    content TEXT,
    category TEXT,
    priority TEXT NOT NULL DEFAULT 'NORMAL',
    is_published INTEGER NOT NULL DEFAULT 0,
    published_at TEXT,
    expires_at TEXT,
    created_by_user_id INTEGER,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_announcement_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS system_config (
    config_id INTEGER PRIMARY KEY AUTOINCREMENT,
    config_key TEXT NOT NULL UNIQUE,
    config_value TEXT,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS schema_migrations (
    migration_name TEXT NOT NULL PRIMARY KEY,
    applied_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS offline_sync_queue (
    queue_id INTEGER PRIMARY KEY AUTOINCREMENT,
    sql_text TEXT NOT NULL,
    parameters_json TEXT,
    queued_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'pending'
);

CREATE TABLE IF NOT EXISTS barangay_official (
    official_id INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id INTEGER NOT NULL,
    resident_id INTEGER,
    full_name TEXT NOT NULL,
    position TEXT NOT NULL,
    term_start TEXT,
    term_end TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    sort_order INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    CONSTRAINT fk_official_barangay FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS notification_outbox (
    notification_id INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id INTEGER NOT NULL,
    recipient_type TEXT NOT NULL DEFAULT 'RESIDENT',
    recipient_id INTEGER,
    channel TEXT NOT NULL DEFAULT 'SMS',
    subject TEXT,
    body TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'QUEUED',
    sent_at TEXT,
    error_message TEXT,
    created_by_user_id INTEGER,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_resident_barangay ON resident (barangay_id);
CREATE INDEX IF NOT EXISTS idx_resident_household ON resident (household_id);
CREATE INDEX IF NOT EXISTS idx_resident_purok ON resident (purok_id);
CREATE INDEX IF NOT EXISTS idx_resident_status ON resident (status);
CREATE INDEX IF NOT EXISTS idx_resident_name ON resident (last_name, first_name);
CREATE INDEX IF NOT EXISTS idx_household_barangay ON household (barangay_id);
CREATE INDEX IF NOT EXISTS idx_household_purok ON household (purok_id);
CREATE INDEX IF NOT EXISTS idx_certificate_resident ON certificate (resident_id);
CREATE INDEX IF NOT EXISTS idx_certificate_status ON certificate (status);
CREATE INDEX IF NOT EXISTS idx_case_record_status ON case_record (status);
CREATE INDEX IF NOT EXISTS idx_user_account_username ON user_account (username);
CREATE INDEX IF NOT EXISTS idx_audit_trail_module ON audit_trail (module, created_at);

-- Default data
INSERT OR IGNORE INTO barangay (barangay_id, name) VALUES (1, 'Default Barangay');
INSERT OR IGNORE INTO purok_sitio (purok_id, barangay_id, name, type) VALUES (1, 1, 'Default Purok', 'PUROK');
INSERT OR IGNORE INTO role (role_id, name, description) VALUES (1, 'Super Admin', 'Primary system owner');
INSERT OR IGNORE INTO role (role_id, name, description) VALUES (2, 'Admin', 'System administrator');
INSERT OR IGNORE INTO role (role_id, name, description) VALUES (3, 'Staff', 'Staff account');
INSERT OR IGNORE INTO case_type (name) VALUES ('General');
INSERT OR IGNORE INTO document_type (name, code, requires_approval) VALUES ('Barangay Clearance', 'BC', 1);
INSERT OR IGNORE INTO document_type (name, code, requires_approval) VALUES ('Certificate of Residency', 'CR', 1);
INSERT OR IGNORE INTO document_type (name, code, requires_approval) VALUES ('Indigency', 'IND', 1);
INSERT OR IGNORE INTO document_type (name, code, requires_approval) VALUES ('Business Clearance', 'BUS', 1);
