USE [WID]
GO

-- Tracks the last successful download for each BLS URL.
-- Used by bls_get_flat_file.ps1 to skip downloads when the remote file has not changed.
CREATE TABLE [dbo].[import_history](
    [id]       [int]          IDENTITY(1,1) NOT NULL,
    [name]     [varchar](500) NOT NULL,   -- The full BLS URL that was downloaded
    [run_date] [datetime]     NOT NULL,
    CONSTRAINT [PK_import_history] PRIMARY KEY CLUSTERED ([id] ASC)
)
GO
