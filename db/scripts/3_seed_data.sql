-- The following users do not include Auth entries, as it would request creating password hash, salt etc
-- Can't login with these users, register is required to login 


USE [$(DB_NAME)];
GO

IF NOT EXISTS (SELECT 1 FROM MainSchema.Users WHERE Email = N'firstname.lastname@example.com')
BEGIN
    INSERT INTO MainSchema.Users (FirstName, LastName, Email, Gender, Active)
    VALUES (N'FirstName', N'LastName', N'firstname.lastname@example.com', N'Female', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM MainSchema.Users WHERE Email = N'name.surname@example.com')
BEGIN
    INSERT INTO MainSchema.Users (FirstName, LastName, Email, Gender, Active)
    VALUES (N'Name', N'Surname', N'name.surname@example.com', N'Male', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM MainSchema.Users WHERE Email = N'test@example.com')
BEGIN
    INSERT INTO MainSchema.Users (FirstName, LastName, Email, Gender, Active)
    VALUES (N'Testing', N'Test', N'test@example.com', N'Female', 0);
END
GO
