-- Table: Create if not exists
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'tbl_test') THEN
        CREATE TABLE public.tbl_test (
            id SERIAL PRIMARY KEY,
            name VARCHAR(250),
            createdat TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        );
    END IF;
END
$$;

-- Procedure: Drop and recreate for idempotency
DROP PROCEDURE IF EXISTS public.add_data;
CREATE OR REPLACE PROCEDURE public.add_data(
    IN p_name VARCHAR,
    OUT p_OutputParam VARCHAR
)
LANGUAGE plpgsql
AS $$
BEGIN
    INSERT INTO public.tbl_test("name") VALUES (p_name);
    p_OutputParam := 'Inserted';
END;
$$;

-- Function: Drop and recreate for idempotency
DROP FUNCTION IF EXISTS public.get_data(INT);
CREATE OR REPLACE FUNCTION public.get_data(p_limit INT)
RETURNS TABLE(id INT, name VARCHAR, createdat TIMESTAMP)
LANGUAGE plpgsql
AS $$
BEGIN
    RETURN QUERY SELECT * FROM public.tbl_test LIMIT p_limit;
END;
$$;