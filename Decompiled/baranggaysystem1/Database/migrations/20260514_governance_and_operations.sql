-- Migration: Governance, Facility Booking, and Tanod Patrol features
-- Date: 2026-05-14

-- =========================================================================
-- MEETINGS & RESOLUTIONS / ORDINANCES
-- =========================================================================

CREATE TABLE IF NOT EXISTS barangay_meeting (
    meeting_id       INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id      INT NOT NULL DEFAULT 1,
    meeting_type     VARCHAR(40) NOT NULL DEFAULT 'REGULAR',
    title            VARCHAR(200) NOT NULL,
    scheduled_at     DATETIME NOT NULL,
    venue            VARCHAR(200) NULL,
    agenda           TEXT NULL,
    minutes          LONGTEXT NULL,
    status           VARCHAR(30) NOT NULL DEFAULT 'SCHEDULED',
    attendance_count INT NOT NULL DEFAULT 0,
    quorum_reached   TINYINT NOT NULL DEFAULT 0,
    created_by_user_id INT NULL,
    created_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_meeting_scheduled (scheduled_at),
    INDEX idx_meeting_status (status)
);

CREATE TABLE IF NOT EXISTS meeting_attendance (
    attendance_id    INT AUTO_INCREMENT PRIMARY KEY,
    meeting_id       INT NOT NULL,
    official_id      INT NULL,
    attendee_name    VARCHAR(150) NOT NULL,
    position         VARCHAR(100) NULL,
    is_present       TINYINT NOT NULL DEFAULT 1,
    remarks          VARCHAR(255) NULL,
    INDEX idx_attendance_meeting (meeting_id),
    FOREIGN KEY (meeting_id) REFERENCES barangay_meeting(meeting_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS barangay_resolution (
    resolution_id    INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id      INT NOT NULL DEFAULT 1,
    meeting_id       INT NULL,
    document_type    VARCHAR(30) NOT NULL DEFAULT 'RESOLUTION',
    document_number  VARCHAR(50) NOT NULL,
    series_year      INT NOT NULL,
    title            VARCHAR(300) NOT NULL,
    description      TEXT NULL,
    full_text        LONGTEXT NULL,
    effectivity_date DATE NULL,
    expiration_date  DATE NULL,
    status           VARCHAR(30) NOT NULL DEFAULT 'DRAFT',
    authored_by      VARCHAR(150) NULL,
    approved_by      VARCHAR(150) NULL,
    approved_at      DATETIME NULL,
    created_by_user_id INT NULL,
    created_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_resolution_number (document_number, series_year),
    INDEX idx_resolution_status (status),
    INDEX idx_resolution_type (document_type),
    FOREIGN KEY (meeting_id) REFERENCES barangay_meeting(meeting_id) ON DELETE SET NULL
);

-- =========================================================================
-- FACILITY BOOKING
-- =========================================================================

CREATE TABLE IF NOT EXISTS barangay_facility (
    facility_id      INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id      INT NOT NULL DEFAULT 1,
    facility_name    VARCHAR(150) NOT NULL,
    facility_type    VARCHAR(40) NOT NULL DEFAULT 'VENUE',
    capacity         INT NULL,
    hourly_rate      DECIMAL(10,2) NOT NULL DEFAULT 0,
    location         VARCHAR(200) NULL,
    description      TEXT NULL,
    is_active        TINYINT NOT NULL DEFAULT 1,
    created_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_facility_active (is_active),
    INDEX idx_facility_type (facility_type)
);

CREATE TABLE IF NOT EXISTS facility_booking (
    booking_id       INT AUTO_INCREMENT PRIMARY KEY,
    facility_id      INT NOT NULL,
    resident_id      INT NULL,
    requester_name   VARCHAR(150) NOT NULL,
    requester_contact VARCHAR(50) NULL,
    purpose          VARCHAR(300) NOT NULL,
    start_at         DATETIME NOT NULL,
    end_at           DATETIME NOT NULL,
    expected_guests  INT NULL,
    total_amount     DECIMAL(10,2) NOT NULL DEFAULT 0,
    payment_status   VARCHAR(20) NOT NULL DEFAULT 'UNPAID',
    status           VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    approved_by_user_id INT NULL,
    approved_at      DATETIME NULL,
    cancellation_reason VARCHAR(255) NULL,
    remarks          TEXT NULL,
    created_by_user_id INT NULL,
    created_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_booking_facility (facility_id, start_at),
    INDEX idx_booking_range (start_at, end_at),
    INDEX idx_booking_status (status),
    FOREIGN KEY (facility_id) REFERENCES barangay_facility(facility_id) ON DELETE CASCADE
);

-- Seed common facilities
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
    tanod_id         INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id      INT NOT NULL DEFAULT 1,
    resident_id      INT NULL,
    full_name        VARCHAR(150) NOT NULL,
    contact_number   VARCHAR(50) NULL,
    rank_title       VARCHAR(50) NULL,
    date_assigned    DATE NULL,
    is_active        TINYINT NOT NULL DEFAULT 1,
    remarks          VARCHAR(255) NULL,
    created_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_tanod_active (is_active)
);

CREATE TABLE IF NOT EXISTS tanod_shift (
    shift_id         INT AUTO_INCREMENT PRIMARY KEY,
    barangay_id      INT NOT NULL DEFAULT 1,
    shift_date       DATE NOT NULL,
    shift_type       VARCHAR(20) NOT NULL DEFAULT 'MORNING',
    start_time       TIME NOT NULL,
    end_time         TIME NOT NULL,
    area_assignment  VARCHAR(200) NULL,
    notes            TEXT NULL,
    created_by_user_id INT NULL,
    created_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_shift_date (shift_date),
    INDEX idx_shift_type (shift_type)
);

CREATE TABLE IF NOT EXISTS tanod_shift_assignment (
    assignment_id    INT AUTO_INCREMENT PRIMARY KEY,
    shift_id         INT NOT NULL,
    tanod_id         INT NOT NULL,
    attendance_status VARCHAR(20) NOT NULL DEFAULT 'SCHEDULED',
    check_in_at      DATETIME NULL,
    check_out_at     DATETIME NULL,
    INDEX idx_assignment_shift (shift_id),
    INDEX idx_assignment_tanod (tanod_id),
    UNIQUE KEY ux_shift_tanod (shift_id, tanod_id),
    FOREIGN KEY (shift_id) REFERENCES tanod_shift(shift_id) ON DELETE CASCADE,
    FOREIGN KEY (tanod_id) REFERENCES tanod_member(tanod_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS tanod_patrol_log (
    log_id           INT AUTO_INCREMENT PRIMARY KEY,
    shift_id         INT NULL,
    barangay_id      INT NOT NULL DEFAULT 1,
    logged_at        DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    location         VARCHAR(200) NULL,
    incident_type    VARCHAR(60) NULL,
    description      TEXT NOT NULL,
    severity         VARCHAR(20) NOT NULL DEFAULT 'LOW',
    action_taken     TEXT NULL,
    reported_by      VARCHAR(150) NULL,
    created_by_user_id INT NULL,
    created_at       DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_patrol_shift (shift_id),
    INDEX idx_patrol_logged (logged_at),
    INDEX idx_patrol_severity (severity),
    FOREIGN KEY (shift_id) REFERENCES tanod_shift(shift_id) ON DELETE SET NULL
);
