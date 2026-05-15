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
    suffix TEXT,
    sex TEXT NOT NULL DEFAULT 'M',
    birth_date TEXT,
    civil_status TEXT,
    contact_no TEXT,
    status TEXT NOT NULL DEFAULT 'ACTIVE',
    photo BLOB,
    photo_url TEXT,
    education_level TEXT,
    occupation TEXT,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    is_senior INTEGER NOT NULL DEFAULT 0,
    is_pwd INTEGER NOT NULL DEFAULT 0,
    is_4ps_beneficiary INTEGER NOT NULL DEFAULT 0,
    is_registered_voter INTEGER NOT NULL DEFAULT 0,
    is_head_of_family INTEGER NOT NULL DEFAULT 0,
    is_solo_parent INTEGER NOT NULL DEFAULT 0,
    is_youth INTEGER NOT NULL DEFAULT 0,
    is_indigent INTEGER NOT NULL DEFAULT 0,
    deleted_at TEXT,
    deleted_by_user_id INTEGER,
    delete_reason TEXT,
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
    action_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
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
    name TEXT,
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


CREATE TABLE IF NOT EXISTS sync_queue (
    queue_id INTEGER PRIMARY KEY AUTOINCREMENT,
    table_name TEXT NOT NULL DEFAULT 'unknown',
    operation TEXT NOT NULL DEFAULT 'UNKNOWN',
    sql_text TEXT NOT NULL,
    parameter_json TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    dedupe_key TEXT UNIQUE,
    retry_count INTEGER NOT NULL DEFAULT 0,
    last_error TEXT
);
CREATE INDEX IF NOT EXISTS idx_sync_queue_created ON sync_queue (created_at);

-- =========================================================================
-- GOVERNANCE: MEETINGS & RESOLUTIONS
-- =========================================================================

CREATE TABLE IF NOT EXISTS barangay_meeting (
    meeting_id         INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id        INTEGER NOT NULL DEFAULT 1,
    meeting_type       TEXT NOT NULL DEFAULT 'REGULAR',
    title              TEXT NOT NULL,
    scheduled_at       TEXT NOT NULL,
    venue              TEXT,
    agenda             TEXT,
    minutes            TEXT,
    status             TEXT NOT NULL DEFAULT 'SCHEDULED',
    attendance_count   INTEGER NOT NULL DEFAULT 0,
    quorum_reached     INTEGER NOT NULL DEFAULT 0,
    created_by_user_id INTEGER,
    created_at         TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at         TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status        TEXT NOT NULL DEFAULT 'synced'
);

CREATE INDEX IF NOT EXISTS idx_meeting_scheduled ON barangay_meeting (scheduled_at);
CREATE INDEX IF NOT EXISTS idx_meeting_status ON barangay_meeting (status);

CREATE TABLE IF NOT EXISTS meeting_attendance (
    attendance_id  INTEGER PRIMARY KEY AUTOINCREMENT,
    meeting_id     INTEGER NOT NULL,
    official_id    INTEGER,
    attendee_name  TEXT NOT NULL,
    position       TEXT,
    is_present     INTEGER NOT NULL DEFAULT 1,
    remarks        TEXT,
    sync_status    TEXT NOT NULL DEFAULT 'synced',
    FOREIGN KEY (meeting_id) REFERENCES barangay_meeting (meeting_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_attendance_meeting ON meeting_attendance (meeting_id);

CREATE TABLE IF NOT EXISTS barangay_resolution (
    resolution_id      INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id        INTEGER NOT NULL DEFAULT 1,
    meeting_id         INTEGER,
    document_type      TEXT NOT NULL DEFAULT 'RESOLUTION',
    document_number    TEXT NOT NULL,
    series_year        INTEGER NOT NULL,
    title              TEXT NOT NULL,
    description        TEXT,
    full_text          TEXT,
    effectivity_date   TEXT,
    expiration_date    TEXT,
    status             TEXT NOT NULL DEFAULT 'DRAFT',
    authored_by        TEXT,
    approved_by        TEXT,
    approved_at        TEXT,
    created_by_user_id INTEGER,
    created_at         TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at         TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status        TEXT NOT NULL DEFAULT 'synced',
    FOREIGN KEY (meeting_id) REFERENCES barangay_meeting (meeting_id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_resolution_number ON barangay_resolution (document_number, series_year);
CREATE INDEX IF NOT EXISTS idx_resolution_status ON barangay_resolution (status);
CREATE INDEX IF NOT EXISTS idx_resolution_type ON barangay_resolution (document_type);

-- =========================================================================
-- FACILITY BOOKING
-- =========================================================================

CREATE TABLE IF NOT EXISTS barangay_facility (
    facility_id     INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id     INTEGER NOT NULL DEFAULT 1,
    facility_name   TEXT NOT NULL,
    facility_type   TEXT NOT NULL DEFAULT 'VENUE',
    capacity        INTEGER,
    hourly_rate     REAL NOT NULL DEFAULT 0,
    location        TEXT,
    description     TEXT,
    is_active       INTEGER NOT NULL DEFAULT 1,
    created_at      TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status     TEXT NOT NULL DEFAULT 'synced'
);

CREATE INDEX IF NOT EXISTS idx_facility_active ON barangay_facility (is_active);
CREATE INDEX IF NOT EXISTS idx_facility_type ON barangay_facility (facility_type);

CREATE TABLE IF NOT EXISTS facility_booking (
    booking_id           INTEGER PRIMARY KEY AUTOINCREMENT,
    facility_id          INTEGER NOT NULL,
    resident_id          INTEGER,
    requester_name       TEXT NOT NULL,
    requester_contact    TEXT,
    purpose              TEXT NOT NULL,
    start_at             TEXT NOT NULL,
    end_at               TEXT NOT NULL,
    expected_guests      INTEGER,
    total_amount         REAL NOT NULL DEFAULT 0,
    payment_status       TEXT NOT NULL DEFAULT 'UNPAID',
    status               TEXT NOT NULL DEFAULT 'PENDING',
    approved_by_user_id  INTEGER,
    approved_at          TEXT,
    cancellation_reason  TEXT,
    remarks              TEXT,
    created_by_user_id   INTEGER,
    created_at           TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at           TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status          TEXT NOT NULL DEFAULT 'synced',
    FOREIGN KEY (facility_id) REFERENCES barangay_facility (facility_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_booking_facility ON facility_booking (facility_id, start_at);
CREATE INDEX IF NOT EXISTS idx_booking_range ON facility_booking (start_at, end_at);
CREATE INDEX IF NOT EXISTS idx_booking_status ON facility_booking (status);

-- Seed default facilities
INSERT INTO barangay_facility (facility_name, facility_type, capacity, hourly_rate, location, is_active)
SELECT 'Barangay Hall', 'VENUE', 100, 0, 'Main Building', 1
WHERE NOT EXISTS (SELECT 1 FROM barangay_facility WHERE facility_name = 'Barangay Hall');

INSERT INTO barangay_facility (facility_name, facility_type, capacity, hourly_rate, location, is_active)
SELECT 'Covered Court', 'VENUE', 300, 0, 'Plaza Area', 1
WHERE NOT EXISTS (SELECT 1 FROM barangay_facility WHERE facility_name = 'Covered Court');

INSERT INTO barangay_facility (facility_name, facility_type, capacity, hourly_rate, location, is_active)
SELECT 'Multi-Purpose Hall', 'VENUE', 150, 0, 'Community Center', 1
WHERE NOT EXISTS (SELECT 1 FROM barangay_facility WHERE facility_name = 'Multi-Purpose Hall');

-- =========================================================================
-- TANOD PATROL SCHEDULER
-- =========================================================================

CREATE TABLE IF NOT EXISTS tanod_member (
    tanod_id        INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id     INTEGER NOT NULL DEFAULT 1,
    resident_id     INTEGER,
    full_name       TEXT NOT NULL,
    contact_number  TEXT,
    rank_title      TEXT,
    date_assigned   TEXT,
    is_active       INTEGER NOT NULL DEFAULT 1,
    remarks         TEXT,
    created_at      TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status     TEXT NOT NULL DEFAULT 'synced'
);

CREATE INDEX IF NOT EXISTS idx_tanod_active ON tanod_member (is_active);

CREATE TABLE IF NOT EXISTS tanod_shift (
    shift_id           INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id        INTEGER NOT NULL DEFAULT 1,
    shift_date         TEXT NOT NULL,
    shift_type         TEXT NOT NULL DEFAULT 'MORNING',
    start_time         TEXT NOT NULL,
    end_time           TEXT NOT NULL,
    area_assignment    TEXT,
    notes              TEXT,
    created_by_user_id INTEGER,
    created_at         TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at         TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status        TEXT NOT NULL DEFAULT 'synced'
);

CREATE INDEX IF NOT EXISTS idx_shift_date ON tanod_shift (shift_date);
CREATE INDEX IF NOT EXISTS idx_shift_type ON tanod_shift (shift_type);

CREATE TABLE IF NOT EXISTS tanod_shift_assignment (
    assignment_id     INTEGER PRIMARY KEY AUTOINCREMENT,
    shift_id          INTEGER NOT NULL,
    tanod_id          INTEGER NOT NULL,
    attendance_status TEXT NOT NULL DEFAULT 'SCHEDULED',
    check_in_at       TEXT,
    check_out_at      TEXT,
    sync_status       TEXT NOT NULL DEFAULT 'synced',
    UNIQUE (shift_id, tanod_id),
    FOREIGN KEY (shift_id) REFERENCES tanod_shift (shift_id) ON DELETE CASCADE,
    FOREIGN KEY (tanod_id) REFERENCES tanod_member (tanod_id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_assignment_shift ON tanod_shift_assignment (shift_id);
CREATE INDEX IF NOT EXISTS idx_assignment_tanod ON tanod_shift_assignment (tanod_id);

CREATE TABLE IF NOT EXISTS tanod_patrol_log (
    log_id             INTEGER PRIMARY KEY AUTOINCREMENT,
    shift_id           INTEGER,
    barangay_id        INTEGER NOT NULL DEFAULT 1,
    logged_at          TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    location           TEXT,
    incident_type      TEXT,
    description        TEXT NOT NULL,
    severity           TEXT NOT NULL DEFAULT 'LOW',
    action_taken       TEXT,
    reported_by        TEXT,
    created_by_user_id INTEGER,
    created_at         TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status        TEXT NOT NULL DEFAULT 'synced',
    FOREIGN KEY (shift_id) REFERENCES tanod_shift (shift_id) ON DELETE SET NULL
);

CREATE INDEX IF NOT EXISTS idx_patrol_shift ON tanod_patrol_log (shift_id);
CREATE INDEX IF NOT EXISTS idx_patrol_logged ON tanod_patrol_log (logged_at);
CREATE INDEX IF NOT EXISTS idx_patrol_severity ON tanod_patrol_log (severity);

-- =========================================================================
-- DOCUMENT REQUEST / ANNOUNCEMENTS / NOTIFICATIONS / HEARINGS
-- =========================================================================

CREATE TABLE IF NOT EXISTS document_request (
    doc_request_id   INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id      INTEGER NOT NULL DEFAULT 1,
    resident_id      INTEGER,
    document_type_id INTEGER,
    document_no      TEXT,
    status           TEXT NOT NULL DEFAULT 'SUBMITTED',
    purpose          TEXT,
    fee              REAL NOT NULL DEFAULT 0,
    or_number        TEXT,
    business_name    TEXT,
    business_nature  TEXT,
    verification_token TEXT,
    verification_token_created_at TEXT,
    expires_at       TEXT,
    renewed_from_request_id INTEGER,
    renewal_notified_at TEXT,
    release_notified_at TEXT,
    print_count      INTEGER NOT NULL DEFAULT 0,
    last_printed_at  TEXT,
    remarks          TEXT,
    requested_at     TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    approved_at      TEXT,
    released_at      TEXT,
    cancelled_at     TEXT,
    created_by_user_id INTEGER,
    created_at       TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at       TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status      TEXT NOT NULL DEFAULT 'synced'
);
CREATE INDEX IF NOT EXISTS idx_doc_request_status ON document_request (status);
CREATE INDEX IF NOT EXISTS idx_doc_request_resident ON document_request (resident_id);

CREATE TABLE IF NOT EXISTS document_payment (
    payment_id     INTEGER PRIMARY KEY AUTOINCREMENT,
    doc_request_id INTEGER NOT NULL,
    amount         REAL,
    or_no          TEXT,
    payment_method TEXT,
    paid_at        TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    received_by_user_id INTEGER,
    sync_status    TEXT NOT NULL DEFAULT 'synced'
);
CREATE INDEX IF NOT EXISTS idx_doc_payment_request ON document_payment (doc_request_id);

CREATE TABLE IF NOT EXISTS announcements (
    announcement_id INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id     INTEGER NOT NULL DEFAULT 1,
    title           TEXT NOT NULL,
    body            TEXT,
    priority        TEXT NOT NULL DEFAULT 'Normal',
    status          TEXT NOT NULL DEFAULT 'Published',
    is_pinned       INTEGER NOT NULL DEFAULT 0,
    created_by_user_id INTEGER,
    created_at      TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at      TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status     TEXT NOT NULL DEFAULT 'synced'
);
CREATE INDEX IF NOT EXISTS idx_announcement_status ON announcements (status, created_at);

CREATE TABLE IF NOT EXISTS outbound_notification (
    notification_id  INTEGER PRIMARY KEY AUTOINCREMENT,
    dedupe_key       TEXT,
    channel          TEXT NOT NULL,
    recipient        TEXT NOT NULL,
    subject          TEXT,
    message          TEXT NOT NULL,
    status           TEXT NOT NULL DEFAULT 'PENDING',
    source_module    TEXT,
    source_record_id INTEGER,
    template_key     TEXT,
    scheduled_at     TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sent_at          TEXT,
    attempts         INTEGER NOT NULL DEFAULT 0,
    last_error       TEXT,
    created_by_user_id INTEGER,
    created_at       TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at       TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status      TEXT NOT NULL DEFAULT 'synced',
    UNIQUE (dedupe_key)
);
CREATE INDEX IF NOT EXISTS idx_outbound_status ON outbound_notification (status, scheduled_at);
CREATE INDEX IF NOT EXISTS idx_outbound_source ON outbound_notification (source_module, source_record_id);

CREATE TABLE IF NOT EXISTS outbound_notification_attempt (
    attempt_id       INTEGER PRIMARY KEY AUTOINCREMENT,
    notification_id  INTEGER NOT NULL,
    attempt_no       INTEGER NOT NULL,
    attempted_at     TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    success          INTEGER NOT NULL DEFAULT 0,
    response_code    TEXT,
    response_message TEXT,
    FOREIGN KEY (notification_id) REFERENCES outbound_notification (notification_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS case_hearing (
    hearing_id       INTEGER PRIMARY KEY AUTOINCREMENT,
    case_id          INTEGER NOT NULL,
    schedule_at      TEXT,
    venue            TEXT,
    status           TEXT NOT NULL DEFAULT 'SCHEDULED',
    minutes          TEXT,
    result           TEXT,
    created_by_user_id INTEGER,
    sync_status      TEXT NOT NULL DEFAULT 'synced',
    FOREIGN KEY (case_id) REFERENCES case_record (case_id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS idx_case_hearing_case ON case_hearing (case_id);
CREATE INDEX IF NOT EXISTS idx_case_hearing_schedule ON case_hearing (schedule_at);

CREATE TABLE IF NOT EXISTS case_timeline (
    timeline_id      INTEGER PRIMARY KEY AUTOINCREMENT,
    case_id          INTEGER NOT NULL,
    event_type       TEXT NOT NULL,
    event_title      TEXT NOT NULL,
    event_details    TEXT,
    from_status      TEXT,
    to_status        TEXT,
    created_at       TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INTEGER,
    sync_status      TEXT NOT NULL DEFAULT 'synced',
    FOREIGN KEY (case_id) REFERENCES case_record (case_id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS idx_case_timeline_case ON case_timeline (case_id);

CREATE TABLE IF NOT EXISTS record_attachment (
    attachment_id    INTEGER PRIMARY KEY AUTOINCREMENT,
    entity_type      TEXT NOT NULL,
    entity_id        INTEGER NOT NULL,
    file_name        TEXT NOT NULL,
    file_ext         TEXT,
    mime_type        TEXT,
    file_size_bytes  INTEGER NOT NULL DEFAULT 0,
    file_hash        TEXT,
    file_blob        BLOB NOT NULL,
    notes            TEXT,
    uploaded_by_user_id INTEGER,
    uploaded_at      TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status      TEXT NOT NULL DEFAULT 'synced'
);
CREATE INDEX IF NOT EXISTS idx_attachment_entity ON record_attachment (entity_type, entity_id);

CREATE TABLE IF NOT EXISTS backup_run (
    backup_run_id    INTEGER PRIMARY KEY AUTOINCREMENT,
    started_at       TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ended_at         TEXT,
    status           TEXT NOT NULL DEFAULT 'RUNNING',
    backup_type      TEXT NOT NULL DEFAULT 'FULL',
    base_started_at  TEXT,
    base_backup_run_id INTEGER,
    file_path        TEXT,
    file_size_bytes  INTEGER,
    error_message    TEXT,
    created_by_user_id INTEGER,
    sync_status      TEXT NOT NULL DEFAULT 'synced'
);

-- =========================================================================
-- EMERGENCY CONTACTS DIRECTORY
-- =========================================================================

CREATE TABLE IF NOT EXISTS emergency_contact (
    contact_id       INTEGER PRIMARY KEY AUTOINCREMENT,
    barangay_id      INTEGER NOT NULL DEFAULT 1,
    category         TEXT NOT NULL DEFAULT 'OTHER',
    agency_name      TEXT NOT NULL,
    contact_person   TEXT,
    phone_primary    TEXT NOT NULL,
    phone_secondary  TEXT,
    email            TEXT,
    address          TEXT,
    notes            TEXT,
    is_priority      INTEGER NOT NULL DEFAULT 0,
    is_active        INTEGER NOT NULL DEFAULT 1,
    created_by_user_id INTEGER,
    created_at       TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at       TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status      TEXT NOT NULL DEFAULT 'synced'
);
CREATE INDEX IF NOT EXISTS idx_ec_category ON emergency_contact (category);
CREATE INDEX IF NOT EXISTS idx_ec_priority ON emergency_contact (is_priority, is_active);

INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'POLICE', 'Philippine National Police (PNP) Emergency', '911', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='POLICE' AND agency_name='Philippine National Police (PNP) Emergency');

INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'FIRE', 'Bureau of Fire Protection (BFP)', '160', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='FIRE' AND agency_name='Bureau of Fire Protection (BFP)');

INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'MEDICAL', 'Red Cross 143 Emergency Hotline', '143', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='MEDICAL' AND agency_name='Red Cross 143 Emergency Hotline');

INSERT INTO emergency_contact (category, agency_name, phone_primary, is_priority, is_active)
SELECT 'DISASTER', 'NDRRMC Emergency Operations Center', '(02) 8911-1406', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM emergency_contact WHERE category='DISASTER' AND agency_name='NDRRMC Emergency Operations Center');
