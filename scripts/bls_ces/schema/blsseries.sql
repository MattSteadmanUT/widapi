USE [WID]
GO

-- Staging table for BLS flat file downloads.
-- This table is truncated at the start of every pipeline run.
-- Do not use it as a persistent data store.
CREATE TABLE [dbo].[blsseries](
    [seriesId]       [varchar](30)  NULL,
    [year]           [varchar](4)   NULL,
    [period]         [varchar](3)   NULL,
    [value]          [decimal](18,1) NULL,
    [footnote_codes] [varchar](10)  NULL
)
GO
