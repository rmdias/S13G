-- Initial database setup for S13G
-- This script is run automatically when the PostgreSQL container starts

-- Create extensions if they don't exist
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create database if not exists (useful for development)
-- Note: This is usually handled by POSTGRES_DB environment variable

-- Enable UUID uuid generation
CREATE OR REPLACE FUNCTION uuid_generate_v4()
RETURNS uuid AS '$libdir/uuid-ossp', 'uuid_generate_v4'
LANGUAGE c STRICT;
