/****** DROP TABLES IN REVERSE ORDER ******/
-- Drop tables in reverse order to handle foreign key dependencies
DROP TABLE MatrixXOcc;
DROP TABLE MatrixXInd;
DROP TABLE LicenseXOcc;
DROP TABLE OccupationXOccupation;
DROP TABLE UIClaims;
DROP TABLE Programs;
DROP TABLE ProgramCompleters;
DROP TABLE Schools;
DROP TABLE IOWage;
DROP TABLE LicenseHistory;
DROP TABLE License;
DROP TABLE LicenseAuthorities;
DROP TABLE LicenseVeteran;
DROP TABLE LicenseActiveStatuses;
DROP TABLE LicensePhysicalReqs;
DROP TABLE LicenseCriminal;
DROP TABLE LicenseExperience;
DROP TABLE LicenseCertifications;
DROP TABLE LicenseContinuingEdu;
DROP TABLE LicenseEducation;
DROP TABLE LicenseExams;
DROP TABLE LicenseTypes;
DROP TABLE LaborForce;
DROP TABLE ProjectionsMatrix;
DROP TABLE Industry;
DROP TABLE IndustrySums;
DROP TABLE EmpDB;
DROP TABLE EmpDBInf;
DROP TABLE CPI;
DROP TABLE CES;
DROP TABLE ContactTitles;
DROP TABLE ContactProTitles;
DROP TABLE PrivateGovt;
DROP TABLE LocationStatuses;
DROP TABLE LicenseNumberTypes;
DROP TABLE GrowthCodes;
DROP TABLE EmpSizeRange;
DROP TABLE EmpSizeFlag;
DROP TABLE CreditCodes;
DROP TABLE CPISources;
DROP TABLE CPITypes;
DROP TABLE Benchmark;
DROP TABLE AnnualSalesRanges;
DROP TABLE AnnualSalesCodes;
DROP TABLE AgeGroups;
DROP TABLE AgeGroupTypes;
DROP TABLE IndustryCodes;
DROP TABLE OccupationCodes;
DROP TABLE SOCCodes;
DROP TABLE IndDirectories;
DROP TABLE RaceCodes;
DROP TABLE EthnicityCodes;
DROP TABLE NAICSCodes;
DROP TABLE NAICSLevels;
DROP TABLE NAICSSectors;
DROP TABLE NAICSSuperSectors;
DROP TABLE NAICSDomains;
DROP TABLE InstitutionTypes;
DROP TABLE InstitutionOwnerships;
DROP TABLE Genders;
DROP TABLE CompleterTypes;
DROP TABLE CIPCodes;
DROP TABLE CESCodes;
DROP TABLE Ownerships;
DROP TABLE IndSubLevels;
DROP TABLE WageSources;
DROP TABLE WageRateTypes;
DROP TABLE OccDirectories;
DROP TABLE OccSubLevels;
DROP TABLE IndCodeTypes;
DROP TABLE OccCodeTypes;
DROP TABLE Periods;
DROP TABLE PeriodTypes;
DROP TABLE PeriodYears;
DROP TABLE CodeFlags;
DROP TABLE SubGeographies;
DROP TABLE Geographies;
DROP TABLE GeoPrecisionCodes;
DROP TABLE AreaTypeVersions;
DROP TABLE AreaTypes;
DROP TABLE StateFips;

/****** Object:  Table StateFips 1 ******/
CREATE TABLE StateFips (
    StFips CHAR(2) NOT NULL,
    StateName VARCHAR2(20),
    Abbreviation CHAR(2),
    CONSTRAINT PK_STATEFIPS PRIMARY KEY (StFips)
);

/****** Object:  Table AreaTypes 2 ******/
CREATE TABLE AreaTypes (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeName VARCHAR2(200) NOT NULL,
    CONSTRAINT PK_AreaTypes PRIMARY KEY (StFips, AreaType),
    CONSTRAINT fk_areatypes_statefips FOREIGN KEY (StFips) REFERENCES StateFips (StFips)
);

/****** Object:  Table AreaTypeVersions 3 ******/
CREATE TABLE AreaTypeVersions (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    AreaTypeVersionNotes VARCHAR2(500) NOT NULL,
    CONSTRAINT PK_AREATYPEVERSION PRIMARY KEY (StFips, AreaType, AreaTypeVersion),
    CONSTRAINT fk_areatypeversions_areatypes FOREIGN KEY (StFips, AreaType) REFERENCES AreaTypes (StFips, AreaType)
);

/****** Object:  Table GeoPrecisionCodes 4 ******/
CREATE TABLE GeoPrecisionCodes (
    GeoPrecisionCode CHAR(1) NOT NULL,
    GeoPrecisionDesc VARCHAR2(40),
    CONSTRAINT PK_GEOPRECISIONCODE PRIMARY KEY (GeoPrecisionCode)
);

/****** Object:  Table Geographies 5 ******/
CREATE TABLE Geographies (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    AreaName VARCHAR2(100) NOT NULL,
    AreaDesc CLOB,
    Latitude NUMBER(11,6),
    Longitude NUMBER(11,6),
    GeoPrecisionCode CHAR(1),
    CONSTRAINT PK_GEOGRAPHIES PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_geographies_statefips FOREIGN KEY (StFips) REFERENCES StateFips (StFips),
    CONSTRAINT fk_geographies_geoprecisioncode FOREIGN KEY (GeoPrecisionCode) REFERENCES GeoPrecisionCodes (GeoPrecisionCode),
    CONSTRAINT fk_geographies_areatypeversions FOREIGN KEY (StFips, AreaType, AreaTypeVersion) REFERENCES AreaTypeVersions (StFips, AreaType, AreaTypeVersion)
);

/****** Object:  Table SubGeographies 6 ******/
CREATE TABLE SubGeographies (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    SubStFips CHAR(2) NOT NULL,
    SubAreaType CHAR(2) NOT NULL,
    SubAreaTypeVersion CHAR(4) NOT NULL,
    SubArea CHAR(6) NOT NULL,
    CONSTRAINT PK_SUBGEOGRAPHIES PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, SubStFips, SubAreaType, SubAreaTypeVersion, SubArea),
    CONSTRAINT fk_subgeographies_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_subgeographies_geographiessub FOREIGN KEY (SubStFips, SubAreaType, SubAreaTypeVersion, SubArea) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area)
);

/****** Object:  Table CodeFlags 7 ******/
CREATE TABLE CodeFlags (
    Flag CHAR(1) NOT NULL,
    FlagDesc VARCHAR2(100),
    CONSTRAINT PK_CODEFLAGS PRIMARY KEY (Flag)
);

/****** Object:  Table PeriodYears 8 ******/
CREATE TABLE PeriodYears (
    PeriodYear CHAR(4) NOT NULL,
    CONSTRAINT PK_PERIODYEARS PRIMARY KEY (PeriodYear)
);

/****** Object:  Table PeriodTypes 9 ******/
CREATE TABLE PeriodTypes (
    PeriodType CHAR(2) NOT NULL,
    PeriodTypeDesc VARCHAR2(40),
    CONSTRAINT PK_PERIODTYPES PRIMARY KEY (PeriodType)
);

/****** Object:  Table Periods 10 ******/
CREATE TABLE Periods (
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    PeriodDesc VARCHAR2(25),
    CONSTRAINT PK_PERIODS PRIMARY KEY (PeriodType, Period),
    CONSTRAINT fk_periods_periodtypes FOREIGN KEY (PeriodType) REFERENCES PeriodTypes (PeriodType)
);

/****** Object:  Table OccCodeTypes 11 ******/
CREATE TABLE OccCodeTypes (
    CodeType CHAR(2) NOT NULL,
    CodeTypeDesc VARCHAR2(40),
    CONSTRAINT PK_OCCCODETYPES PRIMARY KEY (CodeType)
);

/****** Object:  Table IndCodeTypes 12 ******/
CREATE TABLE IndCodeTypes (
    CodeType CHAR(2) NOT NULL,
    CodeTypeDesc VARCHAR2(40),
    CONSTRAINT PK_INDCODETYPES PRIMARY KEY (CodeType)
);

/****** Object:  Table OccSubLevels 20 ******/
CREATE TABLE OccSubLevels (
    SubTotal CHAR(1) NOT NULL,
    SubTotalDesc VARCHAR2(60),
    CONSTRAINT PK_OCCSUBLEVELS PRIMARY KEY (SubTotal)
);

/****** Object:  Table OccDirectories 21 ******/
CREATE TABLE OccDirectories (
    MatrixOccCode CHAR(10) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    ProjectedYear CHAR(4) NOT NULL,
    MatrixOccTitle VARCHAR2(200) NOT NULL,
    SubTotal CHAR(1),
    CONSTRAINT PK_OCCDIRECTORY PRIMARY KEY (MatrixOccCode, PeriodYear, PeriodType, Period, ProjectedYear),
    CONSTRAINT fk_occdirectory_occsublevels FOREIGN KEY (SubTotal) REFERENCES OccSubLevels (SubTotal),
    CONSTRAINT fk_occdirectory_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_occdirectory_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear)
);

/****** Object:  Table WageRateTypes 22 ******/
CREATE TABLE WageRateTypes (
    RateType CHAR(1) NOT NULL,
    RateTypeDesc VARCHAR2(40) NOT NULL,
    CONSTRAINT PK_WAGERATETYPES PRIMARY KEY (RateType)
);

/****** Object:  Table WageSources 24 ******/
CREATE TABLE WageSources (
    StFips CHAR(2) NOT NULL,
    WageSource CHAR(1) NOT NULL,
    WageSourceDesc VARCHAR2(60),
    CONSTRAINT PK_WAGESOURCE PRIMARY KEY (StFips, WageSource),
    CONSTRAINT fk_wagesource_statefips FOREIGN KEY (StFips) REFERENCES StateFips (StFips)
);


/****** Object:  Table IndSubLevels 25 ******/
CREATE TABLE IndSubLevels (
    SubTotal CHAR(1) NOT NULL,
    SubTotalDesc VARCHAR2(60),
    CONSTRAINT PK_INDSUBLEVELS PRIMARY KEY (SubTotal)
);

/****** Object:  Table Ownerships 26 ******/
CREATE TABLE Ownerships (
    Ownership CHAR(2) NOT NULL,
    OwnerTitle VARCHAR2(40) NOT NULL,
    CONSTRAINT PK_OWNERSHIPS PRIMARY KEY (Ownership)
);

/****** Object:  Table CESCodes 29 ******/
CREATE TABLE CESCodes (
    StFips CHAR(2) NOT NULL,
    SeriesCodeType CHAR(2) NOT NULL,
    SeriesCode CHAR(8) NOT NULL,
    SeriesTitle VARCHAR2(100),
    SeriesTitleLong VARCHAR2(200),
    SeriesDesc CLOB,
    SeriesLevel CHAR(1),
    CONSTRAINT PK_CESCODE PRIMARY KEY (StFips, SeriesCodeType, SeriesCode),
    --CONSTRAINT fk_cescodes_statefips FOREIGN KEY (StFips) REFERENCES StateFips (StFips),
    CONSTRAINT fk_cescodes_indcodetypes FOREIGN KEY (SeriesCodeType) REFERENCES IndCodeTypes (CodeType)
    --CONSTRAINT fk_cescodes_serieslevels FOREIGN KEY (SeriesCodeType, SeriesLevel) REFERENCES SeriesLevel (OccCodeType, SeriesLevel)
);

/****** Object:  Table CIPCodes 30 ******/
CREATE TABLE CIPCodes (
    CIPCodeType CHAR(2) NOT NULL,
    CIPCode CHAR(10) NOT NULL,
    CIPTitle VARCHAR2(100) NOT NULL,
    CIPTitleLong VARCHAR2(200),
    CIPDesc CLOB,
    CIPLevel CHAR(1),
    CONSTRAINT PK_CIPCODE PRIMARY KEY (CIPCodeType, CIPCode),
    CONSTRAINT fk_cipcode_occtypes FOREIGN KEY (CIPCodeType) REFERENCES OccCodeTypes (CodeType)
);

/****** Object:  Table CompleterTypes 31 ******/
CREATE TABLE CompleterTypes (
    StFips CHAR(2) NOT NULL,
    CompleterType CHAR(2) NOT NULL,
    CompleterTitle VARCHAR2(40) NOT NULL,
    CompleterDesc CLOB,
    CONSTRAINT PK_COMPLETERTYPE PRIMARY KEY (StFips, CompleterType),
    CONSTRAINT fk_completertype_statefips FOREIGN KEY (StFips) REFERENCES StateFips (StFips)
);

/****** Object:  Table Genders 32 ******/
CREATE TABLE Genders (
    GenderCode CHAR(1) NOT NULL,
    GenderDesc VARCHAR2(12),
    CONSTRAINT PK_GENDERS PRIMARY KEY (GenderCode)
);

/****** Object:  Table InstitutionOwnerships 33 ******/
CREATE TABLE InstitutionOwnerships (
    InstitutionOwnership CHAR(1) NOT NULL,
    InstitutionOwnershipDesc VARCHAR2(40) NOT NULL,
    CONSTRAINT PK_INSTITUTIONOWNERSHIPS PRIMARY KEY (InstitutionOwnership)
);

/****** Object:  Table InstitutionTypes 34 ******/
CREATE TABLE InstitutionTypes (
    StFips CHAR(2) NOT NULL,
    InstitutionType CHAR(2) NOT NULL,
    InstitutionTypeDesc VARCHAR2(50),
    CONSTRAINT PK_INSTITUTIONTYPE PRIMARY KEY (StFips, InstitutionType),
    CONSTRAINT fk_institutiontype_statefips FOREIGN KEY (StFips) REFERENCES StateFips (StFips)
);

/****** Object:  Table NAICSDomains 36 ******/
CREATE TABLE NAICSDomains (
    NAICSDomain CHAR(3) NOT NULL,
    DomainTitle VARCHAR2(25),
    CONSTRAINT PK_NAICSDOMAINS PRIMARY KEY (NAICSDomain)
);

/****** Object:  Table NAICSSuperSectors 37 ******/
CREATE TABLE NAICSSuperSectors (
    NAICSSuper CHAR(4) NOT NULL,
    SuperTitle VARCHAR2(35),
    NAICSDomain CHAR(3),
    CONSTRAINT PK_NAICSSUPERSECTORS PRIMARY KEY (NAICSSuper),
    CONSTRAINT fk_naicssupersector_naicsdomain FOREIGN KEY (NAICSDomain) REFERENCES NAICSDomains (NAICSDomain)
);

/****** Object:  Table NAICSSectors 38 ******/
CREATE TABLE NAICSSectors (
    NAICSSector CHAR(2) NOT NULL,
    SectorDesc VARCHAR2(45),
    SectorDescLong VARCHAR2(120),
    NAICSSuper CHAR(4),
    CONSTRAINT PK_NAICSSECTORS PRIMARY KEY (NAICSSector),
    CONSTRAINT fk_naicssector_naicsuper FOREIGN KEY (NAICSSuper) REFERENCES NAICSSuperSectors (NAICSSuper)
);

/****** Object:  Table NAICSLevels 39 ******/
CREATE TABLE NAICSLevels (
    NAICSLevel CHAR(1) NOT NULL,
    LevelDesc VARCHAR2(40),
    CONSTRAINT PK_NAICSLEVELS PRIMARY KEY (NAICSLevel)
);

/****** Object:  Table NAICSCodes 40 ******/
CREATE TABLE NAICSCodes (
    NAICSCodeType CHAR(2) NOT NULL,
    NAICSCode CHAR(6) NOT NULL,
    NAICSTitle VARCHAR2(100),
    NAICSTitleLong VARCHAR2(200),
    NAICSTitleDesc CLOB,
    NAICSLevel CHAR(1),
    NAICSSector CHAR(2),
    Flag CHAR(1) NOT NULL,
    CONSTRAINT PK_NAICSCODES PRIMARY KEY (NAICSCodeType, NAICSCode),
    CONSTRAINT fk_naicscode_flag FOREIGN KEY (Flag) REFERENCES CodeFlags (Flag),
    CONSTRAINT fk_naicscode_naicslevel FOREIGN KEY (NAICSLevel) REFERENCES NAICSLevels (NAICSLevel),
    CONSTRAINT fk_naicscode_naicssector FOREIGN KEY (NAICSSector) REFERENCES NAICSSectors (NAICSSector)
);

/****** Object:  Table EthnicityCodes 41 ******/
CREATE TABLE EthnicityCodes (
    EthnicityCode CHAR(1) NOT NULL,
    EthnicityDesc VARCHAR2(35),
    CONSTRAINT PK_ETHNICITYCODES PRIMARY KEY (EthnicityCode)
);

/****** Object:  Table RaceCodes 42 ******/
CREATE TABLE RaceCodes (
    RaceCode CHAR(2) NOT NULL,
    RaceDesc VARCHAR2(35),
    CONSTRAINT PK_RACECODES PRIMARY KEY (RaceCode)
);

/****** Object:  Table IndDirectories 44 ******/
CREATE TABLE IndDirectories (
    MatrixIndCode CHAR(15) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    ProjectedYear CHAR(4) NOT NULL,
    MatrixIndTitle VARCHAR2(200) NOT NULL,
    SubTotal CHAR(1),
    Ownership CHAR(2),
    CONSTRAINT PK_INDDIRECTORY PRIMARY KEY (MatrixIndCode, PeriodYear, PeriodType, Period, ProjectedYear),
    CONSTRAINT fk_IndDirectories_indsublevels FOREIGN KEY (SubTotal) REFERENCES IndSubLevels (SubTotal),
    CONSTRAINT fk_IndDirectories_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_IndDirectories_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear)
);

/****** Object:  Table SOCCodes 46 ******/
CREATE TABLE SOCCodes (
    SOCCodeType CHAR(2) NOT NULL,
    SOCCode CHAR(6) NOT NULL,
    SOCTitle VARCHAR2(100),
    SOCTitleLong VARCHAR2(200),
    SOCDesc CLOB,
    Education CHAR(1),
    Experience CHAR(1),
    Training CHAR(1),
    Flag CHAR(1),
    SOCParent CHAR(6),
    SeriesLevel CHAR(1),
    CONSTRAINT PK_SOCCODE PRIMARY KEY (SOCCodeType, SOCCode),
    --CONSTRAINT fk_soccode_blseducation FOREIGN KEY (Education) REFERENCES BLSEducation (EduCategory),
    --CONSTRAINT fk_soccode_blstrainingcodes FOREIGN KEY (Training) REFERENCES BLSTrainingCodes (TrainingCode),
    --CONSTRAINT fk_soccode_experience FOREIGN KEY (Experience) REFERENCES Experience (ExperienceCode),
    CONSTRAINT fk_soccode_flag FOREIGN KEY (Flag) REFERENCES CodeFlags (Flag),
    CONSTRAINT fk_soccode_occtypes FOREIGN KEY (SOCCodeType) REFERENCES OccCodeTypes (CodeType),
    CONSTRAINT fk_soccode_soccode FOREIGN KEY (SOCCodeType, SOCParent) REFERENCES SOCCodes (SOCCodeType, SOCCode)
    --CONSTRAINT fk_soccode_serieslevels FOREIGN KEY (SOCCodeType, SeriesLevel) REFERENCES SeriesLevel (OccCodeType, SeriesLevel)
);

/****** Object:  Table OccupationCodes 48 ******/
CREATE TABLE OccupationCodes (
    StFips CHAR(2) NOT NULL,
    CodeType CHAR(2) NOT NULL,
    Code CHAR(10) NOT NULL,
    CodeTitle VARCHAR2(200) NOT NULL,
    CONSTRAINT PK_OCCCODES PRIMARY KEY (StFips, CodeType, Code),
    CONSTRAINT fk_occcodes_statefips FOREIGN KEY (StFips) REFERENCES StateFips (StFips),
    CONSTRAINT fk_occcodes_occtypes FOREIGN KEY (CodeType) REFERENCES OccCodeTypes (CodeType)
);

/****** Object:  Table IndustryCodes 49 ******/
CREATE TABLE IndustryCodes (
    StFips CHAR(2) NOT NULL,
    CodeType CHAR(2) NOT NULL,
    Code CHAR(10) NOT NULL,
    CodeTitle VARCHAR2(200) NOT NULL,
    CONSTRAINT PK_INDUSTRYCODES PRIMARY KEY (StFips, CodeType, Code),
    CONSTRAINT fk_industrycodes_indcodetypes FOREIGN KEY (CodeType) REFERENCES IndCodeTypes (CodeType),
    CONSTRAINT fk_IndustryCodes_statefips FOREIGN KEY (StFips) REFERENCES StateFips (StFips)
);


/****** Object:  Table AgeGroupTypes 51 ******/
CREATE TABLE AgeGroupTypes (
    AgeGroupType CHAR(2) NOT NULL,
    SourceCategory VARCHAR2(35),
    CONSTRAINT PK_AGEGROUPTYPES PRIMARY KEY (AgeGroupType)
);

/****** Object:  Table AgeGroups 52 ******/
CREATE TABLE AgeGroups (
    AgeGroupType CHAR(2) NOT NULL,
    AgeGroup CHAR(2) NOT NULL,
    AgeGroupDesc CHAR(20),
    CONSTRAINT PK_AGEGROUPS PRIMARY KEY (AgeGroup),
    CONSTRAINT fk_agegroups_agegrouptypes FOREIGN KEY (AgeGroupType) REFERENCES AgeGroupTypes (AgeGroupType)
);


/****** Object:  Table AnnualSalesCodes 53 ******/
CREATE TABLE AnnualSalesCodes (
    AnnSalesCode CHAR(1) NOT NULL,
    AnnSalesDesc VARCHAR2(40),
    CONSTRAINT PK_ANNUALSALESCODES PRIMARY KEY (AnnSalesCode)
);

/****** Object:  Table AnnualSalesRanges 54 ******/
CREATE TABLE AnnualSalesRanges (
    AnnSalesRange CHAR(2) NOT NULL,
    AnnRangeDesc VARCHAR2(40),
    CONSTRAINT PK_ANNUALSALESRANGES PRIMARY KEY (AnnSalesRange)
);

/****** Object:  Table Benchmark 55 ******/
CREATE TABLE Benchmark (
    Benchmark CHAR(4) NOT NULL,
    BenchmarkDesc VARCHAR2(60),
    CONSTRAINT PK_BENCHMARK PRIMARY KEY (Benchmark)
);

/****** Object:  Table CPITypes 56 ******/
CREATE TABLE CPITypes (
    CPIType CHAR(2) NOT NULL,
    CPITitle VARCHAR2(55) NOT NULL,
    CPIDesc VARCHAR2(200),
    CONSTRAINT PK_CPITYPES PRIMARY KEY (CPIType)
);

/****** Object:  Table CPISources 57 ******/
CREATE TABLE CPISources (
    StFips CHAR(2) NOT NULL,
    CPISource CHAR(1) NOT NULL,
    CPISourceDesc VARCHAR2(40) NOT NULL,
    CONSTRAINT PK_CPISOURCE PRIMARY KEY (StFips, CPISource),
    CONSTRAINT fk_cpisource_statefips FOREIGN KEY (StFips) REFERENCES StateFips (StFips)
);

/****** Object:  Table CreditCodes 58 ******/
CREATE TABLE CreditCodes (
    CreditCode CHAR(1) NOT NULL,
    CreditDesc CLOB,
    CONSTRAINT PK_CREDITCODES PRIMARY KEY (CreditCode)
);

/****** Object:  Table EmpSizeFlag 59 ******/
CREATE TABLE EmpSizeFlag (
    EmpSizeFlag CHAR(1) NOT NULL,
    EmpFlagDesc VARCHAR2(50),
    CONSTRAINT PK_EMPSIZEFLAG PRIMARY KEY (EmpSizeFlag)
);

/****** Object:  Table EmpSizeRange 60 ******/
CREATE TABLE EmpSizeRange (
    EmpSizeRange CHAR(2) NOT NULL,
    EmpRangeDesc VARCHAR2(40),
    CONSTRAINT PK_EMPSIZERANGE PRIMARY KEY (EmpSizeRange)
);

/****** Object:  Table GrowthCodes 61 ******/
CREATE TABLE GrowthCodes (
    StFips CHAR(2) NOT NULL,
    GrowthCode CHAR(2) NOT NULL,
    GrowthDesc VARCHAR2(20),
    CONSTRAINT PK_GROWTHCODES PRIMARY KEY (StFips, GrowthCode),
    CONSTRAINT fk_growthcodes_statefips FOREIGN KEY (StFips) REFERENCES StateFips (StFips)
);


/****** Object:  Table LicenseNumberTypes 65 ******/
CREATE TABLE LicenseNumberTypes (
    LicenseNumberType CHAR(2) NOT NULL,
    NumberDesc VARCHAR2(50) NOT NULL,
    CONSTRAINT PK_LICENSENUMBERTYPES PRIMARY KEY (LicenseNumberType)
);

/****** Object:  Table LocationStatuses 66 ******/
CREATE TABLE LocationStatuses (
    LocationStatusCode CHAR(1) NOT NULL,
    LocationStatusDesc VARCHAR2(40),
    CONSTRAINT PK_LOCATIONSTATUSES PRIMARY KEY (LocationStatusCode)
);


/****** Object:  Table PrivateGovt 67 ******/
CREATE TABLE PrivateGovt (
    Ownership CHAR(1) NOT NULL,
    PrvGovDesc VARCHAR2(15),
    CONSTRAINT PK_PRIVATEGOVT PRIMARY KEY (Ownership)
);

/****** Object:  Table ContactProTitles 70 ******/
CREATE TABLE ContactProTitles (
    ContactProTitle CHAR(3) NOT NULL,
    ContactProDesc VARCHAR2(35),
    CONSTRAINT PK_CONTACTPROTITLES PRIMARY KEY (ContactProTitle)
);

/****** Object:  Table ContactTitles 71 ******/
CREATE TABLE ContactTitles (
    ContactTitleCode CHAR(1) NOT NULL,
    ContactTitleDesc VARCHAR2(35),
    CONSTRAINT PK_CONTACTTITLES PRIMARY KEY (ContactTitleCode)
);


/****** Object:  Table CES 77 ******/
CREATE TABLE CES (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    SeriesCodeType CHAR(2) NOT NULL,
    SeriesCode CHAR(8) NOT NULL,
    Adjusted CHAR(1) NOT NULL,
    Benchmark CHAR(4) NOT NULL,
    Prelim CHAR(1) NOT NULL,
    EmpCES NUMBER(9,0),
    EmpProductionWorkers NUMBER(9,0),
    EmpFemaleWorkers NUMBER(9,0),
    HoursPerWeek NUMBER(3,1),
    EarningsPerWeek NUMBER(8,2),
    EarningsPerHour NUMBER(6,2),
    SuppRecord CHAR(1) NOT NULL,
    SuppHoursEarnings CHAR(1) NOT NULL,
    SuppProdWorkers CHAR(1) NOT NULL,
    SuppFemaleWorkers CHAR(1) NOT NULL,
    HoursAllWorkers NUMBER(3,1),
    EarningsAllWorkers NUMBER(8,2),
    HourlyEarningsAllWorkers NUMBER(6,2),
    SuppHEAllWrkr CHAR(1) NOT NULL,
    CONSTRAINT PK_CES PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period, SeriesCodeType, SeriesCode, Adjusted),
    CONSTRAINT fk_ces_benchmark FOREIGN KEY (Benchmark) REFERENCES Benchmark (Benchmark),
    CONSTRAINT fk_ces_cescode FOREIGN KEY (StFips, SeriesCodeType, SeriesCode) REFERENCES CESCodes (StFips, SeriesCodeType, SeriesCode),
    CONSTRAINT fk_ces_indcodetypes FOREIGN KEY (SeriesCodeType) REFERENCES IndCodeTypes (CodeType),
    CONSTRAINT fk_ces_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_ces_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_ces_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear)
);

/****** Object:  Table CPI 79 ******/
CREATE TABLE CPI (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    CPIType CHAR(2) NOT NULL,
    CPISource CHAR(1) NOT NULL,
    CPI NUMBER(8,3),
    PctChangeY2Y NUMBER(6,1),
    PctChangeM2M NUMBER(6,1),
    CONSTRAINT PK_CPI PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period, CPIType, CPISource),
    CONSTRAINT fk_cpi_cpisource FOREIGN KEY (StFips, CPISource) REFERENCES CPISources (StFips, CPISource),
    CONSTRAINT fk_cpi_cpitype FOREIGN KEY (CPIType) REFERENCES CPITypes (CPIType),
    CONSTRAINT fk_cpi_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_cpi_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_cpi_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear)
);

/****** Object:  Table EmpDBInf 82 ******/
CREATE TABLE EmpDBInf (
    ReleaseNumber CHAR(3) NOT NULL,
    ReleaseMonth CHAR(2) NOT NULL,
    ReleaseYear CHAR(4) NOT NULL,
    CopyrightYear CHAR(4),
    ContractYear CHAR(4),
    EditionYear CHAR(4),
    CONSTRAINT PK_EMPDBINF PRIMARY KEY (ReleaseNumber)
);

/****** Object:  Table EmpDB 83 ******/
CREATE TABLE EmpDB (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    UniqueID CHAR(9) NOT NULL,
    FEIN CHAR(9),
    LastUpdate CHAR(6),
    Name VARCHAR2(35),
    AddressP VARCHAR2(40),
    CityP VARCHAR2(30),
    StateP CHAR(2),
    ZipCodeP CHAR(5),
    ZipPlusP CHAR(4),
    Latitude NUMBER(11,6),
    Longitude NUMBER(11,6),
    GeoPrecisionCode CHAR(1),
    CensusTract CHAR(6),
    CensusBlockGrp CHAR(1),
    AddressM VARCHAR2(40),
    CityM VARCHAR2(30),
    StateM CHAR(2),
    ZipCodeM CHAR(5),
    ZipPlusM CHAR(4),
    AddressL VARCHAR2(40),
    CityL VARCHAR2(30),
    StateL CHAR(2),
    ZipCodeL CHAR(5),
    ZipPlusL CHAR(4),
    TeleNum CHAR(10),
    ContactLastName VARCHAR2(30),
    ContactFirstName VARCHAR2(30),
    ContactTitle VARCHAR2(35),
    ContactTitleCode CHAR(1),
    ContactProTitle CHAR(3),
    ContactGender CHAR(1),
    ContactEmail VARCHAR2(60),
    TollFreeTele CHAR(10),
    FaxNumber CHAR(10),
    WebURL VARCHAR2(40),
    BusinessDesc VARCHAR2(45),
    PrimarySIC CHAR(6),
    SIC2 CHAR(6),
    SIC3 CHAR(6),
    SIC4 CHAR(6),
    SIC5 CHAR(6),
    PrimaryNAICS CHAR(8),
    NAICS2 CHAR(8),
    NAICS3 CHAR(8),
    NAICS4 CHAR(8),
    NAICS5 CHAR(8),
    Ownership CHAR(1),
    LocationStatusCode CHAR(1),
    StockExchangeCode CHAR(1),
    StockTicker CHAR(6),
    WhiteCollarPct NUMBER(4,1),
    WhiteCollarFlag CHAR(1),
    EmpSizeRange CHAR(2),
    EmpSizeValue NUMBER(9,0),
    EmpSizeFlag CHAR(1),
    AnnualSalesRange CHAR(2),
    AnnualSales VARCHAR2(15),
    AnnualSalesFlag CHAR(1),
    YearEst CHAR(4),
    CreditCode CHAR(1),
    HeadQuartersID CHAR(9),
    ParentID CHAR(9),
    UltimateParentID CHAR(9),
    ForeignParentFlag CHAR(1),
    ExportImportFlag CHAR(1),
    BusinessType CHAR(1),
    WorkAtHome CHAR(1),
    ReleaseNumber CHAR(3),
    CONSTRAINT PK_EMPDB PRIMARY KEY (UniqueID),
    CONSTRAINT FK_EMPDB_annualsalescodes FOREIGN KEY (AnnualSalesFlag) REFERENCES AnnualSalesCodes (AnnSalesCode),
    CONSTRAINT FK_EMPDB_annualsalesranges FOREIGN KEY (AnnualSalesRange) REFERENCES AnnualSalesRanges (AnnSalesRange),
    CONSTRAINT FK_EMPDB_CONTACTPROTITLES FOREIGN KEY (ContactProTitle) REFERENCES ContactProTitles (ContactProTitle),
    CONSTRAINT FK_EMPDB_CONTACTTITLE FOREIGN KEY (ContactTitleCode) REFERENCES ContactTitles (ContactTitleCode),
    CONSTRAINT FK_EMPDB_CREDITCODE FOREIGN KEY (CreditCode) REFERENCES CreditCodes (CreditCode),
    CONSTRAINT FK_EMPDB_EMPDBINF FOREIGN KEY (ReleaseNumber) REFERENCES EmpDBInf (ReleaseNumber),
    CONSTRAINT FK_EMPDB_EMPSIZEFLAG FOREIGN KEY (EmpSizeFlag) REFERENCES EmpSizeFlag (EmpSizeFlag),
    CONSTRAINT FK_EMPDB_EMPSIZERANGE FOREIGN KEY (EmpSizeRange) REFERENCES EmpSizeRange (EmpSizeRange),
    CONSTRAINT FK_EMPDB_GEOGRAPHIES FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT FK_EMPDB_GEOPRECISIONCODE FOREIGN KEY (GeoPrecisionCode) REFERENCES GeoPrecisionCodes (GeoPrecisionCode),
    CONSTRAINT FK_EMPDB_LOCATIONSTATUS FOREIGN KEY (LocationStatusCode) REFERENCES LocationStatuses (LocationStatusCode),
    CONSTRAINT FK_EMPDB_PRIVATEGOVT FOREIGN KEY (Ownership) REFERENCES PrivateGovt (Ownership)
    --CONSTRAINT FK_EMPDB_STOCKEXCHANGE FOREIGN KEY (StockExchangeCode) REFERENCES StockExchange (StockExchangeCode)
);

/****** Object:  Table IndustrySums 86 ******/
CREATE TABLE IndustrySums (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    IndCodeType CHAR(2) NOT NULL,
    IndCode CHAR(10) NOT NULL,
    IndSource CHAR(1) NOT NULL,
    Employers NUMBER(6,0),
    CONSTRAINT PK_INDUSTRYSUM PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, IndCodeType, IndCode, IndSource),
    CONSTRAINT fk_industrysum_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_industrysum_industrycodes FOREIGN KEY (StFips, IndCodeType, IndCode) REFERENCES IndustryCodes (StFips, CodeType, Code)
);


/****** Object:  Table Industry 88 ******/
CREATE TABLE Industry (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    IndCodeType CHAR(2) NOT NULL,
    IndCode CHAR(10) NOT NULL,
    Ownership CHAR(2) NOT NULL,
    Prelim CHAR(1),
    Firms NUMBER(8,0),
    Establishments NUMBER(8,0),
    QuarterAvgEmp NUMBER(9,0),
    Month1Emp NUMBER(9,0),
    Month2Emp NUMBER(9,0),
    Month3Emp NUMBER(9,0),
    TopEmployerAvgEmp NUMBER(9,0),
    TotalWages NUMBER(14,0),
    AvgWeeklyWage NUMBER(8,0),
    TaxableWages NUMBER(14,0),
    UIContributions NUMBER(12,0),
    Suppress CHAR(1),
	NECC NUMBER(9,0), -- Custom Oregon Field - Non-Economic Code Changes
    CONSTRAINT PK_INDUSTRY PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period, IndCodeType, IndCode, Ownership),
    CONSTRAINT fk_industry_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_industry_industrycodes FOREIGN KEY (StFips, IndCodeType, IndCode) REFERENCES IndustryCodes (StFips, CodeType, Code),
    CONSTRAINT fk_industry_ownerships FOREIGN KEY (Ownership) REFERENCES Ownerships (Ownership),
    CONSTRAINT fk_industry_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_industry_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear)
);

/****** Object:  Table ProjectionsMatrix 89 ******/
CREATE TABLE ProjectionsMatrix (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    MatrixIndCode CHAR(15) NOT NULL,
    MatrixOccCode CHAR(10) NOT NULL,
    ProjectedYear CHAR(4) NOT NULL,
    EstimatedEmp NUMBER(9,0),
    ProjectedEmp NUMBER(9,0),
    PctEstInd NUMBER(6,2),
    PctEstOcc NUMBER(6,2),
    PctProjInd NUMBER(6,2),
    PctProjOcc NUMBER(6,2),
    NumericChange NUMBER(9,0),
    PercentChange NUMBER(9,4),
    GrowthRate NUMBER(8,4),
    GrowthCode CHAR(2),
    -- Uncomment these lines for Michigan
    -- aopeng NUMBER(9,0),
    -- aopenr NUMBER(9,0),
    -- aopent NUMBER(9,0),
    Exits NUMBER(9,0),
    AnnualExits NUMBER(9,0),
    Transfers NUMBER(9,0),
    AnnualTransfers NUMBER(9,0),
    Change NUMBER(9,0),
    AnnualChange NUMBER(9,0),
    Openings NUMBER(9,0),
    AnnualOpenings NUMBER(9,0),
    Suppress CHAR(1) NOT NULL,
    CONSTRAINT PK_PROJECTIONSMATRIX PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period, MatrixIndCode, MatrixOccCode, ProjectedYear),
    CONSTRAINT fk_projectionsmatrix_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_projectionsmatrix_growthcodes FOREIGN KEY (StFips, GrowthCode) REFERENCES GrowthCodes (StFips, GrowthCode),
    CONSTRAINT FK_PROJECTIONSMATRIX_INDDIRECTORIES FOREIGN KEY (MatrixIndCode, PeriodYear, PeriodType, Period, ProjectedYear) REFERENCES IndDirectories (MatrixIndCode, PeriodYear, PeriodType, Period, ProjectedYear),
    CONSTRAINT FK_PROJECTIONSMATRIX_OCCDIRECTORIES FOREIGN KEY (MatrixOccCode, PeriodYear, PeriodType, Period, ProjectedYear) REFERENCES OccDirectories (MatrixOccCode, PeriodYear, PeriodType, Period, ProjectedYear),
    CONSTRAINT fk_projectionsmatrix_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_projectionsmatrix_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear)
);

/****** Object:  Table LaborForce 90 ******/
CREATE TABLE LaborForce (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    Adjusted CHAR(1) NOT NULL,
    Prelim CHAR(1) NOT NULL,
    Benchmark CHAR(4),
    LaborForce NUMBER(9,0),
    Employed NUMBER(9,0),
    Unemployed NUMBER(9,0),
    UnemployedRate NUMBER(5,1),
    CLFPRate NUMBER(5,1),
    EmpPopRatio NUMBER(5,1),
    CONSTRAINT PK_LABORFORCE PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period, Adjusted),
    CONSTRAINT fk_laborforce_benchmark FOREIGN KEY (Benchmark) REFERENCES Benchmark (Benchmark),
    CONSTRAINT fk_laborforce_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_laborforce_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_laborforce_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear)
);

/****** Object:  Table LicenseTypes 91 ******/
CREATE TABLE LicenseTypes (
    LicenseType CHAR(1) NOT NULL,
    TypeDesc VARCHAR2(50) NOT NULL,
    CONSTRAINT PK_LICENSETYPES PRIMARY KEY (LicenseType)
);

/****** Object:  Table LicenseExams 92 ******/
CREATE TABLE LicenseExams (
    LicenseExam CHAR(1) NOT NULL,
    ExamDesc VARCHAR2(50) NOT NULL,
    CONSTRAINT PK_LICENSEEXAMS PRIMARY KEY (LicenseExam)
);

/****** Object:  Table LicenseEducation 93 ******/
CREATE TABLE LicenseEducation (
    LicenseEducation CHAR(1) NOT NULL,
    EducationDesc VARCHAR2(25) NOT NULL,
    CONSTRAINT PK_LICENSEEDUCATION PRIMARY KEY (LicenseEducation)
);

/****** Object:  Table LicenseContinuingEdu 94 ******/
CREATE TABLE LicenseContinuingEdu (
    LicenseContinuingEdu CHAR(1) NOT NULL,
    ContinuingEduDesc VARCHAR2(50) NOT NULL,
    CONSTRAINT PK_LICENSECONTINUINGEDU PRIMARY KEY (LicenseContinuingEdu)
);

/****** Object:  Table LicenseCertifications 95 ******/
CREATE TABLE LicenseCertifications (
    LicenseCertification CHAR(1) NOT NULL,
    CertificationDesc VARCHAR2(60) NOT NULL,
    CONSTRAINT PK_LICENSECERTIFICATIONS PRIMARY KEY (LicenseCertification)
);

/****** Object:  Table LicenseExperience 96 ******/
CREATE TABLE LicenseExperience (
    LicenseExperience CHAR(1) NOT NULL,
    ExperienceDesc VARCHAR2(25) NOT NULL,
    CONSTRAINT PK_LICENSEEXPERIENCE PRIMARY KEY (LicenseExperience)
);

/****** Object:  Table LicenseCriminal 97 ******/
CREATE TABLE LicenseCriminal (
    LicenseCriminal CHAR(1) NOT NULL,
    CriminalDesc VARCHAR2(50) NOT NULL,
    CONSTRAINT PK_LICENSECRIMINAL PRIMARY KEY (LicenseCriminal)
);

/****** Object:  Table LicensePhysicalReqs 98 ******/
CREATE TABLE LicensePhysicalReqs (
    LicensePhysicalReq CHAR(1) NOT NULL,
    PhysicalReqDesc VARCHAR2(50) NOT NULL,
    CONSTRAINT PK_LICENSEPHYSICALREQS PRIMARY KEY (LicensePhysicalReq)
);

/****** Object:  Table LicenseActiveStatuses 99 ******/
CREATE TABLE LicenseActiveStatuses (
    LicenseActiveStatus CHAR(1) NOT NULL,
    ActiveStatusDesc VARCHAR2(25) NOT NULL,
    CONSTRAINT PK_LICENSEACTIVESTATUSES PRIMARY KEY (LicenseActiveStatus)
);

/****** Object:  Table LicenseVeteran 100 ******/
CREATE TABLE LicenseVeteran (
    LicenseVeteran CHAR(1) NOT NULL,
    VeteranDesc VARCHAR2(200) NOT NULL,
    CONSTRAINT PK_LICENSEVETERAN PRIMARY KEY (LicenseVeteran)
);

/****** Object:  Table LicenseAuthorities 101 ******/
CREATE TABLE LicenseAuthorities (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    LicAuthID CHAR(3) NOT NULL,
    Department VARCHAR2(255),
    Division VARCHAR2(255),
    Board VARCHAR2(255),
    Address1 VARCHAR2(75),
    Address2 VARCHAR2(75),
    City VARCHAR2(30) NOT NULL,
    State CHAR(2) NOT NULL,
    ZipCode CHAR(5) NOT NULL,
    ZipExt CHAR(4),
    Latitude NUMBER(11,6),
    Longitude NUMBER(11,6),
    GeoPrecisionCode CHAR(1),
    Telephone VARCHAR2(10),
    TeleExt VARCHAR2(10),
    Fax VARCHAR2(10),
    Contact VARCHAR2(50),
    Email VARCHAR2(70),
    URL VARCHAR2(200),
    CONSTRAINT PK_LICENSEAUTHORITIES PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, LicAuthID),
    CONSTRAINT fk_licenseauthorities_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_licenseauthorities_geoprecisioncode FOREIGN KEY (GeoPrecisionCode) REFERENCES GeoPrecisionCodes (GeoPrecisionCode)
);

/****** Object:  Table License 102 ******/
CREATE TABLE License (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    LicenseID CHAR(10) NOT NULL,
    LicAuthID CHAR(3) NOT NULL,
    LicenseTitle VARCHAR2(200) NOT NULL,
    LicenseDesc CLOB,
    LicenseType CHAR(1),
    Exam CHAR(1),
    Education CHAR(1),
    ContinuingEdu CHAR(1),
    Certification CHAR(1),
    Experience CHAR(1),
    Criminal CHAR(1),
    PhysicalReq CHAR(1),
    Veteran CHAR(1),
    Inactive CHAR(1),
    LicenseURL CHAR(200),
    LicenseUpdated CHAR(8),
    CONSTRAINT PK_LICENSE PRIMARY KEY (StFips, LicenseID),
    CONSTRAINT fk_license_liccontinuingedu FOREIGN KEY (ContinuingEdu) REFERENCES LicenseContinuingEdu (LicenseContinuingEdu),
    CONSTRAINT fk_license_licenseactivestatus FOREIGN KEY (Inactive) REFERENCES LicenseActiveStatuses (LicenseActiveStatus),
    CONSTRAINT fk_license_licenseauthorities FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area, LicAuthID) REFERENCES LicenseAuthorities (StFips, AreaType, AreaTypeVersion, Area, LicAuthID),
    CONSTRAINT fk_license_licensecertification FOREIGN KEY (Certification) REFERENCES LicenseCertifications (LicenseCertification),
    CONSTRAINT fk_license_licensecriminal FOREIGN KEY (Criminal) REFERENCES LicenseCriminal (LicenseCriminal),
    CONSTRAINT fk_license_licenseeducation FOREIGN KEY (Education) REFERENCES LicenseEducation (LicenseEducation),
    CONSTRAINT fk_license_licenseexams FOREIGN KEY (Exam) REFERENCES LicenseExams (LicenseExam),
    CONSTRAINT fk_license_licenseexperience FOREIGN KEY (Experience) REFERENCES LicenseExperience (LicenseExperience),
    CONSTRAINT fk_license_licensephysicalreqs FOREIGN KEY (PhysicalReq) REFERENCES LicensePhysicalReqs (LicensePhysicalReq),
    CONSTRAINT fk_license_licensetypes FOREIGN KEY (LicenseType) REFERENCES LicenseTypes (LicenseType),
    CONSTRAINT fk_license_licenseveteran FOREIGN KEY (Veteran) REFERENCES LicenseVeteran (LicenseVeteran)
);

/****** Object:  Table LicenseHistory 104 ******/
CREATE TABLE LicenseHistory (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    LicenseID CHAR(10) NOT NULL,
    LicenseNumberType CHAR(2) NOT NULL,
    LicenseNumber NUMBER(9,0),
    CONSTRAINT PK_LICENSEHISTORY PRIMARY KEY (StFips, PeriodYear, PeriodType, Period, LicenseID, LicenseNumberType),
    CONSTRAINT fk_licensehistory_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_licensehistory_license FOREIGN KEY (StFips, LicenseID) REFERENCES License (StFips, LicenseID),
    CONSTRAINT fk_licensehistory_licensenumbertype FOREIGN KEY (LicenseNumberType) REFERENCES LicenseNumberTypes (LicenseNumberType),
    CONSTRAINT fk_licensehistory_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_licensehistory_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear)
);

/****** Object:  Table IOWage 105 ******/
CREATE TABLE IOWage (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    IndCodeType CHAR(2) NOT NULL,
    IndCode CHAR(10) NOT NULL,
    OccCodeType CHAR(2) NOT NULL,
    OccCode CHAR(10) NOT NULL,
    WageSource CHAR(1) NOT NULL,
    EmpCount NUMBER(10,0) NOT NULL,
    RateType CHAR(1) NOT NULL,
    ResponseRate NUMBER(6,0),
    MeanWage NUMBER(9,2),
    EntryWage NUMBER(9,2),
    ExperiencedWage NUMBER(9,2),
    Percentile10Wage NUMBER(9,2),
    Percentile25Wage NUMBER(9,2),
    MedianWage NUMBER(9,2),
    Percentile75Wage NUMBER(9,2),
    Percentile90Wage NUMBER(9,2),
    UserDefinedPct NUMBER(3,0),
    UserDefinedPctWage NUMBER(9,2),
    UserDefinedRangeLoPct NUMBER(3,0),
    UserDefinedRangeHiPct NUMBER(3,0),
    UserDefinedRangeMean NUMBER(9,2),
    WageRelativePctError NUMBER(6,2),
    EmpRelativePctError NUMBER(6,2),
    PanelCode CHAR(6),
    SuppressWage CHAR(1) NOT NULL,
    SuppressAll CHAR(1) NOT NULL,
    SuppressEmp CHAR(1) NOT NULL,
    CONSTRAINT PK_IOWAGE PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period, IndCodeType, IndCode, OccCodeType, OccCode, WageSource, RateType),
    CONSTRAINT fk_iowage_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
--    CONSTRAINT fk_iowage_industrycodes FOREIGN KEY (StFips, IndCodeType, IndCode) REFERENCES IndustryCodes (StFips, CodeType, Code),
--    CONSTRAINT fk_iowage_occcodes FOREIGN KEY (StFips, OccCodeType, OccCode) REFERENCES OccupationCodes (StFips, CodeType, Code),
    CONSTRAINT fk_iowage_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_iowage_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear),
    CONSTRAINT fk_iowage_wageratetypes FOREIGN KEY (RateType) REFERENCES WageRateTypes (RateType),
    CONSTRAINT fk_iowage_wagesource FOREIGN KEY (StFips, WageSource) REFERENCES WageSources (StFips, WageSource)
);

/****** Object:  Table Schools 108 ******/
CREATE TABLE Schools (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    InstitutionCode CHAR(10) NOT NULL,
    InstitutionType CHAR(2) NOT NULL,
    InstitutionOwnership CHAR(1) NOT NULL,
    InstName1 VARCHAR2(80),
    InstName2 VARCHAR2(80),
    Address1 VARCHAR2(35),
    Address2 VARCHAR2(35),
    City VARCHAR2(30),
    State CHAR(2),
    ZipCode CHAR(5),
    ZipExt CHAR(4),
    Latitude NUMBER(11,6),
    Longitude NUMBER(11,6),
    GeoPrecisionCode CHAR(1),
    Telephone CHAR(10),
    TeleExt VARCHAR2(10),
    Fax CHAR(10),
    URL VARCHAR2(200),
    Contact VARCHAR2(50),
    DistanceLearn CHAR(1),
    SatelliteCampus CHAR(1),
    CONSTRAINT PK_SCHOOLS PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, InstitutionCode),
    CONSTRAINT fk_schools_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_schools_geoprecisioncode FOREIGN KEY (GeoPrecisionCode) REFERENCES GeoPrecisionCodes (GeoPrecisionCode),
    CONSTRAINT fk_schools_institutiontype FOREIGN KEY (StFips, InstitutionType) REFERENCES InstitutionTypes (StFips, InstitutionType)
);

/****** Object:  Table ProgramCompleters 109 ******/
CREATE TABLE ProgramCompleters (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    InstitutionCode CHAR(10) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    CodeType CHAR(2) NOT NULL,
    Code CHAR(10) NOT NULL,
    CompleterType CHAR(2) NOT NULL,
    Completers NUMBER(8,0),
    Placements CLOB,
    CONSTRAINT PK_PROGRAMCOMPLETERS PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, InstitutionCode, PeriodYear, PeriodType, Period, CodeType, Code, CompleterType),
    CONSTRAINT fk_programcompleters_completertype FOREIGN KEY (StFips, CompleterType) REFERENCES CompleterTypes (StFips, CompleterType),
    CONSTRAINT fk_programcompleters_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_programcompleters_occcodes FOREIGN KEY (StFips, CodeType, Code) REFERENCES OccupationCodes (StFips, CodeType, Code),
    CONSTRAINT fk_programcompleters_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_programcompleters_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear),
    CONSTRAINT fk_programcompleters_schools FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area, InstitutionCode) REFERENCES Schools (StFips, AreaType, AreaTypeVersion, Area, InstitutionCode)
);


/****** Object:  Table Programs 110 ******/
CREATE TABLE Programs (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    InstitutionCode CHAR(10) NOT NULL,
    CodeType CHAR(2) NOT NULL,
    Code CHAR(10) NOT NULL,
    CompleterType CHAR(2) NOT NULL,
    Length NUMBER(8,2),
    LengthType CHAR(2),
    ProgCost NUMBER(6,0),
    ProgTitle VARCHAR2(200) NOT NULL,
    ProgDesc CLOB,
    CIPCodeType CHAR(2),
    CIPCode CHAR(10),
    URL VARCHAR2(200),
    Classroom CHAR(1),
    "ONLINE" CHAR(1),
    ClassTime CHAR(1),
    ETPLApproval CHAR(1),
    CONSTRAINT PK_PROGRAMS PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, InstitutionCode, CodeType, Code, CompleterType),
   -- CONSTRAINT fk_programs_classtime FOREIGN KEY (ClassTime) REFERENCES ClassTime (ClassTime),
    CONSTRAINT fk_programs_completertype FOREIGN KEY (StFips, CompleterType) REFERENCES CompleterTypes (StFips, CompleterType),
    CONSTRAINT fk_programs_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
   -- CONSTRAINT fk_programs_lengthtype FOREIGN KEY (StFips, LengthType) REFERENCES LengthTypes (StFips, LengthType),
    CONSTRAINT fk_programs_occcodes FOREIGN KEY (StFips, CodeType, Code) REFERENCES OccupationCodes (StFips, CodeType, Code),
    CONSTRAINT fk_programs_occcodescip FOREIGN KEY (StFips, CIPCodeType, CIPCode) REFERENCES OccupationCodes (StFips, CodeType, Code),
    CONSTRAINT fk_programs_schools FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area, InstitutionCode) REFERENCES Schools (StFips, AreaType, AreaTypeVersion, Area, InstitutionCode)
);

/****** Object:  Table UIClaims 114 ******/
CREATE TABLE UIClaims (
    StFips CHAR(2) NOT NULL,
    AreaType CHAR(2) NOT NULL,
    AreaTypeVersion CHAR(4) NOT NULL,
    Area CHAR(6) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    ClaimType CHAR(1) NOT NULL,
    OccCodeType CHAR(2) NOT NULL,
    OccCode CHAR(10) NOT NULL,
    IndCodeType CHAR(2) NOT NULL,
    IndCode CHAR(10) NOT NULL,
    AgeGroup CHAR(2) NOT NULL,
    RaceCode CHAR(2) NOT NULL,
    Ethnicity CHAR(1) NOT NULL,
    GenderCode CHAR(1) NOT NULL,
    Claimants NUMBER(8,0),
    WeeksComp NUMBER(8,0),
    FirstPayments NUMBER(8,0),
    Duration NUMBER(4,1),
    CONSTRAINT PK_UICLAIMS PRIMARY KEY (StFips, AreaType, AreaTypeVersion, Area, PeriodYear, PeriodType, Period, ClaimType, OccCodeType, OccCode, IndCodeType, IndCode, AgeGroup, RaceCode, Ethnicity, GenderCode),
    CONSTRAINT fk_uiclaims_agegroups FOREIGN KEY (AgeGroup) REFERENCES AgeGroups (AgeGroup),
    CONSTRAINT fk_uiclaims_ethnicitycodes FOREIGN KEY (Ethnicity) REFERENCES EthnicityCodes (EthnicityCode),
    CONSTRAINT fk_uiclaims_gender FOREIGN KEY (GenderCode) REFERENCES Genders (GenderCode),
    CONSTRAINT fk_uiclaims_geographies FOREIGN KEY (StFips, AreaType, AreaTypeVersion, Area) REFERENCES Geographies (StFips, AreaType, AreaTypeVersion, Area),
    CONSTRAINT fk_uiclaims_industrycodes FOREIGN KEY (StFips, IndCodeType, IndCode) REFERENCES IndustryCodes (StFips, CodeType, Code),
    CONSTRAINT fk_uiclaims_occcodes FOREIGN KEY (StFips, OccCodeType, OccCode) REFERENCES OccupationCodes (StFips, CodeType, Code),
    CONSTRAINT fk_uiclaims_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_uiclaims_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear),
    CONSTRAINT fk_uiclaims_racecodes FOREIGN KEY (RaceCode) REFERENCES RaceCodes (RaceCode)
);

/****** Object:  Table OccupationXOccupation 115 ******/
CREATE TABLE OccupationXOccupation (
    StFips CHAR(2) NOT NULL,
    CodeType CHAR(2) NOT NULL,
    Code CHAR(10) NOT NULL,
    CodeType2 CHAR(2) NOT NULL,
    Code2 CHAR(10) NOT NULL,
    CONSTRAINT PK_OCCXOCC PRIMARY KEY (StFips, CodeType, Code, CodeType2, Code2),
    CONSTRAINT fk_occxocc_occcodes FOREIGN KEY (StFips, CodeType, Code) REFERENCES OccupationCodes (StFips, CodeType, Code),
    CONSTRAINT fk_occxocc_occcodes2 FOREIGN KEY (StFips, CodeType2, Code2) REFERENCES OccupationCodes (StFips, CodeType, Code)
);


/****** Object:  Table LicenseXOcc 118 ******/
CREATE TABLE LicenseXOcc (
    StFips CHAR(2) NOT NULL,
    LicenseID CHAR(10) NOT NULL,
    OccCodeType CHAR(2) NOT NULL,
    OccCode CHAR(10) NOT NULL,
    CONSTRAINT PK_LICENSEXOCC PRIMARY KEY (StFips, LicenseID, OccCodeType, OccCode),
    CONSTRAINT fk_licensexocc_license FOREIGN KEY (StFips, LicenseID) REFERENCES License (StFips, LicenseID),
    CONSTRAINT fk_licensexocc_occcodes FOREIGN KEY (StFips, OccCodeType, OccCode) REFERENCES OccupationCodes (StFips, CodeType, Code)
);


/****** Object:  Table MatrixXInd 119 ******/
CREATE TABLE MatrixXInd (
    StFips CHAR(2) NOT NULL,
    MatrixIndCode CHAR(15) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    ProjectedYear CHAR(4) NOT NULL,
    IndCodeType CHAR(2) NOT NULL,
    IndCode CHAR(10) NOT NULL,
    SubTotal CHAR(1),
    CONSTRAINT PK_MATRIXXIND PRIMARY KEY (StFips, MatrixIndCode, PeriodYear, PeriodType, Period, ProjectedYear, IndCodeType, IndCode),
    CONSTRAINT fk_matrixxind_indsublevels FOREIGN KEY (SubTotal) REFERENCES IndSubLevels (SubTotal),
    CONSTRAINT fk_matrixxind_industrycodes FOREIGN KEY (StFips, IndCodeType, IndCode) REFERENCES IndustryCodes (StFips, CodeType, Code),
    CONSTRAINT fk_matrixxind_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_matrixxind_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear)
);

/****** Object:  Table MatrixXOcc 120 ******/
CREATE TABLE MatrixXOcc (
    StFips CHAR(2) NOT NULL,
    MatrixOccCode CHAR(10) NOT NULL,
    PeriodYear CHAR(4) NOT NULL,
    PeriodType CHAR(2) NOT NULL,
    Period CHAR(2) NOT NULL,
    ProjectedYear CHAR(4) NOT NULL,
    OccCodeType CHAR(2) NOT NULL,
    OccCode CHAR(10) NOT NULL,
    SubTotal CHAR(1),
    CONSTRAINT PK_MATRIXXOCC PRIMARY KEY (StFips, MatrixOccCode, PeriodYear, PeriodType, Period, ProjectedYear, OccCodeType, OccCode),
    CONSTRAINT fk_matrixxocc_occcodes FOREIGN KEY (StFips, OccCodeType, OccCode) REFERENCES OccupationCodes (StFips, CodeType, Code),
    CONSTRAINT fk_matrixxocc_occsublevels FOREIGN KEY (SubTotal) REFERENCES OccSubLevels (SubTotal),
    CONSTRAINT fk_matrixxocc_periods FOREIGN KEY (PeriodType, Period) REFERENCES Periods (PeriodType, Period),
    CONSTRAINT fk_matrixxocc_periodyears FOREIGN KEY (PeriodYear) REFERENCES PeriodYears (PeriodYear)
);