-- Auto-generated from Database/Dump20260316 MySQL schema and adapted for SQLite.
PRAGMA foreign_keys = OFF;

CREATE TABLE IF NOT EXISTS activity_log (
    log_id INTEGER NOT NULL,
    resident_id INTEGER NOT NULL,
    module TEXT NOT NULL,
    action TEXT NOT NULL,
    details TEXT,
    action_by INTEGER,
    action_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (log_id)
);

CREATE TABLE IF NOT EXISTS announcement_user_state (
    user_id INTEGER NOT NULL,
    announcement_id INTEGER NOT NULL,
    state TEXT NOT NULL DEFAULT 'NEW',
    read_at TEXT,
    archived_at TEXT,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (user_id, announcement_id)
);

CREATE TABLE IF NOT EXISTS announcements (
    announcement_id INTEGER NOT NULL,
    title TEXT NOT NULL,
    body TEXT,
    priority TEXT DEFAULT 'Normal',
    status TEXT DEFAULT 'Published',
    is_pinned INTEGER DEFAULT 0,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (announcement_id)
);

CREATE TABLE IF NOT EXISTS audit_trail (
    audit_id INTEGER NOT NULL,
    module TEXT NOT NULL,
    entity_type TEXT NOT NULL,
    entity_id TEXT,
    action TEXT NOT NULL,
    before_json TEXT,
    after_json TEXT,
    notes TEXT,
    action_by INTEGER,
    action_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (audit_id)
);

CREATE TABLE IF NOT EXISTS backup_run (
    backup_run_id INTEGER NOT NULL,
    started_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ended_at TEXT,
    status TEXT NOT NULL DEFAULT 'RUNNING',
    file_path TEXT,
    file_size_bytes INTEGER,
    error_message TEXT,
    created_by_user_id INTEGER,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (backup_run_id),
    CONSTRAINT backup_run_ibfk_1 FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS barangay (
    barangay_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    city_municipality TEXT,
    province TEXT,
    region TEXT,
    address_line TEXT,
    contact_no TEXT,
    email TEXT,
    logo_url TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (barangay_id)
);

CREATE TABLE IF NOT EXISTS barangay_official (
    official_id INTEGER NOT NULL,
    term_id INTEGER NOT NULL,
    resident_id INTEGER NOT NULL,
    position TEXT,
    committee TEXT,
    status TEXT DEFAULT 'ACTIVE',
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (official_id),
    CONSTRAINT barangay_official_ibfk_1 FOREIGN KEY (term_id) REFERENCES official_term (term_id) ON DELETE CASCADE,
    CONSTRAINT barangay_official_ibfk_2 FOREIGN KEY (resident_id) REFERENCES resident (resident_id)
);

CREATE TABLE IF NOT EXISTS case_attachment (
    attachment_id INTEGER NOT NULL,
    case_id INTEGER NOT NULL,
    file_url TEXT,
    file_name TEXT,
    uploaded_by_user_id INTEGER,
    uploaded_at TEXT DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (attachment_id),
    CONSTRAINT case_attachment_ibfk_1 FOREIGN KEY (case_id) REFERENCES case_record (case_id) ON DELETE CASCADE,
    CONSTRAINT case_attachment_ibfk_2 FOREIGN KEY (uploaded_by_user_id) REFERENCES user_account (user_id)
);

CREATE TABLE IF NOT EXISTS case_hearing (
    hearing_id INTEGER NOT NULL,
    case_id INTEGER NOT NULL,
    schedule_at TEXT,
    venue TEXT,
    status TEXT DEFAULT 'SCHEDULED',
    minutes TEXT,
    result TEXT,
    created_by_user_id INTEGER,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (hearing_id),
    CONSTRAINT case_hearing_ibfk_1 FOREIGN KEY (case_id) REFERENCES case_record (case_id) ON DELETE CASCADE,
    CONSTRAINT case_hearing_ibfk_2 FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id)
);

CREATE TABLE IF NOT EXISTS case_party (
    case_party_id INTEGER NOT NULL,
    case_id INTEGER NOT NULL,
    resident_id INTEGER,
    full_name TEXT,
    address TEXT,
    contact_no TEXT,
    party_role TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (case_party_id),
    CONSTRAINT case_party_ibfk_1 FOREIGN KEY (case_id) REFERENCES case_record (case_id) ON DELETE CASCADE,
    CONSTRAINT case_party_ibfk_2 FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS case_record (
    case_id INTEGER NOT NULL,
    barangay_id INTEGER NOT NULL,
    case_type_id INTEGER NOT NULL,
    case_no TEXT,
    date_filed TEXT,
    incident_date TEXT,
    incident_location TEXT,
    summary TEXT,
    status TEXT DEFAULT 'OPEN',
    handled_by_user_id INTEGER,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    complainant_id INTEGER,
    respondent_resident_id INTEGER,
    respondent_name TEXT,
    incident_type TEXT,
    incident_time TEXT,
    witness_names TEXT,
    action_taken TEXT,
    resolution_details TEXT,
    incident_details TEXT,
    recorded_by INTEGER,
    ai_summary TEXT,
    ai_key_points TEXT,
    ai_category TEXT,
    ai_category_confidence REAL,
    ai_risk_level TEXT,
    ai_risk_score INTEGER,
    ai_risk_reasons TEXT,
    ai_entities TEXT,
    ai_recommended_next_action TEXT,
    ai_model TEXT,
    ai_processed_at TEXT,
    referral_destination TEXT,
    closure_notes TEXT,
    closed_at TEXT,
    closed_by_user_id INTEGER,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (case_id),
    CONSTRAINT case_record_ibfk_1 FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT case_record_ibfk_2 FOREIGN KEY (case_type_id) REFERENCES case_type (case_type_id),
    CONSTRAINT case_record_ibfk_3 FOREIGN KEY (handled_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS case_resolution (
    resolution_id INTEGER NOT NULL,
    case_id INTEGER NOT NULL,
    resolution_date TEXT,
    outcome TEXT,
    details TEXT,
    signed_by_official_id INTEGER,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (resolution_id),
    CONSTRAINT case_resolution_ibfk_1 FOREIGN KEY (case_id) REFERENCES case_record (case_id) ON DELETE CASCADE,
    CONSTRAINT case_resolution_ibfk_2 FOREIGN KEY (signed_by_official_id) REFERENCES barangay_official (official_id)
);

CREATE TABLE IF NOT EXISTS case_timeline (
    timeline_id INTEGER NOT NULL,
    case_id INTEGER NOT NULL,
    event_type TEXT NOT NULL,
    event_title TEXT NOT NULL,
    event_details TEXT,
    from_status TEXT,
    to_status TEXT,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    created_by_user_id INTEGER,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (timeline_id),
    CONSTRAINT case_timeline_ibfk_1 FOREIGN KEY (case_id) REFERENCES case_record (case_id) ON DELETE CASCADE,
    CONSTRAINT case_timeline_ibfk_2 FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS case_type (
    case_type_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (case_type_id)
);

CREATE TABLE IF NOT EXISTS certificate_audit (
    audit_id INTEGER NOT NULL,
    certificate_id INTEGER NOT NULL,
    action TEXT NOT NULL,
    action_by INTEGER,
    action_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    notes TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (audit_id)
);

CREATE TABLE IF NOT EXISTS document_number_sequence (
    doc_type_id INTEGER NOT NULL,
    year INTEGER NOT NULL,
    last_no INTEGER NOT NULL DEFAULT 0,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (doc_type_id, year),
    CONSTRAINT document_number_sequence_ibfk_1 FOREIGN KEY (doc_type_id) REFERENCES document_type (doc_type_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS document_payment (
    payment_id INTEGER NOT NULL,
    doc_request_id INTEGER NOT NULL,
    amount REAL,
    or_no TEXT,
    payment_method TEXT,
    paid_at TEXT DEFAULT CURRENT_TIMESTAMP,
    received_by_user_id INTEGER,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (payment_id),
    UNIQUE (or_no),
    CONSTRAINT document_payment_ibfk_1 FOREIGN KEY (doc_request_id) REFERENCES document_request (doc_request_id) ON DELETE CASCADE,
    CONSTRAINT document_payment_ibfk_2 FOREIGN KEY (received_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS document_release_log (
    release_log_id INTEGER NOT NULL,
    doc_request_id INTEGER NOT NULL,
    action TEXT,
    user_id INTEGER,
    timestamp TEXT DEFAULT CURRENT_TIMESTAMP,
    notes TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (release_log_id),
    CONSTRAINT document_release_log_ibfk_1 FOREIGN KEY (doc_request_id) REFERENCES document_request (doc_request_id) ON DELETE CASCADE,
    CONSTRAINT document_release_log_ibfk_2 FOREIGN KEY (user_id) REFERENCES user_account (user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS document_request (
    doc_request_id INTEGER NOT NULL,
    barangay_id INTEGER NOT NULL,
    doc_type_id INTEGER NOT NULL,
    resident_id INTEGER NOT NULL,
    purpose TEXT,
    status TEXT DEFAULT 'SUBMITTED',
    requested_at TEXT DEFAULT CURRENT_TIMESTAMP,
    approved_at TEXT,
    released_at TEXT,
    requested_by_user_id INTEGER,
    approved_by_user_id INTEGER,
    released_by_user_id INTEGER,
    remarks TEXT,
    document_no TEXT,
    fee REAL DEFAULT 0.00,
    or_number TEXT,
    business_name TEXT,
    business_nature TEXT,
    print_count INTEGER NOT NULL DEFAULT 0,
    last_printed_at TEXT,
    verification_token TEXT,
    verification_token_created_at TEXT,
    expires_at TEXT,
    renewed_from_request_id INTEGER,
    renewal_notified_at TEXT,
    release_notified_at TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (doc_request_id),
    UNIQUE (verification_token),
    CONSTRAINT document_request_ibfk_1 FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT document_request_ibfk_2 FOREIGN KEY (doc_type_id) REFERENCES document_type (doc_type_id),
    CONSTRAINT document_request_ibfk_3 FOREIGN KEY (resident_id) REFERENCES resident (resident_id),
    CONSTRAINT document_request_ibfk_4 FOREIGN KEY (requested_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL,
    CONSTRAINT document_request_ibfk_5 FOREIGN KEY (approved_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL,
    CONSTRAINT document_request_ibfk_6 FOREIGN KEY (released_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS document_type (
    doc_type_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    code TEXT,
    template_path TEXT,
    template_html TEXT,
    fee_default REAL DEFAULT 0.00,
    validity_days INTEGER,
    requires_approval INTEGER DEFAULT 1,
    renewal_reminder_days INTEGER,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (doc_type_id),
    UNIQUE (code)
);

CREATE TABLE IF NOT EXISTS household (
    household_id INTEGER NOT NULL,
    barangay_id INTEGER NOT NULL,
    purok_id INTEGER NOT NULL,
    house_no TEXT,
    street TEXT,
    subdivision TEXT,
    address_note TEXT,
    latitude REAL,
    longitude REAL,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (household_id),
    CONSTRAINT household_ibfk_1 FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT household_ibfk_2 FOREIGN KEY (purok_id) REFERENCES purok_sitio (purok_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS official_term (
    term_id INTEGER NOT NULL,
    barangay_id INTEGER NOT NULL,
    term_start TEXT,
    term_end TEXT,
    notes TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (term_id),
    CONSTRAINT official_term_ibfk_1 FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS outbound_notification (
    notification_id INTEGER NOT NULL,
    dedupe_key TEXT,
    channel TEXT NOT NULL,
    recipient TEXT NOT NULL,
    subject TEXT,
    message TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'PENDING',
    source_module TEXT,
    source_record_id INTEGER,
    template_key TEXT,
    scheduled_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sent_at TEXT,
    attempts INTEGER NOT NULL DEFAULT 0,
    last_error TEXT,
    created_by_user_id INTEGER,
    created_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (notification_id),
    UNIQUE (dedupe_key),
    CONSTRAINT fk_outbound_notification_user FOREIGN KEY (created_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS outbound_notification_attempt (
    attempt_id INTEGER NOT NULL,
    notification_id INTEGER NOT NULL,
    attempt_no INTEGER NOT NULL,
    attempted_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    success INTEGER NOT NULL DEFAULT 0,
    response_code TEXT,
    response_message TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (attempt_id),
    CONSTRAINT fk_notification_attempt_notification FOREIGN KEY (notification_id) REFERENCES outbound_notification (notification_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS projects (
    project_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    status TEXT DEFAULT 'Planned',
    budget REAL DEFAULT 0.00,
    start_date TEXT,
    end_date TEXT,
    lead TEXT,
    remarks TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (project_id)
);

CREATE TABLE IF NOT EXISTS purok_sitio (
    purok_id INTEGER NOT NULL,
    barangay_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    type TEXT DEFAULT 'PUROK',
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    latitude REAL,
    longitude REAL,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (purok_id),
    CONSTRAINT purok_sitio_ibfk_1 FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS record_attachment (
    attachment_id INTEGER NOT NULL,
    entity_type TEXT NOT NULL,
    entity_id INTEGER NOT NULL,
    file_name TEXT NOT NULL,
    file_ext TEXT,
    mime_type TEXT,
    file_size_bytes INTEGER NOT NULL DEFAULT 0,
    file_hash TEXT,
    file_blob BLOB NOT NULL,
    notes TEXT,
    uploaded_by_user_id INTEGER,
    uploaded_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (attachment_id),
    CONSTRAINT fk_record_attachment_uploaded_by FOREIGN KEY (uploaded_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS resident (
    resident_id INTEGER NOT NULL,
    barangay_id INTEGER NOT NULL,
    household_id INTEGER,
    purok_id INTEGER NOT NULL,
    last_name TEXT NOT NULL,
    first_name TEXT NOT NULL,
    middle_name TEXT,
    suffix TEXT,
    sex TEXT NOT NULL,
    birth_date TEXT NOT NULL,
    birth_place TEXT,
    civil_status TEXT,
    citizenship TEXT DEFAULT 'Filipino',
    religion TEXT,
    contact_no TEXT,
    email TEXT,
    occupation TEXT,
    employer TEXT,
    education_level TEXT,
    is_pwd INTEGER DEFAULT 0,
    pwd_id_no TEXT,
    is_senior INTEGER DEFAULT 0,
    is_4ps_beneficiary INTEGER DEFAULT 0,
    is_registered_voter INTEGER DEFAULT 0,
    voter_precinct_no TEXT,
    status TEXT DEFAULT 'ACTIVE',
    date_registered TEXT DEFAULT (date('now')),
    photo_url TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    photo BLOB,
    is_deleted INTEGER NOT NULL DEFAULT 0,
    deleted_at TEXT,
    deleted_by_user_id INTEGER,
    delete_reason TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (resident_id),
    CONSTRAINT resident_ibfk_1 FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT resident_ibfk_2 FOREIGN KEY (household_id) REFERENCES household (household_id) ON DELETE SET NULL,
    CONSTRAINT resident_ibfk_3 FOREIGN KEY (purok_id) REFERENCES purok_sitio (purok_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS resident_alias (
    alias_id INTEGER NOT NULL,
    resident_id INTEGER NOT NULL,
    alias_name TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (alias_id),
    CONSTRAINT resident_alias_ibfk_1 FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS resident_relationship (
    relationship_id INTEGER NOT NULL,
    resident_id INTEGER NOT NULL,
    related_resident_id INTEGER NOT NULL,
    relation_type TEXT,
    notes TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (relationship_id),
    CONSTRAINT resident_relationship_ibfk_1 FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE CASCADE,
    CONSTRAINT resident_relationship_ibfk_2 FOREIGN KEY (related_resident_id) REFERENCES resident (resident_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS resident_transfer_history (
    transfer_id INTEGER NOT NULL,
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
    PRIMARY KEY (transfer_id),
    CONSTRAINT fk_transfer_history_new_household FOREIGN KEY (new_household_id) REFERENCES household (household_id) ON DELETE SET NULL,
    CONSTRAINT fk_transfer_history_new_purok FOREIGN KEY (new_purok_id) REFERENCES purok_sitio (purok_id) ON DELETE SET NULL,
    CONSTRAINT fk_transfer_history_old_household FOREIGN KEY (old_household_id) REFERENCES household (household_id) ON DELETE SET NULL,
    CONSTRAINT fk_transfer_history_old_purok FOREIGN KEY (old_purok_id) REFERENCES purok_sitio (purok_id) ON DELETE SET NULL,
    CONSTRAINT fk_transfer_history_resident FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE CASCADE,
    CONSTRAINT fk_transfer_history_user FOREIGN KEY (transferred_by_user_id) REFERENCES user_account (user_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS role (
    role_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (role_id)
);

CREATE TABLE IF NOT EXISTS role_permission (
    role_id INTEGER NOT NULL,
    permission_key TEXT NOT NULL,
    is_allowed INTEGER NOT NULL DEFAULT 0,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (role_id, permission_key),
    CONSTRAINT role_permission_ibfk_1 FOREIGN KEY (role_id) REFERENCES role (role_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS schema_migrations (
    migration_name TEXT NOT NULL,
    applied_at TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (migration_name)
);

CREATE TABLE IF NOT EXISTS user_account (
    user_id INTEGER NOT NULL,
    barangay_id INTEGER NOT NULL,
    username TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    resident_id INTEGER,
    full_name TEXT,
    contact_no TEXT,
    email TEXT,
    is_active INTEGER DEFAULT 1,
    last_login_at TEXT,
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    updated_at TEXT DEFAULT CURRENT_TIMESTAMP,
    photo_url TEXT,
    first_name TEXT,
    middle_name TEXT,
    last_name TEXT,
    position TEXT,
    department TEXT,
    last_project TEXT,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (user_id),
    UNIQUE (username),
    CONSTRAINT user_account_ibfk_1 FOREIGN KEY (barangay_id) REFERENCES barangay (barangay_id) ON DELETE CASCADE,
    CONSTRAINT user_account_ibfk_2 FOREIGN KEY (resident_id) REFERENCES resident (resident_id) ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS user_role (
    user_role_id INTEGER NOT NULL,
    user_id INTEGER NOT NULL,
    role_id INTEGER NOT NULL,
    sync_status TEXT NOT NULL DEFAULT 'synced',
    PRIMARY KEY (user_role_id),
    CONSTRAINT user_role_ibfk_1 FOREIGN KEY (user_id) REFERENCES user_account (user_id) ON DELETE CASCADE,
    CONSTRAINT user_role_ibfk_2 FOREIGN KEY (role_id) REFERENCES role (role_id) ON DELETE CASCADE
);

-- Indexes
CREATE INDEX IF NOT EXISTS idx_activity_log_idx_activity_module ON activity_log (module);
CREATE INDEX IF NOT EXISTS idx_activity_log_idx_activity_resident ON activity_log (resident_id);
CREATE INDEX IF NOT EXISTS idx_announcement_user_state_idx_announcement_state ON announcement_user_state (announcement_id,state);
CREATE INDEX IF NOT EXISTS idx_announcement_user_state_idx_user_state ON announcement_user_state (user_id,state);
CREATE INDEX IF NOT EXISTS idx_audit_trail_idx_audit_action_at ON audit_trail (action_at);
CREATE INDEX IF NOT EXISTS idx_audit_trail_idx_audit_entity ON audit_trail (entity_type,entity_id);
CREATE INDEX IF NOT EXISTS idx_audit_trail_idx_audit_module ON audit_trail (module);
CREATE INDEX IF NOT EXISTS idx_backup_run_created_by_user_id ON backup_run (created_by_user_id);
CREATE INDEX IF NOT EXISTS idx_backup_run_idx_backup_run_started_at ON backup_run (started_at);
CREATE INDEX IF NOT EXISTS idx_backup_run_idx_backup_run_status ON backup_run (status);
CREATE INDEX IF NOT EXISTS idx_barangay_official_resident_id ON barangay_official (resident_id);
CREATE INDEX IF NOT EXISTS idx_barangay_official_term_id ON barangay_official (term_id);
CREATE INDEX IF NOT EXISTS idx_case_attachment_case_id ON case_attachment (case_id);
CREATE INDEX IF NOT EXISTS idx_case_attachment_uploaded_by_user_id ON case_attachment (uploaded_by_user_id);
CREATE INDEX IF NOT EXISTS idx_case_hearing_case_id ON case_hearing (case_id);
CREATE INDEX IF NOT EXISTS idx_case_hearing_created_by_user_id ON case_hearing (created_by_user_id);
CREATE INDEX IF NOT EXISTS idx_case_party_case_id ON case_party (case_id);
CREATE INDEX IF NOT EXISTS idx_case_party_resident_id ON case_party (resident_id);
CREATE INDEX IF NOT EXISTS idx_case_record_case_type_id ON case_record (case_type_id);
CREATE INDEX IF NOT EXISTS idx_case_record_handled_by_user_id ON case_record (handled_by_user_id);
CREATE INDEX IF NOT EXISTS idx_case_record_idx_case_record_barangay ON case_record (barangay_id);
CREATE INDEX IF NOT EXISTS idx_case_record_idx_case_record_date_status ON case_record (date_filed,status,complainant_id);
CREATE INDEX IF NOT EXISTS idx_case_record_idx_case_record_incident_date ON case_record (incident_date);
CREATE INDEX IF NOT EXISTS idx_case_record_idx_case_record_status ON case_record (status);
CREATE INDEX IF NOT EXISTS idx_case_resolution_case_id ON case_resolution (case_id);
CREATE INDEX IF NOT EXISTS idx_case_resolution_signed_by_official_id ON case_resolution (signed_by_official_id);
CREATE INDEX IF NOT EXISTS idx_case_timeline_created_by_user_id ON case_timeline (created_by_user_id);
CREATE INDEX IF NOT EXISTS idx_case_timeline_idx_case_timeline_case ON case_timeline (case_id);
CREATE INDEX IF NOT EXISTS idx_case_timeline_idx_case_timeline_created_at ON case_timeline (created_at);
CREATE INDEX IF NOT EXISTS idx_certificate_audit_idx_audit_cert ON certificate_audit (certificate_id);
CREATE INDEX IF NOT EXISTS idx_document_payment_idx_doc_payment_paid_at ON document_payment (paid_at);
CREATE INDEX IF NOT EXISTS idx_document_payment_idx_doc_payment_request ON document_payment (doc_request_id);
CREATE INDEX IF NOT EXISTS idx_document_payment_received_by_user_id ON document_payment (received_by_user_id);
CREATE INDEX IF NOT EXISTS idx_document_release_log_doc_request_id ON document_release_log (doc_request_id);
CREATE INDEX IF NOT EXISTS idx_document_release_log_user_id ON document_release_log (user_id);
CREATE INDEX IF NOT EXISTS idx_document_request_approved_by_user_id ON document_request (approved_by_user_id);
CREATE INDEX IF NOT EXISTS idx_document_request_barangay_id ON document_request (barangay_id);
CREATE INDEX IF NOT EXISTS idx_document_request_doc_type_id ON document_request (doc_type_id);
CREATE INDEX IF NOT EXISTS idx_document_request_idx_doc_request_requested_at ON document_request (requested_at);
CREATE INDEX IF NOT EXISTS idx_document_request_idx_doc_request_resident ON document_request (resident_id);
CREATE INDEX IF NOT EXISTS idx_document_request_idx_doc_request_resident_status ON document_request (resident_id,status);
CREATE INDEX IF NOT EXISTS idx_document_request_idx_doc_request_status ON document_request (status);
CREATE INDEX IF NOT EXISTS idx_document_request_idx_document_request_expires_at ON document_request (expires_at);
CREATE INDEX IF NOT EXISTS idx_document_request_idx_document_request_renewed_from ON document_request (renewed_from_request_id);
CREATE INDEX IF NOT EXISTS idx_document_request_released_by_user_id ON document_request (released_by_user_id);
CREATE INDEX IF NOT EXISTS idx_document_request_requested_by_user_id ON document_request (requested_by_user_id);
CREATE INDEX IF NOT EXISTS idx_household_idx_household_barangay ON household (barangay_id);
CREATE INDEX IF NOT EXISTS idx_household_idx_household_purok ON household (purok_id);
CREATE INDEX IF NOT EXISTS idx_official_term_barangay_id ON official_term (barangay_id);
CREATE INDEX IF NOT EXISTS idx_outbound_notification_attempt_idx_notification_attempt_notification ON outbound_notification_attempt (notification_id,attempted_at);
CREATE INDEX IF NOT EXISTS idx_outbound_notification_fk_outbound_notification_user ON outbound_notification (created_by_user_id);
CREATE INDEX IF NOT EXISTS idx_outbound_notification_idx_outbound_notification_channel ON outbound_notification (channel,status);
CREATE INDEX IF NOT EXISTS idx_outbound_notification_idx_outbound_notification_source ON outbound_notification (source_module,source_record_id);
CREATE INDEX IF NOT EXISTS idx_outbound_notification_idx_outbound_notification_status ON outbound_notification (status,scheduled_at);
CREATE INDEX IF NOT EXISTS idx_purok_sitio_idx_purok_barangay ON purok_sitio (barangay_id);
CREATE INDEX IF NOT EXISTS idx_purok_sitio_idx_purok_coordinates ON purok_sitio (latitude,longitude);
CREATE INDEX IF NOT EXISTS idx_record_attachment_fk_record_attachment_uploaded_by ON record_attachment (uploaded_by_user_id);
CREATE INDEX IF NOT EXISTS idx_record_attachment_idx_attachment_entity ON record_attachment (entity_type,entity_id,uploaded_at);
CREATE INDEX IF NOT EXISTS idx_record_attachment_idx_attachment_hash ON record_attachment (file_hash);
CREATE INDEX IF NOT EXISTS idx_resident_alias_resident_id ON resident_alias (resident_id);
CREATE INDEX IF NOT EXISTS idx_resident_household_id ON resident (household_id);
CREATE INDEX IF NOT EXISTS idx_resident_idx_resident_barangay ON resident (barangay_id);
CREATE INDEX IF NOT EXISTS idx_resident_idx_resident_name ON resident (last_name,first_name,middle_name);
CREATE INDEX IF NOT EXISTS idx_resident_idx_resident_purok ON resident (purok_id);
CREATE INDEX IF NOT EXISTS idx_resident_idx_resident_status ON resident (status);
CREATE INDEX IF NOT EXISTS idx_resident_relationship_related_resident_id ON resident_relationship (related_resident_id);
CREATE INDEX IF NOT EXISTS idx_resident_relationship_resident_id ON resident_relationship (resident_id);
CREATE INDEX IF NOT EXISTS idx_resident_transfer_history_fk_transfer_history_new_household ON resident_transfer_history (new_household_id);
CREATE INDEX IF NOT EXISTS idx_resident_transfer_history_fk_transfer_history_old_household ON resident_transfer_history (old_household_id);
CREATE INDEX IF NOT EXISTS idx_resident_transfer_history_fk_transfer_history_user ON resident_transfer_history (transferred_by_user_id);
CREATE INDEX IF NOT EXISTS idx_resident_transfer_history_idx_transfer_history_new_location ON resident_transfer_history (new_purok_id,new_household_id);
CREATE INDEX IF NOT EXISTS idx_resident_transfer_history_idx_transfer_history_old_location ON resident_transfer_history (old_purok_id,old_household_id);
CREATE INDEX IF NOT EXISTS idx_resident_transfer_history_idx_transfer_history_resident ON resident_transfer_history (resident_id,transferred_at);
CREATE INDEX IF NOT EXISTS idx_user_account_barangay_id ON user_account (barangay_id);
CREATE INDEX IF NOT EXISTS idx_user_account_resident_id ON user_account (resident_id);
CREATE INDEX IF NOT EXISTS idx_user_role_role_id ON user_role (role_id);
CREATE INDEX IF NOT EXISTS idx_user_role_user_id ON user_role (user_id);
CREATE UNIQUE INDEX IF NOT EXISTS idx_document_payment_ux_document_payment_or_no ON document_payment (or_no);
CREATE UNIQUE INDEX IF NOT EXISTS idx_document_request_ux_document_request_verification_token ON document_request (verification_token);
CREATE UNIQUE INDEX IF NOT EXISTS idx_document_type_code ON document_type (code);
CREATE UNIQUE INDEX IF NOT EXISTS idx_outbound_notification_ux_outbound_notification_dedupe ON outbound_notification (dedupe_key);
CREATE UNIQUE INDEX IF NOT EXISTS idx_user_account_username ON user_account (username);

-- Offline sync queue
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

-- Clean seed data
INSERT OR IGNORE INTO role(role_id, name, description) VALUES (1, 'Super Admin', 'Primary system owner'), (2, 'Admin', 'System administrator'), (3, 'Staff', 'Staff account');
INSERT OR IGNORE INTO barangay(barangay_id, name, city_municipality, province, region, address_line, contact_no, email, logo_url, created_at, updated_at) VALUES (1, 'Default Barangay', NULL, NULL, NULL, NULL, NULL, NULL, NULL, '2026-02-11 12:42:26', '2026-02-11 12:42:26');
INSERT OR IGNORE INTO purok_sitio(purok_id, barangay_id, name, type, created_at, updated_at, latitude, longitude) VALUES (1, 1, 'Default Purok', 'PUROK', '2026-02-11 13:10:16', '2026-02-11 13:10:16', NULL, NULL), (2, 1, 'Purok 1', 'PUROK', '2026-02-11 14:11:15', '2026-02-11 14:11:15', NULL, NULL), (4, 1, 'Purok 2', 'PUROK', '2026-02-11 14:11:47', '2026-02-11 14:11:47', NULL, NULL), (6, 1, 'Purok 3', 'PUROK', '2026-02-11 14:11:55', '2026-02-11 14:11:55', NULL, NULL);
INSERT OR IGNORE INTO household(household_id, barangay_id, purok_id, house_no, street, subdivision, address_note, latitude, longitude, created_at, updated_at) VALUES (1, 1, 2, '001', 'Rizal St', NULL, NULL, NULL, NULL, '2026-02-11 14:11:26', '2026-02-11 14:11:26'), (2, 1, 2, '002', 'Rizal St', NULL, NULL, NULL, NULL, '2026-02-11 14:11:32', '2026-02-11 14:11:32'), (3, 1, 4, '003', 'Bonifacio Ave', NULL, NULL, NULL, NULL, '2026-02-11 14:11:53', '2026-02-11 14:11:53'), (4, 1, 4, '004', 'Bonifacio Ave', NULL, NULL, NULL, NULL, '2026-02-11 14:11:57', '2026-02-11 14:11:57'), (5, 1, 6, '005', 'Mabini St', NULL, NULL, NULL, NULL, '2026-02-11 14:12:00', '2026-02-11 14:12:00');
INSERT OR IGNORE INTO document_type(doc_type_id, name, code, template_path, template_html, fee_default, validity_days, requires_approval, renewal_reminder_days) VALUES (1, 'Barangay Clearance', 'BC', NULL, NULL, 0.00, 365, 1, 30), (2, 'Certificate of Residency', 'CR', NULL, NULL, 0.00, 365, 1, NULL), (3, 'Indigency', 'IND', NULL, NULL, 0.00, 365, 1, NULL), (4, 'Business Clearance', 'BUS', NULL, NULL, 0.00, 365, 1, NULL);
INSERT OR IGNORE INTO case_type(case_type_id, name) VALUES (1, 'General');
INSERT OR IGNORE INTO user_account(user_id, barangay_id, username, password_hash, resident_id, full_name, contact_no, email, is_active, last_login_at, created_at, updated_at, photo_url, first_name, middle_name, last_name, position, department, last_project) VALUES (2, 1, 'Janelle', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', NULL, '', NULL, NULL, 1, NULL, '2026-02-07 11:48:28', '2026-02-11 14:06:23', 'C:\\Users\\Loona\\Downloads\\d0258d35-9358-45a1-b1f4-cd18e9e9b2e2.jpg', NULL, NULL, NULL, NULL, NULL, NULL), (3, 1, 'daryll', '8bb0cf6eb9b17d0f7d22b456f121257dc1254e1f01665370476383ea776df414', NULL, 'daryll', NULL, NULL, 1, '2026-02-18 18:17:42', '2026-02-11 12:42:26', '2026-02-18 10:17:42', NULL, NULL, NULL, NULL, NULL, NULL, NULL), (4, 1, 'daryll2', 'v1.100000.dAOjJ+H8Onq788lFWOnprQ==.0+w9F2M7vNbZi4NkAG+Vhnl0fjriU2xu0wiAvKY/YUk=', NULL, 'daryll2', NULL, NULL, 1, NULL, '2026-02-18 00:08:25', '2026-02-18 00:08:25', 'storage/profile-photos/ed19b0e858414884b758aabe73b887c5.png', NULL, NULL, NULL, NULL, NULL, NULL), (5, 1, 'daryll24', 'v1.100000.8Jz8EKIXS9IL4gfXwQZnnA==.rWlO0/aBN0Q1j2rom0TAUVhWVxFo6w0d7jUrt13tGZ0=', NULL, 'daryll24', NULL, NULL, 1, NULL, '2026-02-18 00:14:19', '2026-02-18 00:14:19', NULL, NULL, NULL, NULL, NULL, NULL, NULL), (7, 1, 'admin', 'v1.100000.MURu1hmxbjzirejeInxAVQ==.5Q61lAVbeSueUJTQQGyM38e9VVxmRVBIq5PbbegsQHE=', NULL, NULL, '', '', 1, '2026-02-18 08:47:14', '2026-02-18 00:19:40', '2026-02-18 10:27:41', 'C:\\Users\\Loona\\Downloads\\136bb18c-aa50-4589-b21c-6bfd6739c050.jpg', '', '', '', '', '', '');
INSERT OR IGNORE INTO user_role(user_role_id, user_id, role_id) VALUES (1, 3, 1), (2, 2, 3), (3, 4, 2), (4, 5, 2), (5, 7, 1);
INSERT OR IGNORE INTO schema_migrations(migration_name, applied_at) VALUES ('20260207_ai_blotter_case_assistant.sql', '2026-02-16 12:59:17'), ('20260211_add_indexes.sql', '2026-02-16 12:46:43'), ('20260211_data_migration.sql', '2026-02-16 12:59:17'), ('20260211_new_schema.sql', '2026-02-16 12:46:42'), ('20260211_patch_app_compat.sql', '2026-02-16 12:46:42'), ('20260211_patch_user_account.sql', '2026-02-16 12:46:42'), ('20260215_backup_run.sql', '2026-02-16 12:46:43'), ('20260215_role_permission_matrix.sql', '2026-02-16 12:46:43'), ('20260216_phase2_feature_gaps.sql', '2026-02-16 12:46:44');

PRAGMA foreign_keys = ON;
