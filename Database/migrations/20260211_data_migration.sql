-- @manual
-- Data migration from legacy schema to new schema.
-- Assumes legacy DB name is `barangay_db` and new DB name is `barangay_system`.
-- Update database names below if your legacy DB differs.

SET FOREIGN_KEY_CHECKS = 0;

-- Households
INSERT IGNORE INTO barangay_system.purok_sitio (barangay_id, name, type)
SELECT 1, TRIM(h.purok), 'PUROK'
FROM barangay_db.households h
WHERE h.purok IS NOT NULL AND TRIM(h.purok) <> '';

INSERT IGNORE INTO barangay_system.household (household_id, barangay_id, purok_id, house_no, street, created_at)
SELECT h.household_id,
       1,
       COALESCE(p.purok_id, 1),
       h.house_no,
       h.street,
       h.created_at
FROM barangay_db.households h
LEFT JOIN barangay_system.purok_sitio p ON p.barangay_id = 1 AND p.name = h.purok;

UPDATE barangay_system.household h
LEFT JOIN barangay_db.households lh ON lh.household_id = h.household_id
LEFT JOIN barangay_system.purok_sitio p ON p.barangay_id = 1 AND p.name = lh.purok
SET h.purok_id = COALESCE(p.purok_id, 1)
WHERE h.purok_id IS NULL OR h.purok_id = 1;

-- Residents
INSERT IGNORE INTO barangay_system.resident (
    resident_id,
    barangay_id,
    household_id,
    purok_id,
    first_name,
    middle_name,
    last_name,
    sex,
    birth_date,
    civil_status,
    contact_no,
    status,
    photo,
    created_at
)
SELECT r.resident_id,
       1,
       r.household_id,
       COALESCE(hh.purok_id, 1),
       r.firstname,
       r.middlename,
       r.lastname,
       CASE r.gender
           WHEN 'Male' THEN 'M'
           WHEN 'Female' THEN 'F'
           ELSE 'M'
       END,
       r.date_of_birth,
       r.civil_status,
       r.contact_no,
       CASE r.status
           WHEN 'Active' THEN 'ACTIVE'
           WHEN 'Deceased' THEN 'DECEASED'
           ELSE 'MOVED_OUT'
       END,
       r.photo,
       r.created_at
FROM barangay_db.residents r
LEFT JOIN barangay_system.household hh ON hh.household_id = r.household_id;

UPDATE barangay_system.resident r
LEFT JOIN barangay_system.household hh ON hh.household_id = r.household_id
SET r.purok_id = COALESCE(hh.purok_id, 1)
WHERE r.purok_id IS NULL OR r.purok_id = 1;

-- Users
INSERT IGNORE INTO barangay_system.user_account (
    user_id,
    barangay_id,
    username,
    password_hash,
    full_name,
    first_name,
    middle_name,
    last_name,
    contact_no,
    email,
    position,
    department,
    last_project,
    photo_url,
    is_active,
    last_login_at,
    created_at
)
SELECT u.user_id,
       1,
       u.username,
       u.password_hash,
       TRIM(CONCAT_WS(' ', u.first_name, u.middle_name, u.last_name)),
       u.first_name,
       u.middle_name,
       u.last_name,
       u.contact_no,
       u.email,
       u.position,
       u.department,
       u.last_project,
       u.photo,
       CASE WHEN u.is_active = 1 THEN 1 ELSE 0 END,
       u.last_login,
       u.created_at
FROM barangay_db.users u;

-- Roles
INSERT IGNORE INTO barangay_system.user_role (user_id, role_id)
SELECT u.user_id, r.role_id
FROM barangay_db.users u
JOIN barangay_system.role r ON r.name = u.role;

-- Certificates -> Document Requests
INSERT IGNORE INTO barangay_system.document_request (
    doc_request_id,
    barangay_id,
    doc_type_id,
    resident_id,
    purpose,
    status,
    requested_at,
    approved_at,
    released_at,
    requested_by_user_id,
    approved_by_user_id,
    released_by_user_id,
    remarks,
    document_no,
    fee,
    or_number,
    business_name,
    business_nature,
    print_count,
    last_printed_at
)
SELECT c.certificate_id,
       1,
       COALESCE(dt.doc_type_id, (SELECT doc_type_id FROM barangay_system.document_type WHERE name = 'Barangay Clearance' LIMIT 1)),
       c.resident_id,
       c.purpose,
       CASE c.status
           WHEN 'Requested' THEN 'SUBMITTED'
           WHEN 'Approved' THEN 'APPROVED'
           WHEN 'Issued' THEN 'RELEASED'
           WHEN 'Cancelled' THEN 'CANCELLED'
           ELSE 'SUBMITTED'
       END,
       c.requested_at,
       c.approved_at,
       CASE WHEN c.issued_date IS NULL THEN NULL ELSE CONCAT(c.issued_date, ' 00:00:00') END,
       c.requested_by,
       c.approved_by,
       c.issued_by,
       c.remarks,
       c.certificate_no,
       c.fee,
       c.or_number,
       c.business_name,
       c.business_nature,
       c.print_count,
       c.last_printed_at
FROM barangay_db.certificates c
LEFT JOIN barangay_system.document_type dt ON dt.name = c.certificate_type;

-- Blotter -> Cases
INSERT IGNORE INTO barangay_system.case_record (
    case_id,
    barangay_id,
    case_type_id,
    date_filed,
    incident_date,
    incident_location,
    summary,
    status,
    handled_by_user_id,
    complainant_id,
    respondent_resident_id,
    respondent_name,
    incident_type,
    incident_time,
    witness_names,
    action_taken,
    resolution_details,
    incident_details,
    recorded_by,
    created_at
)
SELECT b.blotter_id,
       1,
       COALESCE(ct.case_type_id, (SELECT case_type_id FROM barangay_system.case_type WHERE name = 'General' LIMIT 1)),
       b.incident_date,
       b.incident_date,
       NULL,
       b.incident_details,
       CASE b.status
           WHEN 'Ongoing' THEN 'ONGOING'
           WHEN 'Settled' THEN 'SETTLED'
           WHEN 'Referred' THEN 'REFERRED'
           ELSE 'OPEN'
       END,
       b.recorded_by,
       b.complainant_id,
       b.respondent_resident_id,
       b.respondent_name,
       b.incident_type,
       b.incident_time,
       b.witness_names,
       b.action_taken,
       b.resolution_details,
       b.incident_details,
       b.recorded_by,
       b.created_at
FROM barangay_db.blotter_records b
LEFT JOIN barangay_system.case_type ct ON ct.name = b.incident_type;

-- Certificate audit + activity log (carry over)
INSERT IGNORE INTO barangay_system.certificate_audit
SELECT * FROM barangay_db.certificate_audit;

INSERT IGNORE INTO barangay_system.activity_log
SELECT * FROM barangay_db.activity_log;

SET FOREIGN_KEY_CHECKS = 1;
