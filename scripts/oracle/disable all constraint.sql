-- =====================================================
-- 01_disable_constraints.sql
-- Disable all foreign key constraints before data migration
-- Based on actual WID 3.0 schema
-- =====================================================

-- Core geographic and reference table constraints
ALTER TABLE AreaTypes DISABLE CONSTRAINT fk_areatypes_statefips;
ALTER TABLE AreaTypeVersions DISABLE CONSTRAINT fk_areatypeversions_areatypes;
ALTER TABLE Geographies DISABLE CONSTRAINT fk_geographies_statefips;
ALTER TABLE Geographies DISABLE CONSTRAINT fk_geographies_geoprecisioncode;
ALTER TABLE Geographies DISABLE CONSTRAINT fk_geographies_areatypeversions;
ALTER TABLE SubGeographies DISABLE CONSTRAINT fk_subgeographies_geographies;
ALTER TABLE SubGeographies DISABLE CONSTRAINT fk_subgeographies_geographiessub;

-- Period and time-related constraints
ALTER TABLE Periods DISABLE CONSTRAINT fk_periods_periodtypes;
ALTER TABLE OccDirectories DISABLE CONSTRAINT fk_occdirectory_occsublevels;
ALTER TABLE OccDirectories DISABLE CONSTRAINT fk_occdirectory_periods;
ALTER TABLE OccDirectories DISABLE CONSTRAINT fk_occdirectory_periodyears;

-- Wage and source constraints
ALTER TABLE WageSources DISABLE CONSTRAINT fk_wagesource_statefips;

-- CES and industry constraints
-- Note: fk_cescodes_statefips is commented out in the creation script
ALTER TABLE CESCodes DISABLE CONSTRAINT fk_cescodes_indcodetypes;

-- CIP and occupation code constraints
ALTER TABLE CIPCodes DISABLE CONSTRAINT fk_cipcode_occtypes;
ALTER TABLE CompleterTypes DISABLE CONSTRAINT fk_completertype_statefips;
ALTER TABLE InstitutionTypes DISABLE CONSTRAINT fk_institutiontype_statefips;

-- NAICS hierarchy constraints
ALTER TABLE NAICSSuperSectors DISABLE CONSTRAINT fk_naicssupersector_naicsdomain;
ALTER TABLE NAICSSectors DISABLE CONSTRAINT fk_naicssector_naicsuper;
ALTER TABLE NAICSCodes DISABLE CONSTRAINT fk_naicscode_flag;
ALTER TABLE NAICSCodes DISABLE CONSTRAINT fk_naicscode_naicslevel;
ALTER TABLE NAICSCodes DISABLE CONSTRAINT fk_naicscode_naicssector;

-- Industry directory constraints
ALTER TABLE IndDirectories DISABLE CONSTRAINT fk_IndDirectories_indsublevels;
ALTER TABLE IndDirectories DISABLE CONSTRAINT fk_IndDirectories_periods;
ALTER TABLE IndDirectories DISABLE CONSTRAINT fk_IndDirectories_periodyears;

-- SOC codes constraints
ALTER TABLE SOCCodes DISABLE CONSTRAINT fk_soccode_flag;
ALTER TABLE SOCCodes DISABLE CONSTRAINT fk_soccode_occtypes;
ALTER TABLE SOCCodes DISABLE CONSTRAINT fk_soccode_soccode;

-- Occupation and industry code constraints
ALTER TABLE OccupationCodes DISABLE CONSTRAINT fk_occcodes_statefips;
ALTER TABLE OccupationCodes DISABLE CONSTRAINT fk_occcodes_occtypes;
ALTER TABLE IndustryCodes DISABLE CONSTRAINT fk_industrycodes_indcodetypes;
ALTER TABLE IndustryCodes DISABLE CONSTRAINT fk_IndustryCodes_statefips;

-- Age group constraints
ALTER TABLE AgeGroups DISABLE CONSTRAINT fk_agegroups_agegrouptypes;

-- Growth and CPI constraints
ALTER TABLE GrowthCodes DISABLE CONSTRAINT fk_growthcodes_statefips;
ALTER TABLE CPISources DISABLE CONSTRAINT fk_cpisource_statefips;

-- CES data table constraints
ALTER TABLE CES DISABLE CONSTRAINT fk_ces_benchmark;
ALTER TABLE CES DISABLE CONSTRAINT fk_ces_cescode;
ALTER TABLE CES DISABLE CONSTRAINT fk_ces_indcodetypes;
ALTER TABLE CES DISABLE CONSTRAINT fk_ces_geographies;
ALTER TABLE CES DISABLE CONSTRAINT fk_ces_periods;
ALTER TABLE CES DISABLE CONSTRAINT fk_ces_periodyears;

-- CPI data constraints
ALTER TABLE CPI DISABLE CONSTRAINT fk_cpi_cpisource;
ALTER TABLE CPI DISABLE CONSTRAINT fk_cpi_cpitype;
ALTER TABLE CPI DISABLE CONSTRAINT fk_cpi_geographies;
ALTER TABLE CPI DISABLE CONSTRAINT fk_cpi_periods;
ALTER TABLE CPI DISABLE CONSTRAINT fk_cpi_periodyears;

-- Employee database constraints
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_annualsalescodes;
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_annualsalesranges;
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_CONTACTPROTITLES;
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_CONTACTTITLE;
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_CREDITCODE;
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_EMPDBINF;
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_EMPSIZEFLAG;
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_EMPSIZERANGE;
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_GEOGRAPHIES;
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_GEOPRECISIONCODE;
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_LOCATIONSTATUS;
ALTER TABLE EmpDB DISABLE CONSTRAINT FK_EMPDB_PRIVATEGOVT;

-- Industry sums constraints
ALTER TABLE IndustrySums DISABLE CONSTRAINT fk_industrysum_geographies;
ALTER TABLE IndustrySums DISABLE CONSTRAINT fk_industrysum_industrycodes;

-- Industry constraints
ALTER TABLE Industry DISABLE CONSTRAINT fk_industry_geographies;
ALTER TABLE Industry DISABLE CONSTRAINT fk_industry_industrycodes;
ALTER TABLE Industry DISABLE CONSTRAINT fk_industry_ownerships;
ALTER TABLE Industry DISABLE CONSTRAINT fk_industry_periods;
ALTER TABLE Industry DISABLE CONSTRAINT fk_industry_periodyears;

-- Projections matrix constraints
ALTER TABLE ProjectionsMatrix DISABLE CONSTRAINT fk_projectionsmatrix_geographies;
ALTER TABLE ProjectionsMatrix DISABLE CONSTRAINT fk_projectionsmatrix_growthcodes;
ALTER TABLE ProjectionsMatrix DISABLE CONSTRAINT FK_PROJECTIONSMATRIX_INDDIRECTORIES;
ALTER TABLE ProjectionsMatrix DISABLE CONSTRAINT FK_PROJECTIONSMATRIX_OCCDIRECTORIES;
ALTER TABLE ProjectionsMatrix DISABLE CONSTRAINT fk_projectionsmatrix_periods;
ALTER TABLE ProjectionsMatrix DISABLE CONSTRAINT fk_projectionsmatrix_periodyears;

-- Labor force constraints
ALTER TABLE LaborForce DISABLE CONSTRAINT fk_laborforce_benchmark;
ALTER TABLE LaborForce DISABLE CONSTRAINT fk_laborforce_geographies;
ALTER TABLE LaborForce DISABLE CONSTRAINT fk_laborforce_periods;
ALTER TABLE LaborForce DISABLE CONSTRAINT fk_laborforce_periodyears;

-- License authority constraints
ALTER TABLE LicenseAuthorities DISABLE CONSTRAINT fk_licenseauthorities_geographies;
ALTER TABLE LicenseAuthorities DISABLE CONSTRAINT fk_licenseauthorities_geoprecisioncode;

-- License constraints
ALTER TABLE License DISABLE CONSTRAINT fk_license_liccontinuingedu;
ALTER TABLE License DISABLE CONSTRAINT fk_license_licenseactivestatus;
ALTER TABLE License DISABLE CONSTRAINT fk_license_licenseauthorities;
ALTER TABLE License DISABLE CONSTRAINT fk_license_licensecertification;
ALTER TABLE License DISABLE CONSTRAINT fk_license_licensecriminal;
ALTER TABLE License DISABLE CONSTRAINT fk_license_licenseeducation;
ALTER TABLE License DISABLE CONSTRAINT fk_license_licenseexams;
ALTER TABLE License DISABLE CONSTRAINT fk_license_licenseexperience;
ALTER TABLE License DISABLE CONSTRAINT fk_license_licensephysicalreqs;
ALTER TABLE License DISABLE CONSTRAINT fk_license_licensetypes;
ALTER TABLE License DISABLE CONSTRAINT fk_license_licenseveteran;

-- License history constraints
ALTER TABLE LicenseHistory DISABLE CONSTRAINT fk_licensehistory_geographies;
ALTER TABLE LicenseHistory DISABLE CONSTRAINT fk_licensehistory_license;
ALTER TABLE LicenseHistory DISABLE CONSTRAINT fk_licensehistory_licensenumbertype;
ALTER TABLE LicenseHistory DISABLE CONSTRAINT fk_licensehistory_periods;
ALTER TABLE LicenseHistory DISABLE CONSTRAINT fk_licensehistory_periodyears;

-- IO Wage constraints
ALTER TABLE IOWage DISABLE CONSTRAINT fk_iowage_geographies;
ALTER TABLE IOWage DISABLE CONSTRAINT fk_iowage_industrycodes;
ALTER TABLE IOWage DISABLE CONSTRAINT fk_iowage_occcodes;
ALTER TABLE IOWage DISABLE CONSTRAINT fk_iowage_periods;
ALTER TABLE IOWage DISABLE CONSTRAINT fk_iowage_periodyears;
ALTER TABLE IOWage DISABLE CONSTRAINT fk_iowage_wageratetypes;
ALTER TABLE IOWage DISABLE CONSTRAINT fk_iowage_wagesource;

-- Schools constraints
ALTER TABLE Schools DISABLE CONSTRAINT fk_schools_geographies;
ALTER TABLE Schools DISABLE CONSTRAINT fk_schools_geoprecisioncode;
ALTER TABLE Schools DISABLE CONSTRAINT fk_schools_institutiontype;

-- Program completers constraints
ALTER TABLE ProgramCompleters DISABLE CONSTRAINT fk_programcompleters_completertype;
ALTER TABLE ProgramCompleters DISABLE CONSTRAINT fk_programcompleters_geographies;
ALTER TABLE ProgramCompleters DISABLE CONSTRAINT fk_programcompleters_occcodes;
ALTER TABLE ProgramCompleters DISABLE CONSTRAINT fk_programcompleters_periods;
ALTER TABLE ProgramCompleters DISABLE CONSTRAINT fk_programcompleters_periodyears;
ALTER TABLE ProgramCompleters DISABLE CONSTRAINT fk_programcompleters_schools;

-- Programs constraints (some are commented out in creation script)
ALTER TABLE Programs DISABLE CONSTRAINT fk_programs_completertype;
ALTER TABLE Programs DISABLE CONSTRAINT fk_programs_geographies;
ALTER TABLE Programs DISABLE CONSTRAINT fk_programs_occcodes;
ALTER TABLE Programs DISABLE CONSTRAINT fk_programs_occcodescip;
ALTER TABLE Programs DISABLE CONSTRAINT fk_programs_schools;

-- UI Claims constraints
ALTER TABLE UIClaims DISABLE CONSTRAINT fk_uiclaims_agegroups;
ALTER TABLE UIClaims DISABLE CONSTRAINT fk_uiclaims_ethnicitycodes;
ALTER TABLE UIClaims DISABLE CONSTRAINT fk_uiclaims_gender;
ALTER TABLE UIClaims DISABLE CONSTRAINT fk_uiclaims_geographies;
ALTER TABLE UIClaims DISABLE CONSTRAINT fk_uiclaims_industrycodes;
ALTER TABLE UIClaims DISABLE CONSTRAINT fk_uiclaims_occcodes;
ALTER TABLE UIClaims DISABLE CONSTRAINT fk_uiclaims_periods;
ALTER TABLE UIClaims DISABLE CONSTRAINT fk_uiclaims_periodyears;
ALTER TABLE UIClaims DISABLE CONSTRAINT fk_uiclaims_racecodes;

-- Cross-reference table constraints
ALTER TABLE OccupationXOccupation DISABLE CONSTRAINT fk_occxocc_occcodes;
ALTER TABLE OccupationXOccupation DISABLE CONSTRAINT fk_occxocc_occcodes2;

ALTER TABLE LicenseXOcc DISABLE CONSTRAINT fk_licensexocc_license;
ALTER TABLE LicenseXOcc DISABLE CONSTRAINT fk_licensexocc_occcodes;

ALTER TABLE MatrixXInd DISABLE CONSTRAINT fk_matrixxind_indsublevels;
ALTER TABLE MatrixXInd DISABLE CONSTRAINT fk_matrixxind_industrycodes;
ALTER TABLE MatrixXInd DISABLE CONSTRAINT fk_matrixxind_periods;
ALTER TABLE MatrixXInd DISABLE CONSTRAINT fk_matrixxind_periodyears;

ALTER TABLE MatrixXOcc DISABLE CONSTRAINT fk_matrixxocc_occcodes;
ALTER TABLE MatrixXOcc DISABLE CONSTRAINT fk_matrixxocc_occsublevels;
ALTER TABLE MatrixXOcc DISABLE CONSTRAINT fk_matrixxocc_periods;
ALTER TABLE MatrixXOcc DISABLE CONSTRAINT fk_matrixxocc_periodyears;

COMMIT;

-- Display completion message
SELECT 'All foreign key constraints have been disabled.' AS status FROM dual;