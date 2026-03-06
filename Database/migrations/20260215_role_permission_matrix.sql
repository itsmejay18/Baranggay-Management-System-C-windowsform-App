CREATE TABLE IF NOT EXISTS role_permission (
    role_id INT NOT NULL,
    permission_key VARCHAR(100) NOT NULL,
    is_allowed TINYINT(1) NOT NULL DEFAULT 0,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (role_id, permission_key),
    CONSTRAINT fk_role_permission_role
        FOREIGN KEY (role_id) REFERENCES role(role_id) ON DELETE CASCADE
);

INSERT INTO role_permission (role_id, permission_key, is_allowed)
SELECT
    r.role_id,
    p.permission_key,
    CASE
        WHEN r.name = 'Admin' THEN 1
        WHEN r.name = 'Staff' AND p.permission_key IN (
            'residents.create',
            'residents.update',
            'certificates.request',
            'certificates.edit_request',
            'blotter.create'
        ) THEN 1
        ELSE 0
    END AS is_allowed
FROM role r
INNER JOIN (
    SELECT 'residents.create' AS permission_key
    UNION ALL SELECT 'residents.update'
    UNION ALL SELECT 'residents.delete'
    UNION ALL SELECT 'certificates.request'
    UNION ALL SELECT 'certificates.edit_request'
    UNION ALL SELECT 'certificates.approve'
    UNION ALL SELECT 'certificates.issue'
    UNION ALL SELECT 'certificates.cancel'
    UNION ALL SELECT 'certificates.export'
    UNION ALL SELECT 'blotter.create'
    UNION ALL SELECT 'blotter.update_status'
    UNION ALL SELECT 'users.manage'
    UNION ALL SELECT 'settings.open'
    UNION ALL SELECT 'announcements.manage'
    UNION ALL SELECT 'projects.manage'
) p ON 1 = 1
WHERE r.name IN ('Admin', 'Staff')
ON DUPLICATE KEY UPDATE
    is_allowed = VALUES(is_allowed),
    updated_at = CURRENT_TIMESTAMP;
