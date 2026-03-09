-- @manual
-- Seed pack for Hostinger demo:
-- 1) 30 resident data records
-- 2) 30 document transactions
-- 3) 5 server-side analytics/report views

SET @seed_prefix := 'SEED20260309';

INSERT INTO purok_sitio (barangay_id, name, type)
SELECT 1, 'Seed Purok A', 'PUROK'
WHERE NOT EXISTS (
    SELECT 1 FROM purok_sitio WHERE barangay_id = 1 AND name = 'Seed Purok A'
);

INSERT INTO purok_sitio (barangay_id, name, type)
SELECT 1, 'Seed Purok B', 'PUROK'
WHERE NOT EXISTS (
    SELECT 1 FROM purok_sitio WHERE barangay_id = 1 AND name = 'Seed Purok B'
);

INSERT INTO purok_sitio (barangay_id, name, type)
SELECT 1, 'Seed Purok C', 'PUROK'
WHERE NOT EXISTS (
    SELECT 1 FROM purok_sitio WHERE barangay_id = 1 AND name = 'Seed Purok C'
);

DROP TEMPORARY TABLE IF EXISTS tmp_seed_numbers;
CREATE TEMPORARY TABLE tmp_seed_numbers (
    n INT NOT NULL PRIMARY KEY
);

INSERT INTO tmp_seed_numbers (n)
SELECT 1 UNION ALL SELECT 2 UNION ALL SELECT 3 UNION ALL SELECT 4 UNION ALL SELECT 5
UNION ALL SELECT 6 UNION ALL SELECT 7 UNION ALL SELECT 8 UNION ALL SELECT 9 UNION ALL SELECT 10
UNION ALL SELECT 11 UNION ALL SELECT 12 UNION ALL SELECT 13 UNION ALL SELECT 14 UNION ALL SELECT 15
UNION ALL SELECT 16 UNION ALL SELECT 17 UNION ALL SELECT 18 UNION ALL SELECT 19 UNION ALL SELECT 20
UNION ALL SELECT 21 UNION ALL SELECT 22 UNION ALL SELECT 23 UNION ALL SELECT 24 UNION ALL SELECT 25
UNION ALL SELECT 26 UNION ALL SELECT 27 UNION ALL SELECT 28 UNION ALL SELECT 29 UNION ALL SELECT 30;

INSERT INTO household (barangay_id, purok_id, house_no, street, subdivision, address_note)
SELECT
    1,
    CASE MOD(t.n - 1, 4)
        WHEN 0 THEN (SELECT p.purok_id FROM purok_sitio p WHERE p.barangay_id = 1 AND p.name = 'Default Purok' LIMIT 1)
        WHEN 1 THEN (SELECT p.purok_id FROM purok_sitio p WHERE p.barangay_id = 1 AND p.name = 'Seed Purok A' LIMIT 1)
        WHEN 2 THEN (SELECT p.purok_id FROM purok_sitio p WHERE p.barangay_id = 1 AND p.name = 'Seed Purok B' LIMIT 1)
        ELSE (SELECT p.purok_id FROM purok_sitio p WHERE p.barangay_id = 1 AND p.name = 'Seed Purok C' LIMIT 1)
    END,
    CONCAT('SEED-H', LPAD(t.n, 2, '0')),
    'Seed Street',
    'Seed Subdivision',
    CONCAT(@seed_prefix, ' household ', t.n)
FROM tmp_seed_numbers t
WHERE t.n <= 10
  AND NOT EXISTS (
      SELECT 1
      FROM household h
      WHERE h.barangay_id = 1
        AND h.house_no = CONCAT('SEED-H', LPAD(t.n, 2, '0'))
        AND COALESCE(h.street, '') = 'Seed Street'
  );

INSERT INTO resident (
    barangay_id,
    household_id,
    purok_id,
    last_name,
    first_name,
    middle_name,
    sex,
    birth_date,
    civil_status,
    citizenship,
    contact_no,
    email,
    occupation,
    status,
    date_registered,
    is_deleted
)
SELECT
    1,
    (SELECT h.household_id
     FROM household h
     WHERE h.barangay_id = 1
       AND h.house_no = CONCAT('SEED-H', LPAD(((t.n - 1) % 10) + 1, 2, '0'))
       AND COALESCE(h.street, '') = 'Seed Street'
     LIMIT 1),
    (SELECT h.purok_id
     FROM household h
     WHERE h.barangay_id = 1
       AND h.house_no = CONCAT('SEED-H', LPAD(((t.n - 1) % 10) + 1, 2, '0'))
       AND COALESCE(h.street, '') = 'Seed Street'
     LIMIT 1),
    CONCAT('SeedLast', LPAD(t.n, 2, '0')),
    CONCAT('SeedFirst', LPAD(t.n, 2, '0')),
    CONCAT('M', LPAD(t.n, 2, '0')),
    CASE WHEN MOD(t.n, 2) = 0 THEN 'F' ELSE 'M' END,
    DATE_SUB(CURDATE(), INTERVAL (20 + t.n) YEAR),
    CASE MOD(t.n, 4)
        WHEN 0 THEN 'Married'
        WHEN 1 THEN 'Single'
        WHEN 2 THEN 'Widowed'
        ELSE 'Separated'
    END,
    'Filipino',
    CONCAT('0999', LPAD(100000 + t.n, 6, '0')),
    CONCAT('seed.resident', LPAD(t.n, 2, '0'), '@example.com'),
    CASE MOD(t.n, 5)
        WHEN 0 THEN 'Vendor'
        WHEN 1 THEN 'Driver'
        WHEN 2 THEN 'Teacher'
        WHEN 3 THEN 'Clerk'
        ELSE 'Farmer'
    END,
    'ACTIVE',
    DATE_SUB(CURDATE(), INTERVAL t.n DAY),
    0
FROM tmp_seed_numbers t
WHERE NOT EXISTS (
    SELECT 1
    FROM resident r
    WHERE r.last_name = CONCAT('SeedLast', LPAD(t.n, 2, '0'))
      AND r.first_name = CONCAT('SeedFirst', LPAD(t.n, 2, '0'))
      AND r.barangay_id = 1
);

INSERT INTO document_request (
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
    verification_token,
    verification_token_created_at,
    business_name,
    business_nature,
    print_count,
    expires_at
)
SELECT
    1,
    CASE MOD(t.n, 4)
        WHEN 0 THEN (SELECT dt.doc_type_id FROM document_type dt WHERE dt.code = 'BC' LIMIT 1)
        WHEN 1 THEN (SELECT dt.doc_type_id FROM document_type dt WHERE dt.code = 'CR' LIMIT 1)
        WHEN 2 THEN (SELECT dt.doc_type_id FROM document_type dt WHERE dt.code = 'IND' LIMIT 1)
        ELSE (SELECT dt.doc_type_id FROM document_type dt WHERE dt.code = 'BUS' LIMIT 1)
    END,
    (SELECT r.resident_id
     FROM resident r
     WHERE r.barangay_id = 1
       AND r.last_name = CONCAT('SeedLast', LPAD(t.n, 2, '0'))
       AND r.first_name = CONCAT('SeedFirst', LPAD(t.n, 2, '0'))
     LIMIT 1),
    CONCAT(@seed_prefix, ' transaction #', LPAD(t.n, 2, '0')),
    CASE MOD(t.n, 5)
        WHEN 0 THEN 'SUBMITTED'
        WHEN 1 THEN 'APPROVED'
        WHEN 2 THEN 'RELEASED'
        WHEN 3 THEN 'REJECTED'
        ELSE 'CANCELLED'
    END,
    DATE_SUB(NOW(), INTERVAL (31 - t.n) DAY),
    CASE
        WHEN MOD(t.n, 5) IN (1, 2) THEN DATE_ADD(DATE_SUB(NOW(), INTERVAL (31 - t.n) DAY), INTERVAL 1 DAY)
        ELSE NULL
    END,
    CASE
        WHEN MOD(t.n, 5) = 2 THEN DATE_ADD(DATE_SUB(NOW(), INTERVAL (31 - t.n) DAY), INTERVAL 2 DAY)
        ELSE NULL
    END,
    (SELECT ua.user_id FROM user_account ua ORDER BY ua.user_id LIMIT 1),
    CASE
        WHEN MOD(t.n, 5) IN (1, 2) THEN (SELECT ua.user_id FROM user_account ua ORDER BY ua.user_id LIMIT 1)
        ELSE NULL
    END,
    CASE
        WHEN MOD(t.n, 5) = 2 THEN (SELECT ua.user_id FROM user_account ua ORDER BY ua.user_id LIMIT 1)
        ELSE NULL
    END,
    CONCAT('Seeded transaction note ', t.n),
    CONCAT('DOC-', DATE_FORMAT(CURDATE(), '%Y%m'), '-', LPAD(t.n, 4, '0')),
    50 + MOD(t.n, 4) * 25,
    CASE WHEN MOD(t.n, 5) = 2 THEN CONCAT('OR-', DATE_FORMAT(CURDATE(), '%Y%m'), '-', LPAD(t.n, 4, '0')) ELSE NULL END,
    CONCAT('seedtok', LPAD(t.n, 25, '0')),
    DATE_SUB(NOW(), INTERVAL (31 - t.n) DAY),
    CASE WHEN MOD(t.n, 4) = 3 THEN CONCAT('Seed Biz ', t.n) ELSE NULL END,
    CASE WHEN MOD(t.n, 4) = 3 THEN 'Retail' ELSE NULL END,
    CASE WHEN MOD(t.n, 5) = 2 THEN 1 ELSE 0 END,
    DATE_ADD(DATE_SUB(NOW(), INTERVAL (31 - t.n) DAY), INTERVAL 365 DAY)
FROM tmp_seed_numbers t
WHERE NOT EXISTS (
    SELECT 1
    FROM document_request dr
    WHERE dr.purpose = CONCAT(@seed_prefix, ' transaction #', LPAD(t.n, 2, '0'))
);

INSERT INTO document_payment (doc_request_id, amount, or_no, payment_method, paid_at, received_by_user_id)
SELECT
    dr.doc_request_id,
    dr.fee,
    COALESCE(dr.or_number, CONCAT('OR-AUTO-', dr.doc_request_id)),
    'Cash',
    COALESCE(dr.released_at, dr.approved_at, dr.requested_at, NOW()),
    (SELECT ua.user_id FROM user_account ua ORDER BY ua.user_id LIMIT 1)
FROM document_request dr
WHERE dr.purpose LIKE CONCAT(@seed_prefix, ' transaction #%')
  AND dr.status = 'RELEASED'
  AND NOT EXISTS (
      SELECT 1
      FROM document_payment dp
      WHERE dp.doc_request_id = dr.doc_request_id
  );

DROP VIEW IF EXISTS vw_rpt_resident_demographics;
CREATE VIEW vw_rpt_resident_demographics AS
SELECT
    p.name AS purok_name,
    COUNT(*) AS total_residents,
    SUM(CASE WHEN r.sex = 'M' THEN 1 ELSE 0 END) AS male_count,
    SUM(CASE WHEN r.sex = 'F' THEN 1 ELSE 0 END) AS female_count,
    SUM(CASE WHEN TIMESTAMPDIFF(YEAR, r.birth_date, CURDATE()) >= 60 THEN 1 ELSE 0 END) AS senior_count
FROM resident r
INNER JOIN purok_sitio p ON p.purok_id = r.purok_id
WHERE IFNULL(r.is_deleted, 0) = 0
GROUP BY p.purok_id, p.name;

DROP VIEW IF EXISTS vw_rpt_certificate_status_summary;
CREATE VIEW vw_rpt_certificate_status_summary AS
SELECT
    UPPER(dr.status) AS certificate_status,
    COUNT(*) AS total_transactions,
    COALESCE(SUM(dr.fee), 0) AS total_amount
FROM document_request dr
GROUP BY UPPER(dr.status);

DROP VIEW IF EXISTS vw_rpt_monthly_transaction_trend;
CREATE VIEW vw_rpt_monthly_transaction_trend AS
SELECT
    DATE_FORMAT(dr.requested_at, '%Y-%m') AS month_key,
    COUNT(*) AS transaction_count,
    SUM(CASE WHEN UPPER(dr.status) = 'RELEASED' THEN 1 ELSE 0 END) AS released_count,
    SUM(CASE WHEN UPPER(dr.status) IN ('SUBMITTED', 'APPROVED') THEN 1 ELSE 0 END) AS in_progress_count
FROM document_request dr
GROUP BY DATE_FORMAT(dr.requested_at, '%Y-%m')
ORDER BY month_key;

DROP VIEW IF EXISTS vw_rpt_document_type_performance;
CREATE VIEW vw_rpt_document_type_performance AS
SELECT
    dt.name AS document_type,
    COUNT(dr.doc_request_id) AS total_requests,
    SUM(CASE WHEN UPPER(dr.status) = 'RELEASED' THEN 1 ELSE 0 END) AS released_requests,
    ROUND(AVG(CASE
        WHEN dr.approved_at IS NOT NULL AND dr.requested_at IS NOT NULL AND dr.approved_at >= dr.requested_at
            THEN TIMESTAMPDIFF(HOUR, dr.requested_at, dr.approved_at)
        ELSE NULL
    END), 2) AS avg_approval_hours
FROM document_type dt
LEFT JOIN document_request dr ON dr.doc_type_id = dt.doc_type_id
GROUP BY dt.doc_type_id, dt.name;

DROP VIEW IF EXISTS vw_rpt_staff_productivity;
CREATE VIEW vw_rpt_staff_productivity AS
SELECT
    ua.user_id,
    ua.username,
    COALESCE(NULLIF(ua.full_name, ''), ua.username) AS display_name,
    SUM(CASE WHEN dr.approved_by_user_id = ua.user_id THEN 1 ELSE 0 END) AS approvals_done,
    SUM(CASE WHEN dr.released_by_user_id = ua.user_id THEN 1 ELSE 0 END) AS releases_done,
    SUM(CASE WHEN cr.recorded_by = ua.user_id THEN 1 ELSE 0 END) AS blotter_recorded,
    SUM(CASE WHEN cr.closed_by_user_id = ua.user_id THEN 1 ELSE 0 END) AS blotter_closed
FROM user_account ua
LEFT JOIN document_request dr
    ON dr.approved_by_user_id = ua.user_id
    OR dr.released_by_user_id = ua.user_id
LEFT JOIN case_record cr
    ON cr.recorded_by = ua.user_id
    OR cr.closed_by_user_id = ua.user_id
GROUP BY ua.user_id, ua.username, ua.full_name;

DROP TEMPORARY TABLE IF EXISTS tmp_seed_numbers;
