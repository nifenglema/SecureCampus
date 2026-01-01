USE master;

BACKUP CERTIFICATE SecureCampusTDECert
TO FILE = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\SecureCampusTDECert.cer'
WITH PRIVATE KEY (
    FILE = 'C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\Backup\SecureCampusTDECert_PrivateKey.pvk',
    ENCRYPTION BY PASSWORD = 'CertBackupPass!2025'
);
