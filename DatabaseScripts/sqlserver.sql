-- Table: Create if not exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'tbl_test' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.tbl_test (
        id INT IDENTITY(1,1) PRIMARY KEY,
        name NVARCHAR(250),
        createdat DATETIME DEFAULT GETDATE()
    );
END

-- Procedure: Drop and recreate for idempotency
CREATE PROCEDURE dbo.add_data
    @p_name NVARCHAR(250),
    @p_OutputParam NVARCHAR(250) OUTPUT
AS
BEGIN
    INSERT INTO dbo.tbl_test(name) VALUES (@p_name);
    SET @p_OutputParam = 'Inserted';
END
GO


CREATE PROCEDURE dbo.get_data
    @p_limit INT
AS
BEGIN
    SELECT TOP (@p_limit) * FROM dbo.tbl_test;
END