USE [WID]
GO

-- =============================================
-- Author:
-- Create date: 
-- Description: Load the CES table from blsseries
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[powershell_loadCes]
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @series char(2);
	SELECT TOP 1 @series= substring(series_id,1,2) FROM blsseries;
	IF (@series NOT IN ('CE','SM'))
		BEGIN
			DECLARE @msg varchar(100) = 'Invalid series code '+@series;
			THROW 50001,@msg,1;
		END;
	DECLARE @start_series int=CASE WHEN @series='CE' THEN 4 ELSE 11 END;
	DECLARE @start_stat int=@start_series+8;

	;WITH cte AS(
		SELECT DISTINCT CASE WHEN @series='CE' THEN '00' ELSE substring(series_id,4,2) END as stfips,
			CASE
				WHEN @series='CE' THEN '00'
				WHEN substring(series_id,6,5)='00000' THEN '01'
				ELSE '31'
			END as areatype,
			CASE WHEN SUBSTRING(series_id,3,1)='S' THEN '1' ELSE '0' END AS adjusted FROM blsseries)
	DELETE FROM ces FROM
		ces l JOIN cte c ON c.stfips=l.stfips and c.areatype=l.areatype AND c.adjusted=l.adjusted

	INSERT ces(stfips,areatype,area,periodyear,periodtype,period,seriescode,adjusted,prelim,benchmark,
		empces,empprodwrk,empfemale,hours,earnings,hourearn,supprecord,supphe,supppw,
		suppfem,hoursallwrkr,earningsallwrkr,hourearnallwrkr,suppheallwrkr)
	SELECT CASE WHEN substring(series_id,1,2)='CE' THEN '00' ELSE substring(series_id,4,2) END as stfips,
		CASE
			WHEN @series='CE' THEN '00'
			WHEN substring(series_id,6,5)='00000' THEN '01'
			ELSE '31'
		END as areatype,
		CASE
			WHEN @series ='CE' THEN '000000'
			WHEN substring(series_id,6,5)='00000' THEN '0000'+substring(series_id,4,2)
			ELSE '0'+substring(series_id,6,5)
		END as area,
		year as periodyear,
		CASE WHEN substring(period,2,2)='13' THEN '01' ELSE '03' END as periodtype,
		CASE WHEN substring(period,2,2)='13' THEN '00' ELSE substring(period,2,2) END as period,
		substring(series_id,@start_series,8) as seriescode,
		CASE WHEN SUBSTRING(series_id,3,1)='S' THEN '1' ELSE '0' END AS adjusted,
		CASE WHEN footnote_codes='P' THEN 1 ELSE 0 END AS prelim,'0000' AS benchmark,
		empces*1000,empprodwrk*1000,empfemale*1000,hours,earnings,hourearn,'0' as supprecord,'0' as supphe,'0' as supppw,
		'0' as suppfem,hoursallwrkr,earningsallwrkr,hourearnallwrkr,'0' as suppheallwrkr
	FROM
	(
	  SELECT SUBSTRING(series_id,1,@start_stat-1) as series_id,year, period, footnote_codes,value,
		  CASE substring(series_id,@start_stat,2)
			WHEN '01' THEN 'empces'
			WHEN '02' THEN 'hoursallwrkr'
			WHEN '03' THEN 'hourearnallwrkr'
			WHEN '06' THEN 'empprodwrk'
			WHEN '07' THEN 'hours'
			WHEN '08' THEN 'hourearn'
			WHEN '10' THEN 'empfemale'
			WHEN '11' THEN 'earningsallwrkr'
			WHEN '30' THEN 'earnings'
		  END as stat
	  FROM blsseries
	  WHERE substring(series_id,@start_stat,2) <= '30'	-- Stats C1, C2, C3 & RR never preliminary but other stats for same period can be, caused dupkey error 01/17/25
	) AS TableToPivot
	PIVOT
	(
	  MAX(value)
	  FOR stat IN (empces,empprodwrk,empfemale,hours,earnings,hourearn,hoursallwrkr,
		earningsallwrkr,hourearnallwrkr)
	) AS PivotTable

	-- FK check here instead of schema to avoid breaking the load
	IF EXISTS(SELECT DISTINCT stfips,seriescode
			FROM ces a
			WHERE stfips=CASE WHEN @series='CE' THEN '00' ELSE '37' END AND NOT EXISTS(SELECT * FROM cescode WHERE stfips=a.stfips AND seriescode=a.seriescode))
		THROW 51000, 'Data loaded successfully, cescode records need to be added.', 1;

END
GO
