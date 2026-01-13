USE [ADB_API_SVILUPPO]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

--------------------------------------------------------------------------------
-- 1. xTSP_API_Put_Fidelity
-- Parameters matching C# SendApiService.RegisterUserAsync (lowercase)
-- Types matching User provided schema
--------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[xTSP_API_Put_Fidelity]
    @store        VARCHAR(6)   = NULL, -- Matches "store" (CdNE)
    @tipo         CHAR(1)      = 'D',  -- Matches "tipo" (Tipo)
    @nome         VARCHAR(40)  = NULL, -- Matches "nome" (Nome)
    @cognome      VARCHAR(40)  = NULL, -- Matches "cognome" (Cognome)
    @sesso        CHAR(1)      = NULL, -- Matches "sesso" (Sesso)
    @data_nascita VARCHAR(8)   = NULL, -- Matches "data_nascita" (ddMMyyyy)
    @indirizzo    VARCHAR(50)  = NULL, -- Matches "indirizzo" (Indirizzo varchar(50))
    @localita     VARCHAR(50)  = NULL, -- Matches "localita" (Localita varchar(50))
    @cap          VARCHAR(6)   = NULL, -- Matches "cap" (CdCap char(6))
    @provincia    VARCHAR(6)   = NULL, -- Matches "provincia" (CdProv char(6))
    @nazione      VARCHAR(6)   = NULL, -- Matches "nazione" (CdNazioni char(6))
    @cellulare    VARCHAR(16)  = NULL, -- Matches "cellulare" (Cellulare varchar(16))
    @email        VARCHAR(100) = NULL  -- Matches "email" (Email)
AS
BEGIN
    SET NOCOUNT ON;

    ----------------------------------------------------------------
    -- NORMALIZZAZIONE INPUT
    ----------------------------------------------------------------
    SET @store     = NULLIF(LTRIM(RTRIM(@store)), '');
    SET @nome      = NULLIF(LTRIM(RTRIM(@nome)), '');
    SET @cognome   = NULLIF(LTRIM(RTRIM(@cognome)), '');
    SET @email     = NULLIF(LOWER(LTRIM(RTRIM(@email))), '');
    SET @indirizzo = NULLIF(LTRIM(RTRIM(@indirizzo)), '');
    SET @localita  = NULLIF(LTRIM(RTRIM(@localita)), '');
    SET @cap       = NULLIF(LTRIM(RTRIM(@cap)), '');
    SET @provincia = NULLIF(LTRIM(RTRIM(@provincia)), '');
    SET @nazione   = NULLIF(LTRIM(RTRIM(@nazione)), '');
    SET @cellulare = NULLIF(LTRIM(RTRIM(@cellulare)), '');

    -- Mappatura @store a @CdNE interno se necessario
    DECLARE @CdNE VARCHAR(6) = @store;

    ----------------------------------------------------------------
    -- VALIDAZIONE DATI OBBLIGATORI
    ----------------------------------------------------------------
    IF @email IS NULL
    BEGIN
        SELECT '{"status":"ERROR","message":"Email obbligatoria"}' AS response;
        RETURN;
    END

    IF @nome IS NULL
    BEGIN
        SELECT '{"status":"ERROR","message":"Nome obbligatorio"}' AS response;
        RETURN;
    END

    IF @cognome IS NULL
    BEGIN
        SELECT '{"status":"ERROR","message":"Cognome obbligatorio"}' AS response;
        RETURN;
    END

    ----------------------------------------------------------------
    -- CONVERSIONE DATA NASCITA (ddMMyyyy -> Date)
    ----------------------------------------------------------------
    DECLARE @DataNascitaDate SMALLDATETIME = NULL;
    IF @data_nascita IS NOT NULL AND LEN(@data_nascita) = 8
    BEGIN
        BEGIN TRY
            -- ddMMyyyy -> yyyy-MM-dd
            SET @DataNascitaDate = CONVERT(
                SMALLDATETIME,
                SUBSTRING(@data_nascita,5,4) + '-' + -- yyyy
                SUBSTRING(@data_nascita,3,2) + '-' + -- MM
                SUBSTRING(@data_nascita,1,2),        -- dd
                120
            );
        END TRY
        BEGIN CATCH
            SET @DataNascitaDate = NULL;
        END CATCH
    END

    ----------------------------------------------------------------
    -- VERIFICA UTENTE ESISTENTE
    ----------------------------------------------------------------
    DECLARE @ExistingId INT = NULL;
    DECLARE @CdFidelity VARCHAR(20) = NULL;

    SELECT 
        @ExistingId = Id,
        @CdFidelity = CdNeFidelity
    FROM NEFidelity
    WHERE LOWER(LTRIM(RTRIM(Email))) = @email;

    ----------------------------------------------------------------
    -- UPDATE / INSERT
    ----------------------------------------------------------------
    BEGIN TRY
        IF @ExistingId IS NOT NULL
        BEGIN
            -- UPDATE utente esistente
            UPDATE NEFidelity
            SET
                CdNE = @CdNE,
                Nome = @nome,
                Cognome = @cognome,
                Sesso = @sesso,
                DataNascita = @DataNascitaDate,
                Indirizzo = @indirizzo,
                Localita = @localita,
                CdCap = @cap,
                CdProv = @provincia,
                CdNazioni = @nazione,
                Cellulare = @cellulare,
                Tipo = @tipo,
                Attiva = 1
                -- DataAggiornamento non esiste nello schema fornito
            WHERE Id = @ExistingId;

            SELECT CONCAT(
                '{"codice_fidelity":"', @CdFidelity,
                '","status":"SUCCESS","action":"UPDATED"}'
            ) AS response;
        END
        ELSE
        BEGIN
            -- GENERA nuovo codice fidelity
            DECLARE @Today VARCHAR(8) = CONVERT(VARCHAR(8), GETDATE(), 112);
            DECLARE @Seq INT = 0;

            -- Logica incremento sequenza giornaliera
            SELECT @Seq = ISNULL(MAX(CAST(RIGHT(CdNeFidelity,3) AS INT)),0)
            FROM NEFidelity
            WHERE CdNeFidelity LIKE 'FID' + @Today + '%';

            SET @Seq = @Seq + 1;
            SET @CdFidelity = 'FID' + @Today + RIGHT('000' + CAST(@Seq AS VARCHAR(3)), 3);

            -- INSERT nuovo utente
            INSERT INTO NEFidelity (
                CdNE, CdNeFidelity, Nome, Cognome, Sesso, DataNascita,
                Indirizzo, Localita, CdCap, CdProv, CdNazioni,
                Cellulare, Email, Tipo, Attiva,
                Punti_Iniziali, Punti_Totali, Punti_Stornati, DataInizio
            )
            VALUES (
                @CdNE, @CdFidelity, @nome, @cognome, @sesso, @DataNascitaDate,
                @indirizzo, @localita, @cap, @provincia, @nazione,
                @cellulare, @email, @tipo, 1,
                0, 0, 0, GETDATE()
            );

            SELECT CONCAT(
                '{"codice_fidelity":"', @CdFidelity,
                '","status":"SUCCESS","action":"CREATED"}'
            ) AS response;
        END
    END TRY
    BEGIN CATCH
        SELECT CONCAT(
            '{"status":"ERROR","message":"', REPLACE(ERROR_MESSAGE(), '"', ''''),
            '","error_number":', ERROR_NUMBER(), '}'
        ) AS response;
    END CATCH
END
GO

--------------------------------------------------------------------------------
-- 2. xTSP_API_Get_Fidelity_ByEmail
-- Parameters matching C# SendApiService.GetUserByEmailAsync ("Email")
--------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[xTSP_API_Get_Fidelity_ByEmail]
    @Email VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @JsonResult NVARCHAR(MAX);

    SET @JsonResult = (
        SELECT 
            LTRIM(RTRIM(CdNeFidelity)) AS codice_fidelity,
            LTRIM(RTRIM(CdNE)) AS cd_ne,
            LTRIM(RTRIM(CdNE)) AS store,
            LTRIM(RTRIM(Nome)) AS nome,
            LTRIM(RTRIM(Cognome)) AS cognome,
            LTRIM(RTRIM(Email)) AS email,
            LTRIM(RTRIM(Cellulare)) AS cellulare,
            LTRIM(RTRIM(Indirizzo)) AS indirizzo,
            LTRIM(RTRIM(Localita)) AS localita,
            LTRIM(RTRIM(CdCap)) AS cap,
            LTRIM(RTRIM(CdProv)) AS provincia,
            LTRIM(RTRIM(CdNazioni)) AS nazione,
            Sesso AS sesso,
            CONCAT(CONVERT(VARCHAR(10), DataNascita, 120), 'T00:00:00') AS data_nascita
        FROM NEFidelity
        WHERE LOWER(Email) = LOWER(LTRIM(RTRIM(@Email)))
        FOR JSON PATH
    );

    IF @JsonResult IS NULL
    BEGIN
        SELECT '{"dataset":[]}' AS response;
    END
    ELSE
    BEGIN
        SELECT '{"dataset":' + @JsonResult + '}' AS response;
    END
END
GO

--------------------------------------------------------------------------------
-- 3. xTSP_API_Get_Fidelity_ByCodice
-- Parameters matching C# SendApiService.GetUserByCdFidelityAsync ("@Codice_Fidelity")
--------------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[xTSP_API_Get_Fidelity_ByCodice]
    @Codice_Fidelity VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @JsonResult NVARCHAR(MAX);

    SET @JsonResult = (
        SELECT 
            LTRIM(RTRIM(CdNeFidelity)) AS codice_fidelity,
            LTRIM(RTRIM(CdNE)) AS cd_ne,
            LTRIM(RTRIM(CdNE)) AS store,
            LTRIM(RTRIM(Nome)) AS nome,
            LTRIM(RTRIM(Cognome)) AS cognome,
            LTRIM(RTRIM(Email)) AS email,
            LTRIM(RTRIM(Cellulare)) AS cellulare,
            LTRIM(RTRIM(Indirizzo)) AS indirizzo,
            LTRIM(RTRIM(Localita)) AS localita,
            LTRIM(RTRIM(CdCap)) AS cap,
            LTRIM(RTRIM(CdProv)) AS provincia,
            LTRIM(RTRIM(CdNazioni)) AS nazione,
            Sesso AS sesso,
            CONCAT(CONVERT(VARCHAR(10), DataNascita, 120), 'T00:00:00') AS data_nascita
        FROM NEFidelity
        WHERE CdNeFidelity = @Codice_Fidelity
        FOR JSON PATH
    );

    IF @JsonResult IS NULL
    BEGIN
         SELECT '{"dataset":[]}' AS response;
    END
    ELSE
    BEGIN
        SELECT '{"dataset":' + @JsonResult + '}' AS response;
    END
END
GO
