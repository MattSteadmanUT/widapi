-- =============================================================================
-- 009_fix_national_area_codes.sql
-- Migrates national-level rows that were inserted with 6-char area codes
-- (stored as "000000 " in char(7)) to the correct "0000000" form.
-- =============================================================================

-- LaborForce: merge old 6-char rows into correct 7-char rows via upsert then delete
INSERT INTO laborforce
    (stfips, areatype, areatypeversion, area, periodyear, periodtype, period, adjusted,
     laborforce, employed, unemployed, unemprate, supprecord, supprate, prelim)
SELECT
    trim(stfips), trim(areatype), trim(areatypeversion), '0000000',
    periodyear, periodtype, period, adjusted,
    laborforce, employed, unemployed, unemprate, supprecord, supprate, prelim
FROM laborforce
WHERE trim(area) != '0000000'
  AND trim(area) SIMILAR TO '0{5,7}'
ON CONFLICT (stfips, areatype, areatypeversion, area, periodyear, periodtype, period, adjusted)
DO UPDATE SET
    laborforce = EXCLUDED.laborforce,
    employed   = EXCLUDED.employed,
    unemployed = EXCLUDED.unemployed,
    unemprate  = EXCLUDED.unemprate,
    supprecord = EXCLUDED.supprecord,
    supprate   = EXCLUDED.supprate,
    prelim     = EXCLUDED.prelim;

DELETE FROM laborforce
WHERE trim(area) != '0000000'
  AND trim(area) SIMILAR TO '0{5,7}';

-- CES: same treatment
INSERT INTO ces
    (stfips, areatype, areatypeversion, area, periodyear, periodtype, period, adjusted,
     seriescodetype, seriescode, empces, empproductionworkers, hoursperweek,
     earningsperweek, earningsperhour, avgweeklyearningspctchange,
     supprecord, supphoursearnings, suppprodworkers, prelim)
SELECT
    trim(stfips), trim(areatype), trim(areatypeversion), '0000000',
    periodyear, periodtype, period, adjusted,
    trim(seriescodetype), trim(seriescode), empces, empproductionworkers, hoursperweek,
    earningsperweek, earningsperhour, avgweeklyearningspctchange,
    supprecord, supphoursearnings, suppprodworkers, prelim
FROM ces
WHERE trim(area) != '0000000'
  AND trim(area) SIMILAR TO '0{5,7}'
ON CONFLICT (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
             seriescodetype, seriescode, adjusted)
DO UPDATE SET
    empces = EXCLUDED.empces,
    empproductionworkers = EXCLUDED.empproductionworkers,
    hoursperweek = EXCLUDED.hoursperweek,
    earningsperweek = EXCLUDED.earningsperweek,
    earningsperhour = EXCLUDED.earningsperhour,
    supprecord = EXCLUDED.supprecord;

DELETE FROM ces
WHERE trim(area) != '0000000'
  AND trim(area) SIMILAR TO '0{5,7}';

-- Industry
INSERT INTO industry
    (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
     ownership, codetype, indcode, avgmonthlyemp, totalwages, taxablewages,
     contributions, weeklywage, empcount, highempq, lowempq, supprecord, prelim)
SELECT
    trim(stfips), trim(areatype), trim(areatypeversion), '0000000',
    periodyear, periodtype, period,
    trim(ownership), trim(codetype), trim(indcode),
    avgmonthlyemp, totalwages, taxablewages,
    contributions, weeklywage, empcount, highempq, lowempq, supprecord, prelim
FROM industry
WHERE trim(area) != '0000000'
  AND trim(area) SIMILAR TO '0{5,7}'
ON CONFLICT (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
             ownership, codetype, indcode)
DO UPDATE SET
    avgmonthlyemp = EXCLUDED.avgmonthlyemp,
    totalwages = EXCLUDED.totalwages,
    supprecord = EXCLUDED.supprecord;

DELETE FROM industry
WHERE trim(area) != '0000000'
  AND trim(area) SIMILAR TO '0{5,7}';

-- IOWage
INSERT INTO iowage
    (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
     indcodetype, indcode, occcodetype, occcode, wagesource, ratetype,
     empcount, pct10, pct25, medianwage, meanwage, pct75, pct90, meanhourly, annualmean, supprecord)
SELECT
    trim(stfips), trim(areatype), trim(areatypeversion), '0000000',
    periodyear, periodtype, period,
    trim(indcodetype), trim(indcode), trim(occcodetype), trim(occcode),
    trim(wagesource), trim(ratetype),
    empcount, pct10, pct25, medianwage, meanwage, pct75, pct90, meanhourly, annualmean, supprecord
FROM iowage
WHERE trim(area) != '0000000'
  AND trim(area) SIMILAR TO '0{5,7}'
ON CONFLICT (stfips, areatype, areatypeversion, area, periodyear, periodtype, period,
             indcodetype, indcode, occcodetype, occcode)
DO UPDATE SET
    empcount = EXCLUDED.empcount,
    medianwage = EXCLUDED.medianwage,
    meanwage = EXCLUDED.meanwage;

DELETE FROM iowage
WHERE trim(area) != '0000000'
  AND trim(area) SIMILAR TO '0{5,7}';

-- ProjectionsMatrix
INSERT INTO projectionsmatrix
    (stfips, areatype, areatypeversion, area, projectionsperiod,
     indcodetype, indcode, occcodetype, occcode,
     baseyearemp, projectedemp, change, pctchange, openings, supprecord)
SELECT
    trim(stfips), trim(areatype), trim(areatypeversion), '0000000',
    trim(projectionsperiod),
    trim(indcodetype), trim(indcode), trim(occcodetype), trim(occcode),
    baseyearemp, projectedemp, change, pctchange, openings, supprecord
FROM projectionsmatrix
WHERE trim(area) != '0000000'
  AND trim(area) SIMILAR TO '0{5,7}'
ON CONFLICT (stfips, areatype, areatypeversion, area, projectionsperiod,
             indcodetype, indcode, occcodetype, occcode)
DO UPDATE SET
    baseyearemp = EXCLUDED.baseyearemp,
    projectedemp = EXCLUDED.projectedemp,
    change = EXCLUDED.change,
    pctchange = EXCLUDED.pctchange,
    openings = EXCLUDED.openings;

DELETE FROM projectionsmatrix
WHERE trim(area) != '0000000'
  AND trim(area) SIMILAR TO '0{5,7}';
