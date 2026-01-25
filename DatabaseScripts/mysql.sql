-- Table: Create if not exists
CREATE TABLE IF NOT EXISTS tbl_test (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(250),
    createdat TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Procedure: Drop and recreate for idempotency
DROP PROCEDURE IF EXISTS add_data;
DELIMITER //
CREATE PROCEDURE add_data(
    IN p_name VARCHAR(250),
    OUT p_OutputParam VARCHAR(250)
)
BEGIN
    INSERT INTO tbl_test(name) VALUES (p_name);
    SET p_OutputParam = 'Inserted';
END;
//
DELIMITER ;

DROP PROCEDURE IF EXISTS get_data;
DELIMITER //
CREATE PROCEDURE get_data(
    IN p_limit INT
)
BEGIN
    SELECT * FROM tbl_test LIMIT p_limit;
END;
//
DELIMITER ;