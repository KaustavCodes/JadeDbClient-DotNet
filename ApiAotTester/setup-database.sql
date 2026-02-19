-- Database Setup Script for Bulk Insert Tests
-- Run this script to create the products table in your database

-- ============================================================
-- PostgreSQL
-- ============================================================
CREATE TABLE IF NOT EXISTS products (
    ProductId INT PRIMARY KEY,
    ProductName VARCHAR(255) NOT NULL,
    Price DECIMAL(18, 2) NOT NULL,
    Stock INT NULL
);

CREATE TABLE tbl_test (
    id SERIAL PRIMARY KEY,
    name VARCHAR(250)
);


CREATE OR REPLACE PROCEDURE add_data(
    p_name IN VARCHAR,
    INOUT p_outputparam VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO tbl_test(name) VALUES(p_name);
    p_outputparam := 'Success: ' || p_name;
END;
$$;

-- ============================================================
-- MySQL
-- ============================================================
CREATE TABLE IF NOT EXISTS products (
    ProductId INT PRIMARY KEY,
    ProductName VARCHAR(255) NOT NULL,
    Price DECIMAL(18, 2) NOT NULL,
    Stock INT NULL
);

CREATE TABLE tbl_test (
    id INT AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(250)
);


DELIMITER //
CREATE PROCEDURE get_data(IN p_limit INT)
BEGIN
    SELECT id, name FROM tbl_test LIMIT p_limit;
END //
DELIMITER ;

-- ============================================================
-- SQL Server (MSSQL)
-- ============================================================
CREATE TABLE products (
    ProductId INT PRIMARY KEY,
    ProductName NVARCHAR(255) NOT NULL,
    Price DECIMAL(18, 2) NOT NULL,
    Stock INT NULL
);

CREATE TABLE tbl_test (
    id INT IDENTITY(1,1) PRIMARY KEY,
    name NVARCHAR(250)
);

CREATE PROCEDURE get_data @p_limit INT
AS
BEGIN
    SELECT TOP (@p_limit) id, name FROM tbl_test;
END;

-- ============================================================
-- Clean up data between tests (optional)
-- ============================================================
-- TRUNCATE TABLE products;
-- or
-- DELETE FROM products;
