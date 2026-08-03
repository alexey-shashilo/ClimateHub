-- Climate Hub - PostgreSQL Schema Initialization
-- Creates logical schemas for domain modules

CREATE SCHEMA IF NOT EXISTS building;
CREATE SCHEMA IF NOT EXISTS device;
CREATE SCHEMA IF NOT EXISTS environment;
CREATE SCHEMA IF NOT EXISTS outbox;
CREATE SCHEMA IF NOT EXISTS platform;