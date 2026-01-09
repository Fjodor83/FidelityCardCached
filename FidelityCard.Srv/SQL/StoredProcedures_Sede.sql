-- =============================================
-- STORED PROCEDURES PER API FIDELITY CARD
-- Da creare nel database della sede (NEFidelity)
-- =============================================

-- =============================================
-- 1. Cerca utente per EMAIL
-- Nome: xTSP_API_Get_Fidelity_ByEmail
-- =============================================
CREATE PROCEDURE [dbo].[xTSP_API_Get_Fidelity_ByEmail]
    @email VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        codice_fidelity,
        nome,
        cognome,
        email,
        cellulare,
        indirizzo,
        localita,
        cap,
        provincia,
        nazione,
        sesso,
        data_nascita,
        cd_ne as store
    FROM NEFidelity
    WHERE LOWER(LTRIM(RTRIM(email))) = LOWER(LTRIM(RTRIM(@email)))
END
GO


-- =============================================
-- 2. Cerca utente per CODICE FIDELITY
-- Nome: xTSP_API_Get_Fidelity_ByCodice
-- =============================================
CREATE PROCEDURE [dbo].[xTSP_API_Get_Fidelity_ByCodice]
    @codice_fidelity VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        codice_fidelity,
        nome,
        cognome,
        email,
        cellulare,
        indirizzo,
        localita,
        cap,
        provincia,
        nazione,
        sesso,
        data_nascita,
        cd_ne as store
    FROM NEFidelity
    WHERE codice_fidelity = @codice_fidelity
END
GO


-- =============================================
-- NOTE:
-- 
-- Queste stored procedure devono essere create nel 
-- database della sede centrale (quello configurato 
-- in SedeSettings:DbNameSede).
--
-- Assicurarsi che i nomi delle colonne corrispondano
-- a quelli effettivi nella tabella NEFidelity.
--
-- La risposta dell'API sede deve seguire questo formato:
-- {
--   "response": [
--     {
--       "dataset": [
--         {
--           "codice_fidelity": "2026000093",
--           "nome": "Mario",
--           "cognome": "Rossi",
--           "email": "mario.rossi@email.com",
--           "cellulare": "3331234567",
--           "indirizzo": "Via Roma 1",
--           "localita": "Milano",
--           "cap": "20100",
--           "provincia": "MI",
--           "nazione": "IT",
--           "sesso": "M",
--           "data_nascita": "1990-01-15",
--           "store": "NE001"
--         }
--       ]
--     }
--   ]
-- }
-- =============================================
