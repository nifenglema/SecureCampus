-- 1) Create a SQL login
CREATE LOGIN sc_viewer WITH PASSWORD = 'P@ssw0rd!123';

-- 2) Create a user in your database
USE SecureCampus;
CREATE USER sc_viewer FOR LOGIN sc_viewer;
