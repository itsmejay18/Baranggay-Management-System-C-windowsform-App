CREATE TABLE IF NOT EXISTS role (
    role_id INTEGER PRIMARY KEY,
    name TEXT NOT NULL,
    description TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);

CREATE TABLE IF NOT EXISTS user_account (
    user_id INTEGER PRIMARY KEY,
    barangay_id INTEGER,
    username TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    first_name TEXT,
    middle_name TEXT,
    last_name TEXT,
    full_name TEXT,
    email TEXT,
    contact_no TEXT,
    position TEXT,
    department TEXT,
    last_project TEXT,
    is_active INTEGER NOT NULL DEFAULT 1,
    photo_url TEXT,
    last_login_at TEXT,
    created_at TEXT,
    updated_at TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);

CREATE TABLE IF NOT EXISTS user_role (
    user_id INTEGER NOT NULL,
    role_id INTEGER NOT NULL,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE IF NOT EXISTS announcements (
    announcement_id INTEGER PRIMARY KEY AUTOINCREMENT,
    title TEXT NOT NULL,
    body TEXT,
    priority TEXT,
    status TEXT,
    is_pinned INTEGER NOT NULL DEFAULT 0,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);

CREATE TABLE IF NOT EXISTS household (
    household_id INTEGER PRIMARY KEY,
    barangay_id INTEGER,
    purok_id INTEGER,
    household_no TEXT,
    address TEXT,
    head_resident_id INTEGER,
    created_at TEXT,
    updated_at TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);

CREATE TABLE IF NOT EXISTS resident (
    resident_id INTEGER PRIMARY KEY,
    household_id INTEGER,
    barangay_id INTEGER,
    purok_id INTEGER,
    first_name TEXT,
    middle_name TEXT,
    last_name TEXT,
    birth_date TEXT,
    sex TEXT,
    civil_status TEXT,
    contact_no TEXT,
    status TEXT,
    photo BLOB,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    created_at TEXT,
    updated_at TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);

CREATE TABLE IF NOT EXISTS case_record (
    case_id INTEGER PRIMARY KEY,
    complainant_id INTEGER,
    respondent_resident_id INTEGER,
    respondent_name TEXT,
    incident_type TEXT,
    incident_date TEXT,
    incident_time TEXT,
    status TEXT,
    incident_details TEXT,
    resolution_details TEXT,
    created_at TEXT,
    updated_at TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);

CREATE TABLE IF NOT EXISTS document_request (
    doc_request_id INTEGER PRIMARY KEY,
    resident_id INTEGER,
    doc_type_id INTEGER,
    document_no TEXT,
    status TEXT,
    fee REAL,
    or_number TEXT,
    verification_token TEXT,
    expires_at TEXT,
    requested_at TEXT,
    approved_at TEXT,
    released_at TEXT,
    updated_at TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced'
);

CREATE TABLE IF NOT EXISTS sync_queue (
    queue_id INTEGER PRIMARY KEY AUTOINCREMENT,
    table_name TEXT NOT NULL,
    operation TEXT NOT NULL,
    sql_text TEXT NOT NULL,
    parameter_json TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    dedupe_key TEXT NOT NULL,
    retry_count INTEGER NOT NULL DEFAULT 0,
    last_error TEXT,
    UNIQUE (dedupe_key)
);