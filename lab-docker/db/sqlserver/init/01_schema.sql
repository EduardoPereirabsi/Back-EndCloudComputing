IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'messages')
BEGIN
  CREATE DATABASE messages;
END
GO
USE messages;
IF NOT EXISTS(SELECT * FROM sysobjects WHERE name='messages' AND xtype='U')
BEGIN
  CREATE TABLE messages (
    id INT IDENTITY(1,1) PRIMARY KEY,
    text NVARCHAR(255) NOT NULL
  );
END
