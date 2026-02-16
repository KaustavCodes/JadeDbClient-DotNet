-- Database Setup Script for Bulk Insert Tests
-- Run this script to create the products table in your database

-- ============================================================
-- PostgreSQL
-- ============================================================
-- CREATE TABLE IF NOT EXISTS products (
--     ProductId INT PRIMARY KEY,
--     ProductName VARCHAR(255) NOT NULL,
--     Price DECIMAL(18, 2) NOT NULL,
--     Stock INT NULL
-- );

-- ============================================================
-- MySQL
-- ============================================================
-- CREATE TABLE IF NOT EXISTS products (
--     ProductId INT PRIMARY KEY,
--     ProductName VARCHAR(255) NOT NULL,
--     Price DECIMAL(18, 2) NOT NULL,
--     Stock INT NULL
-- );

-- ============================================================
-- SQL Server (MSSQL)
-- ============================================================
-- CREATE TABLE products (
--     ProductId INT PRIMARY KEY,
--     ProductName NVARCHAR(255) NOT NULL,
--     Price DECIMAL(18, 2) NOT NULL,
--     Stock INT NULL
-- );

-- ============================================================
-- Clean up data between tests (optional)
-- ============================================================
-- TRUNCATE TABLE products;
-- or
-- DELETE FROM products;
