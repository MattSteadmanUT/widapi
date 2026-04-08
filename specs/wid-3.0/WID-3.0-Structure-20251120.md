[IMAGE OMITTED: 395x177]

## Database Structure Version 3.0 


**Icon Legend**

- `[ICON: APPLE_CORE]` indicates a core table (per WID documentation).
- `[ICON: TRIGGERFISH]` indicates a lookup table where triggers are used.

[IMAGE OMITTED: 107x58]

## **Consortium members:** 

Michigan, Minnesota, Montana, North Carolina, South Carolina, Oregon, Virginia, Washington, Wisconsin, and the Employment and Training Administration (ETA) 

_WID Database Structure – Version 3.0_ 

_May 1, 2025_ 

_- 1 -_ 

## WID Version 3.0 - Release History 

Release Date: 03/11/2024 Revision: 12/12/2024 Revision: 5/1/2025 – Added StFips field to CESCodes table 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 2 -_ 

## **INTRODUCTION** 

The Workforce Information Database (WID) is a standardized database structure developed for the storage and dissemination of local, state, regional, and national workforce information on the economy, industry, labor supply and demand, and other aspects of and areas affected by, or that have an effect on our workforce. Population of the database is a core deliverable of the ETA State Workforce Information Grant and is carried out by the agency that has responsibility for collecting, preparing, and disseminating the information within each state. The WID structure is continually updated to incorporate new data and adapt to new technology in order to accommodate the wide range of uses and users of this information.  Structure updates are also driven by WID users submitting requests, comments, and concerns to Analyst Resource Center (ARC) consortium members or by the ARC directly (see Contact Information below). The change process is coordinated by the Analyst Resource Center Consortium, which is funded by the U.S. Department of Labor, Employment and Training Administration (ETA). The Consortium is led by the state of Minnesota. Membership includes Connecticut, Iowa, Michigan, Minnesota, Montana, Nevada, North Carolina, Oregon, Virginia, Wisconsin, and the ETA. 

## **TABLE LAYOUTS** 

The table layouts for the Workforce Information Database tables are presented in this document. They are organized into four types: lookup tables, data tables, crosswalks, and administrative or application developers’ tables. There is also a section of standard field values. The lookup tables contain relatively constant data that pertains mainly to descriptive data associated with classification codes. The data tables contain information about employment, wages, income, layoffs, industries, occupations, employers, education and training completers, educational programs, population demographics, selected economic indicators, and other data. These tables are intended to contain information that is maintained on a regular basis by each state. The crosswalk tables are a subset of the lookup tables representing the relationships between various classification codes. The administrative tables are tables used by the database administrator to record database management activities. This section also contains the all-codes tables used for industry codes and occupation/training codes (see below). The field values tables present standard values assigned to fields in the respective tables that are to be used in populating the database. 

## **CORE TABLES** 

Core Tables are a group of data tables, and related lookup tables, that contain the most common data produced by state departments of labor and are thus considered mandatory according to the WIG TEGL controlling the ARC. From the WIDCenter website: As stated in the Employment and Training Administration (ETA), Workforce Information Core Products and Services Grant, “States are required to implement and maintain the most current version of the Workforce Information Database and populate all tables designated as core tables in accordance with guidelines issued by the Analyst Resource Center (ARC). Database content must be updated timely in order to be as current as the state’s most recent publications and data releases.”  The core tables are listed in Appendix E, as well as indicated with an apple core: 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 3 -_ 

## **CHANGES MADE FROM PRIOR VERSIONS** 

Appendix D lists the changes made to the Workforce Information Database from the prior version to this current one. 

## **HOW TO READ A TABLE DEFINITION** 

Each table definition contains complete information about the structure of a table. Each definition includes: the name of the table, a complete list of each column name and data type, complete constraint information, and short descriptions of the columns. The following illustration identifies each component of a table definition. 

[IMAGE OMITTED: 574x467]

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 4 -_ 

## **HOW TO USE THE STANDARD FIELD VALUES** 

Those fields with standard values, such as area types and industry and occupation code types, are arranged in alphabetical order under the Standard Field Values section. For example, if you are 

loading or using county data, you can look in the Field Values under "areatype" to see that County is area type = 04. 

|||loadin or usin count data ou|
|---|---|---|
|||g  g y , y|
|areatype||can look in the Field Values under|
|Values:<br>00|<br>US|"areatype" to see that County is<br>area type = 04.|
|01<br>03<br>04<br>05<br>06<br>07<br>08|State<br>SDA<br>County<br>Minor Civil Division<br>BLS Region<br>Broad Geographic Area (BGA)<br>Economic Development Region|The other commonly used<br>standard field is codetype, which<br>denotes the coding system used<br>for industries, occupations, and<br>education programs. This is the<br>field used by the administrative<br>tables indcodes and occcodes to|
|09<br>10<br>11<br>12<br>13<br>14<br>15<br>16<br>17|Planning Region<br>Labor Market Area<br>City<br>Town<br>Township<br>Municipality/Suburb<br>Workforce Investment Region<br>One Stop Area<br>Workforce Development Area|reference the code system.<br>Although the table contains 20<br>values, most industry data will be<br>NAICS-based, codetype = 10, and<br>most occupation data will be<br>SOC- based, either 2010<br>(codetype=14), or the newer<br>2018 (codetype=19).|
|18|Job Center Area|Other fields with standard values|
|19<br>20<br>25<br>26|Congressional District<br>Census Places<br>Metropolitan New England City and Town Area<br>(NECTA)<br>Micropolitan New England City and Town Area|include various look-up tables,<br>such as annualsalesrange,<br>cescode (industry codes specific<br>to CES), and populationsource.|
|27<br>28<br>30<br>31<br>32<br>33<br>34|(NECTA)<br>New England City and Town Area (NECTA) Divisions<br>Combined New England City and Town Area<br>(NECTA)<br>Balance of State<br>Metropolitan Statistical Area<br>Micropolitan Statistical Area<br>Metropolitan Division<br>Combined Statistical Area|In WID 3.0 a major change was to<br>add an areatypeversion field to<br>the area keys.  This allows areas<br>that have different vintages to be<br>easily grouped for a time series.<br>The most significant one is<br>Metropolitan Statistical Areas<br>(MSAs).  Historical versions<br>should be added under the WID|
|35|EEO County Group|2.8 current version areatypes|
|41|BLS CPI|(31-34), with an areatypeversion|
|||of 2018.  Prior versions,|



The other commonly used standard field is codetype, which denotes the coding system used for industries, occupations, and education programs. This is the field used by the administrative tables indcodes and occcodes to reference the code system. Although the table contains 20 values, most industry data will be NAICS-based, codetype = 10, and most occupation data will be SOC- based, either 2010 (codetype=14), or the newer 2018 (codetype=19). 

areatypes (21-24, 02) should also be added under 31-34 but with areatypeversions indicating their vintage. 

For all of these, having standardized values means that we are all talking about the same thing when we look at data. 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 5 -_ 

## **TABLE CONSTRAINTS AND TRIGGERS** 

The Constraint section of each table definition identifies all Primary Keys and Foreign Keys for the table.  The constraints for a table specify rules for data that will be stored in that table. These rules exist in the form of either Primary Keys or Foreign Keys. The definitions of these terms appear below: 

Primary Key:   The Primary Key is a column or group of columns that shall always be (1) non-null and (2) unique. Only one primary key exists for each table. A Primary Key constraint is enforced on a table so that each row in the table can be uniquely identified. 

Foreign Key:   A Foreign Key is a column or group of columns where each value or group of values exists as a Primary Key in another table. Foreign Key constraints are used so that data for a column or group of columns can be validated against another table containing a list of valid values for that column or group of columns. 

Throughout the table definitions, Primary Keys and Foreign Keys are used wherever possible to achieve the greatest level of data quality. For example, in the table ces, there is a column called seriescode.  By specifying a Foreign Key constraint on the seriescode column referencing the table cescode, no row in the ces table will ever contain a value for seriescode that does not already exist in the table cescode. This protects the ces table from ever containing invalid seriescode values. 

The information stored in the Workforce Information Database contains many different classification systems for occupations (DOT, SOC, OES, ONET, etc.), industries (NAICS and SIC) and training programs (various releases of CIP). The states may choose which classification system they are going to use. Some states may choose to classify occupations with ONET codes while other states may use SOC codes. Some states store data making use of several different classifications and some states are in the process of converting from one classification system to another thus needing to store data for both classifications. 

Since its inception, the Workforce Information Database has provided a means for specifying more than one type of classification system in some of the data tables. For example, the programcompleters table has a code field in which several different types of occupational codes (DOT, OES, SOC, etc.) can be used. The programcompleters table’s codetype field defines the type of code being used in a particular record. These multi-code fields provide flexibility in classifying the data, but can cause problems with data integrity. 

All relational database systems require Foreign Keys to be a set of fixed conditions, with the data field having the same length as the Primary Key of the lookup table it references. Each Foreign Key constraint must reference only one lookup table. This restriction presents a problem when trying to set Foreign Key constraints on the multi-code fields in some of the Workforce Information tables. Since the length of the classification codes can vary (4 – 10 characters) and the lookup table to be referenced must be determined by the code-type, Foreign Key constraints cannot be set directly between the multi-code field and the lookup table that needs to be referenced. 

In order to provide a means of enforcing Foreign Key constraints on the multi-code fields, two administrative tables were added to the Workforce Information Database. They are the industrycodes and occupationcodes tables. The industrycodes table is designed to contain the codetype and code of all the industry classifications a state may be using. The occupationcodes table is designed to contain all of the occupation and training classifications a state may be using. Each of 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 6 -_ 

these tables also contains the title associated with the classification code so these multi-code tables can also be used for application development. 

Maintenance of the multi-code tables – industrycodes and occupationcodes – can present additional effort and responsibility for the Workforce Information Database Administrators. In order to minimize the efforts of the DBA, triggers have been identified as a means to maintain the multi-code tables 

that can be used universally. A trigger is a special kind of stored procedure that goes into effect when you modify data in a specified table using one of the data modification operations – UPDATE, INSERT, or DELETE. Triggers can query other tables and can include complex SQL statements. Triggers in the Workforce Information Database are used specifically to maintain the referential integrity of the multi-code tables with any changes that are made to the lookup tables. The advantage of using triggers is they are automatic on all logged operations. They are activated immediately after any modification to the table’s data. Triggers are supported by most enterpriselevel database systems. 

Triggers should be added to each of the lookup tables that need to be represented in the multi- code tables. For example, the soccodes table will have a trigger that executes whenever it is updated and it will automatically update the occupationcodes table with the code and title changes made to the soccodes table. Likewise, the naicscodes table will have triggers that will update the industrycodes table with the code and title changes made to the naicscodes tables. Lookup tables that should have triggers are identified in the Tables section and in the Load Order Document, Appendix A, with the triggerfish icon. 

[ICON: TRIGGERFISH]

## Use of VARCHAR(MAX) 

In order to accommodate some of the large descriptor fields in the Workforce Information Database, ARC recommends using data type varchar(MAX) for the descriptor fields specified in this structure document. If your system does not support data type varchar(MAX), then use the largest varchar size your DBMS and/or organization allows. 

Workforce Information Database Administrators need to be aware of the fact that data type variations may exist depending on the system being used and they will need to adapt this structure for their particular database management system. 

## CONTACT INFORMATION and LINKS: 

For comments, questions, or suggestions:  arc.deed@state.mn.us. 

For sample code: http://www.widcenter.org/ Core tables: http://www.widcenter.org/document/all-core-tables/ 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 7 -_ 

## **Workforce Information Database Version 3.0** 

WID Tables – Listed by Type 

## Contents 

|**_Look-Up Tables .......................................11_**|
|---|
|**AgeGroups ............................................... 11**|
|**AgeGroupTypes ........................................ 11**|
|**AnnualSalesCodes**<br>**................................ 11**|
|**AnnualSalesRanges**<br>**.............................. 11**|
|**AreaTypes**<br>**........................................... 11**|
|**AreaTypeVersions**<br>**................................ 12**|
|**BEDTypes ................................................. 12**|
|**Benchmark**<br>**.......................................... 12**|
|**BLSEducation ........................................... 12**|
|**BLSTrainingCodes ..................................... 12**|
|**CareerClusters .......................................... 13**|
|**CareerPaths ............................................. 13**|
|**CESCodes**<br>**............................................. 13**|
|**CIPCodes**<br>**............................................. 14**|
|**ClassTime ................................................. 14**|
|**CompleterTypes ....................................... 14**|
|**ContactProTitles**<br>**.................................. 15**|
|**ContactTitles**<br>**....................................... 15**|
|**CPIItems .................................................. 15**|
|**CPISources ............................................... 15**|
|**CPITypes .................................................. 15**|
|**CreditCodes**<br>**......................................... 15**|



|**EmpDBInf**<br>**............................................ 16**|
|---|
|**EmpSizeFlag**<br>**......................................... 16**|
|**EmpSizeRange**<br>**..................................... 16**|
|**EthnicityCodes ......................................... 16**|
|**Experience ............................................... 17**|
|**Genders ................................................... 17**|
|**Geographies**<br>**........................................ 17**|
|**GeoPrecisionCodes**<br>**.............................. 18**|
|**GrowthCodes**<br>**....................................... 18**|
|**IncomeSources ......................................... 18**|
|**IncomeTypes ............................................ 18**|
|**IndCodeTypes**<br>**...................................... 19**|
|**IndDirectories**<br>**...................................... 19**|
|**IndOccSpecialIDs ...................................... 20**|
|**IndSubLevels**<br>**....................................... 21**|
|**IndustryCodes**<br>**...................................... 21**|
|**InstitutionOwnerships .............................. 22**|
|**InstitutionTypes ....................................... 22**|
|**JOLTSTypes .............................................. 22**|
|**LayTitles ................................................... 22**|
|**LengthTypes ............................................. 22**|
|**LicenseActiveStatuses**<br>**.......................... 23**|
|**LicenseCertifications**<br>**............................ 23**|
|**LicenseContinuingEdu**<br>**.......................... 23**|
|**LicenseCriminal**<br>**.................................... 23**|



_WID Database Structure – Version 3.0_ 

_- 8 -_ 

_May 1, 2025_ 

|**LicenseEducation**<br>**................................. 24**|
|---|
|**LicenseExams**<br>**....................................... 24**|
|**LicenseExperience**<br>**................................ 24**|
|**LicenseNumberTypes ............................... 24**|
|**LicensePhysicalReqs**<br>**............................. 24**|
|**LicenseTypes**<br>**....................................... 24**|
|**LicenseVeteran**<br>**.................................... 25**|
|**LocationStatuses**<br>**.................................. 25**|
|**NAICSCodes**<br>**......................................... 25**|
|**NAICSDomains ......................................... 26**|
|**NAICSLevels ............................................. 26**|
|**NAICSSectors ........................................... 26**|
|**NAICSSuperSectors ................................... 26**|
|**OccCodeTypes**<br>**..................................... 27**|
|**OccDirectories**<br>**..................................... 27**|
|**OccSubLevels**<br>**....................................... 27**|
|**OccupationCodes**<br>**................................. 28**|
|**ONETCodes**<br>**.......................................... 28**|
|**Ownerships**<br>**......................................... 28**|
|**Periods**<br>**................................................ 29**|
|**PeriodTypes**<br>**......................................... 29**|
|**PeriodYears**<br>**......................................... 29**|
|**PopulationSources ................................... 29**|
|**PrivateGovt**<br>**......................................... 30**|
|**RaceCodes ............................................... 30**|
|**SalesTypes ............................................... 30**|
|**SOCCodes**<br>**............................................ 31**|
|**SpecialIDs ................................................ 31**|



|**StateFips**<br>**............................................. 31**|
|---|
|**StateProgramCode**<br>**............................... 32**|
|**StockExchange**<br>**..................................... 33**|
|**SubGeographies ....................................... 33**|
|**TaxTypes .................................................. 33**|
|**TransferPaymentTypes ............................. 34**|
|**UnitTypes ................................................ 34**|
|**WageRateTypes**<br>**................................... 34**|
|**WageRateTypes**<br>**................................... 34**|
|**_Data Tables ........................................... 35_**|
|**BED .......................................................... 35**|
|**BuildingPermits ........................................ 36**|
|**CES**<br>**...................................................... 37**|
|**Commute ................................................. 39**|
|**CPI ........................................................... 40**|
|**CPIPlus ..................................................... 41**|
|**Demographics .......................................... 42**|
|**EmpDB**<br>**................................................ 46**|
|**Income ..................................................... 50**|
|**Industry**<br>**............................................... 51**|
|**IOWage**<br>**............................................... 53**|
|**JOLTS ....................................................... 55**|
|**LaborForce**<br>**.......................................... 56**|
|**License**<br>**................................................ 57**|
|**LicenseAuthorities**<br>**............................... 59**|
|**LicenseHistory .......................................... 60**|
|**Population ............................................... 61**|
|**ProgramCompleters ................................. 62**|
|**Programs ................................................. 63**|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 9 -_ 

|**ProjectionsMatrix**<br>**................................ 65**|
|---|
|**SalesRevenue ........................................... 67**|
|**Schools .................................................... 68**|
|**Supply ...................................................... 70**|
|**TaxRevenues ............................................ 71**|
|**TransferPayments .................................... 72**|
|**UIClaims .................................................. 73**|
|**_Crosswalk Tables ....................................75_**|
|**IndustryXIndustry..................................... 75**|
|**LayTitleXOcc ............................................ 75**|
|**LicenseXLicense ........................................ 76**|
|**LicenseXOcc**<br>**......................................... 76**|
|**MatrixXInd**<br>**.......................................... 77**|
|**MatrixXOcc**<br>**.......................................... 78**|
|**OccupationXOccupation ........................... 79**|
|**_Administrative Tables .............................80_**|
|**IndustrySums ........................................... 80**|
|**TableList .................................................. 80**|
|**TableSource ............................................. 81**|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 10 -_ 

## **Look-Up Tables** 

## **AgeGroups** 

A table containing codes for identifying age categories. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. AgeGroupType **|char(2)||Source category for the age groups.|
|**2. AgeGroup**|char(2)|Primary Key|Code identifying the age group.|
|**3. AgeGroupDesc**|char(20)||Age group description.|
|Constraints||||
|1. Foreign Key (AgeGroups.AgeGroupType)||references(AgeGroupTypes.AgeGroupType)||



## **AgeGroupTypes** 

The source of the age group listed in the AgeGroups table. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. AgeGroupType**|char(2)|Primary Key|Code for the age group source|
||||category.|
|**2. SourceCategory**|char(35)||Description of the age group source.|



[ICON: APPLE_CORE]

## **AnnualSalesCodes** 

This table contains the annual sales codes used in the empdb table. 

||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**||
|---|---|---|---|---|---|
||**1. AnnSalesCode**|char(1)|Primary Key|Annual sales code.||
||**2. AnnSalesDesc**|varchar(40)||Description of the annual sales code.||
|**AnnualSalesRanges**||||||
||A table of annual sales value ranges for the employers in the empdb table.|||||
||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**||



||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|---|
||**1. AnnSalesRange**|char(2)|Primary Key|Code for the annual sales range.|
||**2. AnnRangeDesc**|varchar(40)|||
|**AreaTypes**|||||



A table containing identifiers for the geographic type, e.g.. MSA, SDA, county, city, township, etc. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeName**|varchar(200)||Descriptive title of the areatype.|
|Constraints||||
|1. Foreign Key (AreaTypes.StFips)references(StateFips.StFips)||||



_WID Database Structure – Version 3.0_ 

_May 1, 2025_ 

_- 11 -_ 

[ICON: APPLE_CORE]

## **AreaTypeVersions** 

A table for tracking series-breaking changes in area definitions, particularly MSAs 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|For AreaTypes that change|
||||periodically, an indication of vintage.|
|**4.**|varchar(500)||Descriptive content about the|
|**AreaTypeVersionNotes**|||AreaTypeVersion.|
|Constraints||||
|1. Foreign Key (AreaTypeVersions.StFips,||AreaTypeVersions.AreaType) references (AreaTypes.StFips,||
|AreaTypes.AreaType)||||



## **BEDTypes** 

A table of business employment dynamics data types. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. BEDTypeCode**|char(1)|Primary Key|Indicator of the type of data.|
|**2. BEDTypeDesc**|varchar(60)||Description of the BED data type.|



[ICON: APPLE_CORE]

## **Benchmark** 

This table contains benchmark years used in revised data such as CES and LAUS. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. Benchmark**|char(4)|Primary Key|Benchmark year used in the|
||||LaborForce and CES tables.|
|**2. BenchmarkDesc**|varchar(60)||Benchmark year description.|



## **BLSEducation** 

## Table of BLS education categories. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. EduCategory**|char(1)|Primary Key|Code assigned to the education|
||||category by BLS|
|**2. EducationDesc**|varchar(35)||Description of the education category|



## **BLSTrainingCodes** 

## A table of codes for the BLS training category for occupations. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. TrainingCode**|char(1)|Primary Key|Code assigned to the on-the-job|
||||training category by BLS|
|**2. TrainingDesc**|varchar(75)||Description of the on-the-job|
||||category|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 12 -_ 

## **CareerClusters** 

This table contains a listing of the Department of Education Career Clusters. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. CodeType**|char(2)|Primary Key||
|**2. ClusterCode**|char(2)|Primary Key|Career Cluster code|
|**3. ClusterTitle**|varchar(200)||Title of the Career Cluster|
|**4. ClusterDesc**|varchar(max)||Description of the Career Cluster|
|Constraints||||
|1. Foreign Key (CareerClusters.CodeType)references(OccCodeTypes.CodeType)||||



## **CareerPaths** 

A table of the Department of Education Career Pathways. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. CodeType**|char(2)|Primary Key||
|**2. PathCode**|char(4)|Primary Key|Career Pathway code|
|**3. PathTitle**|varchar(200)||Title of the Career Pathway|
|Constraints||||
|1. Foreign Key|(CareerPaths.CodeType)|references(OccCodeTypes.CodeType)||



[ICON: APPLE_CORE]

## **CESCodes** 

## The table of Current Employment Statistics codes. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips **|char(2)|PrimaryKey||
|**2. SeriesCodeType**|char(2)|Primary Key||
|**3. SeriesCode**|char(8)|Primary Key|Industry Series Code|
|**4. SeriesTitle**|varchar(100)||Long title used to describe industry|
||||division.|
|**5. SeriesTitleLong**|varchar(200)|||
|**6. SeriesDesc**|varchar(max)||Description of the industries|
||||comprisingthe series.|
|**7. SeriesLevel**|char(1)||Optional report indentation level.|
|Constraints||||
|1. Foreign Key (CESCodes.SeriesCodeType)references(IndCodeTypes.CodeType)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 13 -_ 

## **CIPCodes** 

[ICON: TRIGGERFISH]

A table of the current Classification of Instructional Programs (CIP) codes, including 2, 4 and 6-digit codes. 

||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|---|
||**1. CIPCodeType**|char(2)|Primary Key|A code describing the CIP version|
|||||classification code|
||**2. CIPCode**|char(10)|Primary Key|A 10-digit code assigned to a|
|||||Classification of Instructional|
|||||Programs(CIP) program title.|
||**3. CIPTitle**|varchar(100)||The instructional program title used|
|||||to organize training related data, i.e.,|
|||||enrollments,completers, placement.|
||**4. CIPTitleLong**|varchar(200)|||
||**5. CIPDesc**|varchar(max)||A definition of the curriculum|
|||||included in an instructionalprogram.|
||**6. CIPLevel**|char(1)||Indicator of the hierarchical level of|
|||||the CIP code.|
||Constraints||||
||1. Foreign Key (CIPCodes.CIPCodeType)||references(OccCodeTypes.CodeType)||
|**ClassTime**|||||
||The table of class time codes.||||
||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
||**1. ClassTime**|char(1)|Primary Key|Class time code.|
||**2. ClassTimeTitle**|varchar(40)||Description or title of the class time.|
|**CompleterTypes**|||||



The table of program completer types, by state. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code.|
|**2. CompleterType**|char(2)|Primary Key|A 2-digit code representing type of|
||||program completer.|
|**3. CompleterTitle**|varchar(40)||Title of type of program completer.|
|**4. CompleterDesc**|varchar(max)||Description of type of program|
||||completer.|
|Constraints||||
|1. Foreign Key (CompleterTypes.StFips)||references(StateFips.StFips)||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 14 -_ 

[ICON: APPLE_CORE]

## **ContactProTitles** 

The table of contact professional titles. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. ContactProTitle**|char(3)|Primary Key|Contact’s professional title.|
|**2. ContactProDesc**|varchar(35)||Description of the contact’s|
||||professional title.|



[ICON: APPLE_CORE]

## **ContactTitles** 

The table of the contact title codes. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. ContactTitleCode**|char(1)|Primary Key|Contact title code.|
|**2. ContactTitleDesc**|varchar(35)||Contact’s descriptive title.|



## **CPIItems** 

A table of market basket items included in the CPI. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. CPIItem**|char(9)|Primary Key|Code for the items in the Index|
|**2. ItemDesc**|varchar(100)||Description of the items in the index|
||||(e.g.,all items,food,energy,etc.)|



## **CPISources** 

Table of codes for the source of Consumer Price Index data. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code.|
|**2. CPISource**|char(1)|Primary Key|Source code for CPI data.|
|**3. CPISourceDesc**|varchar(40)||Description of CPI source.|
|Constraints||||
|1. Foreign Key (CPISources.StFips)references(StateFips.StFips)||||



## **CPITypes** 

The table of Consumer Price Index (CPI) types of measure. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. CPIType**|char(2)|Primary Key|A 2-digit code assigned to the type of|
||||CPI measure.|
|**2. CPITitle**|varchar(55)||Title of the CPI measure.|
|**3. CPIDesc**|varchar(200)||Description of the CPI measure.|



[ICON: APPLE_CORE]

## **CreditCodes** 

The table of credit codes, used in the empdb table. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. CreditCode**|char(1)|Primary Key|Credit code|
|**2. CreditDesc**|varchar(max)||Description of types of credit codes.|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 15 -_ 

[ICON: APPLE_CORE]

## **EmpDBInf** 

This table contains information about the current installed version of the empdb file. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. ReleaseNumber**|char(3)|Primary Key|Empdb release number.|
|**2. ReleaseMonth**|char(2)||Release month.|
|**3. ReleaseYear**|char(4)||Release year.|
|**4. CopyrightYear**|char(4)||Copyright year.|
|**5. ContractYear**|char(4)||Contract year.|
|**6. EditionYear**|char(4)||Edition year.|



**EmpSizeFlag** A table of employment size flag codes used in the empdb table. 

||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**||
|---|---|---|---|---|---|
||**1. EmpSizeFlag**|char(1)|Primary Key|Code for the size flag||
||**2. EmpFlagDesc**|varchar(50)||Description of the size flag.||
|**EmpSizeRange **||||||
||A table of size range codes used in the empdb table.|||||
||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**||



||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**||
|---|---|---|---|---|---|
||**1. EmpSizeRange**|char(2)|Primary Key|Code for the size range.||
||**2. EmpRangeDesc**|varchar(40)||Description of the size range.||
|**EthnicityCodes**||||||
||A table for ethnicitycodes.|||||
||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**||
||**1. EthnicityCode**|char(1)|Primary Key|Code assigned to the ethnicity||
|||||category||
||**2. EthnicityDesc**|varchar(35)||Description of the ethnicity code||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 16 -_ 

## **Experience** 

Table of experience needed codes from BLS. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. ExperienceCode**|char(1)|Primary Key|Code assigned to the work|
||||experience category by BLS|
|**2. ExperienceDesc**|varchar(20)||Description of the experience code|



## **Genders** 

A table containing codes for identifying gender of UI Claimants. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. GenderCode**|char(1)|Primary Key|Gender code.|
|**2. GenderDesc**|varchar(12)||Gender description.|



[ICON: APPLE_CORE]

**Geographies** 

A table containing geographic area descriptor records. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
||||a geographic area.  Front fill with|
||||zeroes.|
|**5. AreaName**|varchar(100)||Geographic area name.|
|**6. AreaDesc**|varchar(max)||Narrative description of a|
||||geographic area.|
|**7. Latitude**|numeric(11,6)||geographic coordinate specifying|
||||north-southposition on the Earth|
|**8. Longitude**|numeric(11,6)||geographic coordinate specifying|
||||east-westposition on the Earth|
|**9. GeoPrecisionCode**|char(1)|2|geocode precision level code. The|
||||precision of the longitude and|
||||latitude coordinates.|
|Constraints||||
|1. Foreign Key (Geographies.StFips) references (StateFips.StFips)||||
|2. Foreign Key (Geographies.GeoPrecisionCode) references (GeoPrecisionCodes.GeoPrecisionCode)||||
|3. Foreign Key (Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion) references||||
|(AreaTypeVersions.StFips,AreaTypeVersions.AreaType,AreaTypeVersions.AreaTypeVersion)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 17 -_ 

[ICON: APPLE_CORE]

## **GeoPrecisionCodes** 

A table of the levels of precision possible for the geocode. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. GeoPrecisionCode**|char(1)|Primary Key|Code for the precision level of the|
||||geocode.|
|**2. GeoPrecisionDesc**|varchar(40)||Description of the precision|
||||geocode.|



[ICON: APPLE_CORE]

## **GrowthCodes** 

A table of state-specific growth codes describing an industry or occupation. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code.|
|**2. GrowthCode**|char(2)|Primary Key|Code for the state-specific growth|
||||descriptor.|
|**3. GrowthDesc**|varchar(20)||Brief description interpreting the|
||||growth of the industryor occupation.|
|Constraints||||
|1. Foreign Key (GrowthCodes.StFips)||references(StateFips.StFips)||



## **IncomeSources** 

Codes for the source of income and population estimates for the income table. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code.|
|**2. IncomeSource**|char(1)|Primary Key|Source Code for income data.|
|**3. IncomeSourceDesc**|<br>varchar(40)||Description of income source.|
|Constraints||||
|1. Foreign Key (IncomeSources.StFips)||references(StateFips.StFips)||



## **IncomeTypes** 

A table containing types of income measures. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code.|
|**2. IncomeType**|char(2)|Primary Key|Code for the type of income measure.|
|**3. IncomeDesc**|varchar(75)||Income measure description.|
|Constraints||||
|1. Foreign Key (IncomeTypes.StFips)||references(StateFips.StFips)||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 18 -_ 

[ICON: APPLE_CORE]

## **IndCodeTypes** 

The table of industry code types used throughout the WID database. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. CodeType**|char(2)|Primary Key|Code describing the type of industry|
||||classification code.|
|**2. CodeTypeDesc**|varchar(40)||Title of classification Code.|



[ICON: APPLE_CORE]

## **IndDirectories** 

A table containing a directory of MicroMatrix level industry codes for which projections are performed. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. MatrixIndCode**|char(15)|Primary Key|Industry code from Micro Matrix.|
|**2. PeriodYear**|char(4)|Primary Key|Character representation of the|
||||calendar year (e.g. 2021).|
|**3. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
||||Long-term projections, short-term|
||||projections,etc.)|
|**4. Period**|char(2)|Primary Key|Period Code. Will be set to 00 where|
||||periodtype in annual based.|
|**5. ProjectedYear**|char(4)|Primary Key||
|**6. MatrixIndTitle**|varchar(200)||Industry title.|
|**7. SubTotal**|char(1)||Sum level of the information|
|**8. Ownership**|char(2)||A 2-digit indicator that identifies the|
||||employer by public or_x000D_|
||||private ownership.|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 19 -_ 

## **IndOccSpecialIDs** 

A table that identifies industries and occupations that are in special categories, such as Green jobs, high-tech., etc. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code|
|||2,4||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type version.|
||||Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent a|
||||geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2006).|
|**6. IndCodeType**|char(2)|Primary Key|Code describing the industry code|
|||4|type.|
|**7. IndCode**|char(10)|Primary Key|A code used in the classification of|
|||4|establishments by type of activity in|
||||which they are engaged. For codes|
||||not 6 characters long, left justify and|
||||blank (ASCII 32) fill.  Either SIC or|
||||NAICS code can be used.|
|**8. OccCodeType**|char(2)|Primary Key|Code describing the type of|
|||2|occupational coding system|
|**9. OccCode**|char(10)|Primary Key|The occupational classification code|
|||2|used by the state for this data|
||||element. This code could be a DOT,|
||||OEWS, SOC, CENSUS, etc. For codes|
||||not 10 characters long, left justify and|
||||blank(ASCII 32)fill.|
|**10. SpecialID**|char(3)|Primary Key|Code for the type of job, e.g. Green,|
|||3|STEM, etc.|
|**11. Score**|float|||
|Constraints||||
|1. Foreign Key (IndOccSpecialIDs.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (IndOccSpecialIDs.StFips, IndOccSpecialIDs.OccCodeType, IndOccSpecialIDs.OccCode)||||
|references (OccupationCodes.StFips, OccupationCodes.CodeType, OccupationCodes.Code)||||
|3. Foreign Key (IndOccSpecialIDs.SpecialID)||references (SpecialIDs.SpecialID)||
|4. Foreign Key (IndOccSpecialIDs.StFips, IndOccSpecialIDs.IndCodeType, IndOccSpecialIDs.IndCode)||||
|references (IndustryCodes.StFips, IndustryCodes.CodeType, IndustryCodes.Code)||||
|5. Foreign Key (IndOccSpecialIDs.StFips, IndOccSpecialIDs.AreaType,||||
|IndOccSpecialIDs.AreaTypeVersion, IndOccSpecialIDs.Area) references (Geographies.StFips,||||
|Geographies.AreaType,Geographies.AreaTypeVersion,Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 20 -_ 

[ICON: APPLE_CORE]

## **IndSubLevels** 

A table containing a lookup of industry sum level information. 

||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|---|
||**1. SubTotal**|char(1)|Primary Key|Sum level of the information.|
||**2. SubTotalDesc**|varchar(60)||Sum level description.|
|**IndustryCodes**|||||



Master table of industry code type/code combinations, allowing multiple classification systems to be used. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code.|
|||1||
|**2. CodeType**|char(2)|Primary Key|Code describing the type of industry|
||||classification code.|
|**3. Code**|char(10)|Primary Key|The classification code used by the|
||||state for this data element. This could|
||||be a SIC or NAICS code. For codes not|
||||6 characters long, left justify and blank|
||||(ASCII 32)fill.|
|**4. CodeTitle**|varchar(200)||The descriptive title for this industry|
||||code.|
|Constraints||||
|1. Foreign Key|(IndustryCodes.StFips) references (StateFips.StFips)|||
|2. Foreign Key|(IndustryCodes.CodeType)references(IndCodeTypes.CodeType)|||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 21 -_ 

## **InstitutionOwnerships** 

A table of training institution ownership codes and descriptions. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. InstitutionOwnership**|char(1)|Primary Key||
|**2.**|varchar(40)|||
|**InstitutionOwnershipDesc**||||



## **InstitutionTypes** 

The table of institution types in a state. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|**2. InstitutionType**|char(2)|Primary Key||
|**3.**|varchar(50)|||
|**InstitutionTypeDesc**||||
|Constraints||||
|1. Foreign Key (InstitutionTypes.StFips)||references(StateFips.StFips)||



## **JOLTSTypes** 

Types of the JOLTS numbers 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. JOLTSTypeCode**|char(2)|Primary Key|Code of the JOLTS data|
|**2. JOLTSTypeDesc**|varchar(100)||Description of the JOLTS data.|



## **LayTitles** 

A table of lay titles and codes. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. LayTitleCode**|char(5)|Primary Key|Lay title code|
|**2. LayTitle**|varchar(200)||Lay title associated with an|
||||occupation.|



## **LengthTypes** 

A table of training program lengths. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|**2. LengthType**|char(2)|Primary Key|The identifying code assigned to the|
||||program length.|
|**3. LengthTypeDesc**|varchar(40)||A description of the length of the|
||||assignedprogram length code.|
|Constraints||||
|1. Foreign Key (LengthTypes.StFips)||references(StateFips.StFips)||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 22 -_ 

[ICON: APPLE_CORE]

## **LicenseActiveStatuses** 

## Lookup table of the active status of the license 

||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|---|
||**1.**|char(1)|Primary Key|Indicator of license status|
||**LicenseActiveStatus**||||
||**2. ActiveStatusDesc**|varchar(25)||Description of license status|
|**LicenseCertifications**|||||
||Lookuptable of the certification requirements of the license||||
||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
||**1.**|char(1)|Primary Key|Indicator of license certification|
||**LicenseCertification**|||requirements|
||**2. CertificationDesc**|varchar(60)||Description of license certification|
|||||requirements|



[ICON: APPLE_CORE]

## **LicenseContinuingEdu** 

## Lookup table of the continuing education requirements of the license 

||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|---|
||**1.**|char(1)|Primary Key|Indicator of license continuing|
||**LicenseContinuingEdu**|||education requirements|
||**2. ContinuingEduDesc**|varchar(50)||Description of continuing education|
|||||requirements for license|
|**LicenseCriminal**|||||



## Lookup table of licensing restrictions on criminal records 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. LicenseCriminal**|char(1)|Primary Key|Indicator of criminal records|
||||requirements for the license|
|**2. CriminalDesc**|varchar(50)||Description of the criminal records|
||||requirements for the license|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 23 -_ 

[ICON: APPLE_CORE]

## **LicenseEducation** 

## Lookup table of the education requirements of the license 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. LicenseEducation**|char(1)|Primary Key|Indicator of educational|
||||requirements for the license|
|**2. EducationDesc**|varchar(25)||Description of the educational|
||||requirements|



[ICON: APPLE_CORE]

## **LicenseExams** 

## Lookup table of the exam requirements of the license 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. LicenseExam**|char(1)|Primary Key|Indicator of exams required for the|
||||license|
|**2. ExamDesc**|varchar(50)||Description of the exam requirements|



[ICON: APPLE_CORE]

**LicenseExperience** 

## Lookup table of the experience requirements of the license 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. LicenseExperience**|char(1)|Primary Key|Indicator of experience required for|
||||the license|
|**2. ExperienceDesc**|varchar(25)||Description of the experience|
||||required|



## **LicenseNumberTypes** 

The table of type codes used in the lichist table. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1.**|char(2)|Primary Key|Code for the type of statistic.|
|**LicenseNumberType **||||
|**2. NumberDesc**|varchar(50)||Description of the type of statistic.|



[ICON: APPLE_CORE]

## **LicensePhysicalReqs** 

Lookup table of physical requirements of the license 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. LicensePhysicalReq**|<br>char(1)|Primary Key|Indicator of the physical|
||||requirements for the license|
|**2. PhysicalReqDesc**|varchar(50)||Description of the physical|
||||requirements|



[ICON: APPLE_CORE]

**LicenseTypes** 

## Look-up table for the indicators of the type of license 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. LicenseType**|char(1)|Primary Key|Code for the license type|
|**2. TypeDesc**|varchar(50)||Description of the license type|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 24 -_ 

[ICON: APPLE_CORE]

## **LicenseVeteran** 

Codes indicating the veteran preference for a license 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. LicenseVeteran**|char(1)|Primary Key|Code indicating any veteran's|
||||preference of the license|
|**2. VeteranDesc**|varchar(100)||Description of the veteran|
||||preferences|



[ICON: APPLE_CORE]

## **LocationStatuses** 

The table of location status code types used in the empdb table. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1.**|char(1)|Primary Key|Code for the location status.|
|**LocationStatusCode**||||
|**2. LocationStatusDesc**|<br>varchar(40)||Description of the location status.|



**NAICSCodes** 

[ICON: TRIGGERFISH]

The table of North American Industry Classification System codes. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. NAICSCodeType**|char(2)|Primary Key||
|**2. NAICSCode**|char(6)|Primary Key|A code used in the North American|
||||Industry Classification System|
||||(NAICS).|
|**3. NAICSTitle**|varchar(100)||Title assigned to the NAICS code.|
|**4. NAICSTitleLong**|varchar(200)|||
|**5. NAICSTitleDesc**|varchar(max)|||
|**6. NAICSLevel**|char(1)|2|A code that indicates the hierarchical|
||||level of the NAICS industrycode.|
|**7. NAICSSector**|char(2)|1||
|**8. Flag**|char(1)|||
|Constraints||||
|1. Foreign Key (NAICSCodes.NAICSSector) references (NAICSSectors.NAICSSector)||||
|2. Foreign Key (NAICSCodes.NAICSLevel) references (NAICSLevels.NAICSLevel)||||
|3. Foreign Key (NAICSCodes.Flag)references(CodeFlags.Flag)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 25 -_ 

## **NAICSDomains** 

A table of NAICS Domains, aggregations of Supersectors as defined by BLS extensions of NAICS. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. NAICSDomain**|char(3)|Primary Key||
|**2. DomainTitle**|varchar(25)||Title assigned to the Domain.|



## **NAICSLevels** 

A table of the hierarchical level of the codes in the North American Industry Classification System (NAICS). 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. NAICSLevel**|char(1)|Primary Key|A code that indicates the hierarchical|
||||level of the NAICS industry code.|
|**2. LevelDesc**|varchar(40)||A description of the hierarchical level|
||||of the NAICS industrycode.|



## **NAICSSectors** 

The table of North American Industry Classification System (NAICS) industry Sectors. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. NAICSSector**|char(2)|Primary Key||
|**2. SectorDesc**|varchar(45)||A short description of the NAICS|
||||industrysector.|
|**3. SectorDescLong**|varchar(120)|||
|**4. NAICSSuper**|char(4)|||
|Constraints||||
|1. Foreign Key (NAICSSectors.NAICSSuper)references(NAICSSuperSectors.NAICSSuper)||||



## **NAICSSuperSectors** 

The table of NAICS Supersectors, defined by BLS official extensions to NAICS. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. NAICSSuper**|char(4)|Primary Key||
|**2. SuperTitle**|varchar(35)||Title assigned to the Supersector.|
|**3. NAICSDomain**|char(3)|||
|Constraints||||
|1. Foreign Key (NAICSSuperSectors.NAICSDomain)references|||(NAICSDomains.NAICSDomain)|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 26 -_ 

[ICON: APPLE_CORE]

## **OccCodeTypes** 

The table of occupation and training code types used in the WID database. 

||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|---|
||**1. CodeType**|char(2)|Primary Key|Code describing the type of|
|||||occupation or training classification|
|||||code.|
||**2. CodeTypeDesc**|varchar(40)||Title of classification Code.|
|**OccDirectories**|||||



## A table of Micro Matrix occupation codes for which projections are performed. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. MatrixOccCode**|char(10)|Primary Key|Occupation code from Micro Matrix.|
||||For codes not 10 characters long, left|
||||justifyand blank(ASCII 32)fill.|
|**2. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2021).|
|**3. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||2|Long-term projections, short-term|
||||projections,etc.)|
|**4. Period**|char(2)|Primary Key|Period Code. Will be set to 00 where|
|||1,2|periodtype in annual based.|
|**5. ProjectedYear**|char(4)|Primary Key||
|**6. MatrixOccTitle**|varchar(200)||Occupation title.|
|**7. SubTotal**|char(1)||Sum level of the information|
|Constraints||||
|1. Foreign Key (OccDirectories.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (OccDirectories.PeriodType, OccDirectories.Period) references (Periods.PeriodType,||||
|Periods.Period)||||
|3. Foreign Key (OccDirectories.SubTotal)references(OccSubLevels.SubTotal)||||



[ICON: APPLE_CORE]

## **OccSubLevels** 

## A table of occupation summary level information. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. SubTotal**|char(1)|Primary Key|Sum level of the information.|
|**2. SubTotalDesc**|varchar(60)||Sum level description.|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 27 -_ 

[ICON: APPLE_CORE]

## **OccupationCodes** 

Master table of occupation or training code type/code combination, allowing multiple codes systems to be used. 

||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|---|
||**1. StFips**|char(2)|Primary Key|State FIPS Code.|
||||1||
||**2. CodeType**|char(2)|Primary Key|Code describing the type of|
|||||occupation or training classification|
|||||code.|
||**3. Code**|char(10)|Primary Key|The classification code used by the|
|||||state for this data element. This could|
|||||be a CIP, DOT, OEWS, SOC or other|
|||||occupational code. For codes not 10|
|||||characters long, left justify and blank|
|||||(ASCII 32)fill.|
||**4. CodeTitle**|varchar(200)||The descriptive title for this|
|||||occupation or trainingcode.|
||Constraints||||
||1. Foreign Key (OccupationCodes.StFips) references (StateFips.StFips)||||
||2. Foreign Key (OccupationCodes.CodeType)references(OccCodeTypes.CodeType)||||
|**ONETCodes**|||||
||The table of O*NET codes and their descriptions.||||
||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
||**1. ONETCodeType**|char(2)|Primary Key|Code type for the O*NET code.|
||**2. ONETCode**|char(8)|Primary Key|A 6 or 8-digit code* assigned to the|
|||||Occupational Information Network|
|||||(O*NET) occupational title.  For codes|
|||||not 8 characters long, left justify and|
|||||blank(ASCII 32)fill.|
||**3. ONETYear**|char(4)||O*NET code version year.|
||**4. ONETTitle**|varchar(200)||Title of O*NET Code.|
||**5. ONETDesc**|varchar(max)||Description of the specified O*NET|
|||||code.|
||Constraints||||
||1. Foreign Key (ONETCodes.ONETCodeType)references(OccCodeTypes.CodeType)||||



[ICON: APPLE_CORE]

**Ownerships** 

## The table of codes for each type of ownership. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. Ownership**|char(2)|Primary Key|A 2-digit indicator that identifies the|
||||employer by public or private|
||||ownership.|
|**2. OwnerTitle**|varchar(40)||Title of ownership.|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 28 -_ 

[ICON: APPLE_CORE]

## **Periods** 

The table of time periods used in the WID. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. PeriodType**|<br>char(2)|Primary Key|Code describing type of period (e.g.|
||||Annual, quarterly, monthly, etc.)|
|**2. Period**|char(2)|Primary Key|Period code.|
|**3. PeriodDesc**|<br>varchar(25)||Description of the time period|
|Constraints||||
|1. Foreign Key|(Periods.PeriodType)|references(PeriodTypes.PeriodType)||



[ICON: APPLE_CORE]

## **PeriodTypes** 

The table of types of time periods used (e.g. Annual, quarterly, hourly, etc.) 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
||||Annual, quarterly, monthly, etc.)|
|**2. PeriodTypeDesc**|varchar(40)||A description of the period type.|



[ICON: APPLE_CORE]

**PeriodYears** 

A list of valid years for data. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. PeriodYear**|char(4)|Primary Key|The year for data.|



## **PopulationSources** 

A Table of the codes of the source of population estimates. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code.|
|**2. PopSource**|char(1)|Primary Key|Source code for population data.|
|**3. PopSourceDesc**|varchar(40)||Description of population source.|
|Constraints||||
|1. Foreign Key (PopulationSources.StFips)||references(StateFips.StFips)||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 29 -_ 

[ICON: APPLE_CORE]

## **PrivateGovt** 

A table of private/government status codes used in the empdb. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. Ownership**|char(1)|Primary Key|Code for the private/government status|
||||code.|
|**2. PrvGovDesc**|varchar(15)||Description of the code.|



## **RaceCodes** 

Table of races used in demographics 

||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|---|
||**1. RaceCode**|char(2)|Primary Key|Code representing race for|
|||||demographics|
||**2. RaceDesc**|varchar(35)||Description of the race code|
|**SalesTypes**|||||



A table of sales statistic types. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code.|
|**2. SalesType**|char(2)|Primary Key|A code that represents the sales type.|
|**3. SalesTypeDesc**|varchar(40)||A description of the sales type.|
|Constraints||||
|1. Foreign Key (SalesTypes.StFips)references(StateFips.StFips)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 30 -_ 

## **SOCCodes** 

[ICON: TRIGGERFISH]

## The table of  the current Standard Occupational Classification (SOC) codes. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. SOCCodeType**|char(2)|Primary Key|A code describing the SOC version|
|||1,2|classification code|
|**2. SOCCode**|char(6)|Primary Key|A 6-digit code assigned to the SOC|
|||1,2|occupational title from the current|
||||SOC Classification System.|
|**3. SOCTitle**|varchar(100)||The title assigned to that SOC|
||||occupation. *Note: No standard short|
||||titles are currentlyavailable.|
|**4. SOCTitleLong**|varchar(200)|||
|**5. SOCDesc**|varchar(max)||A narrative description of the SOC|
||||occupational title.|
|**6. Education**|char(1)||Typical education needed for entry|
||||into the occupation|
|**7. Experience**|char(1)|4||
|**8. Training**|char(1)|5|On-the-job training needed to attain|
||||competencyin the occupation|
|**9. Flag**|char(1)|3||
|**10. SOCParent**|char(6)|1|Parent SOC code to use for rollup.|
|Constraints||||
|1. Foreign Key (SOCCodes.SOCCodeType, SOCCodes.SOCParent) references (SOCCodes.SOCCodeType,||||
|SOCCodes.SOCCode)||||
|2. Foreign Key (SOCCodes.SOCCodeType) references (OccCodeTypes.CodeType)||||
|3. Foreign Key (SOCCodes.Flag) references (CodeFlags.Flag)||||
|4. Foreign Key (SOCCodes.Experience) references (Experience.ExperienceCode)||||
|5. Foreign Key (SOCCodes.Training) references (BLSTrainingCodes.TrainingCode)||||
|6. Foreign Key (SOCCodes.Education)references(BLSEducation.EduCategory)||||



## **SpecialIDs** 

Table of special types of jobs, such as Green, STEM, etc. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. SpecialID**|char(3)|Primary Key|The code for the special job type|
|**2. SpecialIDDesc**|varchar(40)||The special ID description|



[ICON: APPLE_CORE]

## **StateFips** 

## The table of State FIPS codes. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|**2. StateName**|varchar(20)||State name.|
|**3. Abreviation**|char(2)|||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 31 -_ 

## **StateProgramCode** 

[ICON: TRIGGERFISH]

## A table for State-specific training program codes. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|||1||
|**2. CodeType**|char(2)|Primary Key|A code describing the State|
||||classification code. NOTE that this|
||||should be set to '09', or issues could|
||||occur with the occcodes table.|
|**3. Code**|char(10)|Primary Key|The classification code used by the|
||||state for this data element.  This code|
||||could be DOT, OEWS, CIP, Cluster,|
||||SOC, Census, etc.  For codes not 10|
||||characters long, left justify and blank|
||||(ASCII 32)fill.|
|**4. Title**|varchar(200)||The program title represented by the|
||||code.|
|**5. CIPCode**|char(10)||A 10-digit code assigned to a|
||||Classification of Instructional|
||||Programs(CIP) program title.|
|**6. CIPCodeType**|char(2)||A code describing the version of the|
||||CIP code.|
|**7. TitleDesc**|varchar(max)||The program description.|
|Constraints||||
|1. Foreign Key (StateProgramCode.StFips) references (StateFips.StFips)||||
|2. Foreign Key (StateProgramCode.StFips, StateProgramCode.CIPCodeType,||||
|StateProgramCode.CIPCode) references (OccupationCodes.StFips, OccupationCodes.CodeType,||||
|OccupationCodes.Code)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 32 -_ 

[ICON: APPLE_CORE]

## **StockExchange** 

A table of the stock exchange codes used in the empdb table. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1.**|char(1)|Primary Key|Stock Exchange code.|
|**StockExchangeCode**||||
|**2. StockExchangeDesc**|<br>varchar(40)||Description of the stock exchange|
||||code.|



## **SubGeographies** 

A table of substate geographic areas and the larger areas that contain the areas. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
|||1|area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
|||1|version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
|||1|a geographic area. Front fill with|
||||zeroes.|
|**5. SubStFips**|char(2)|Primary Key|A 2-digit code assigned to represent|
|||1|a substate FIPS code.|
|**6. SubAreaType**|char(2)|Primary Key|A 2-digit code assigned to represent|
|||1|the type of substate area.|
|**7.**|char(4)|Primary Key|Code indicating the area type|
|**SubAreaTypeVersion**||1|version. Default = 0|
|**8. SubArea**|char(6)|Primary Key|A 6-digit code assigned to represent|
|||1|the name of the substate area.|
|Constraints||||
|1. Foreign Key (SubGeographies.SubStFips, SubGeographies.SubAreaType,||||
|SubGeographies.SubAreaTypeVersion, SubGeographies.SubArea) references (Geographies.StFips,||||
|Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area)||||
|2. Foreign Key (SubGeographies.StFips, SubGeographies.AreaType, SubGeographies.AreaTypeVersion,||||
|SubGeographies.Area) references (Geographies.StFips, Geographies.AreaType,||||
|Geographies.AreaTypeVersion,Geographies.Area)||||



## **TaxTypes** 

|A table of the types of tax collected bya state.|A table of the types of tax collected bya state.|A table of the types of tax collected bya state.||
|---|---|---|---|
|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|**2. TaxType**|char(2)|Primary Key|A 2-digit code identifying the type of|
||||tax.|
|**3. TaxTypeDesc**|varchar(75)||A description of the tax.|
|Constraints||||
|1. Foreign Key (TaxTypes.StFips)references(StateFips.StFips)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 33 -_ 

## **TransferPaymentTypes** 

A table of the types of government transfer payments received. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code.|
|**2. PaymentType**|char(2)|Primary Key|A 2-digit code identifying the|
||||government payment type.|
|**3. PaymentTypeDesc**|varchar(40)|||
|Constraints||||
|1. Foreign Key (TransferPaymentTypes.StFips)references(StateFips.StFips)||||



## **UnitTypes** 

||This table contains|the building permit types,bystate.|the building permit types,bystate.||
|---|---|---|---|---|
||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
||**1. StFips**|char(2)|Primary Key|State FIPS Code.|
||**2. UnitType**|char(2)|Primary Key|Code for the type of building permit.|
||**3. UnitTypeDesc**|varchar(60)||Description of building permit.|
||Constraints||||
||1. Foreign Key (UnitTypes.StFips)references(StateFips.StFips)||||
|**WageRateTypes**|||||
||A table of wage rate types(e.g. hourly,weekly,monthly,|||annually).|
||**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
||**1. RateType**|char(1)|Primary Key|Code which identifies the type of|
|||||wage rate.|
||**2. RateTypeDesc**|varchar(40)||A description of the type of wage|
|||||rate.|



**WageRateTypes** A table of wage rate types (e.g. hourly, weekly, monthly, annually). 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code.|
|**2. WageSource**|char(1)|Primary Key|A code representing the source of|
||||wage data.|
|**3. WageSourceDesc**|varchar(60)||A description of the source of the|
||||wage data.|
|Constraints||||
|1. Foreign Key (WageSources.StFips)||references(StateFips.StFips)||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 34 -_ 

## **Data Tables** 

## **BED** 

Table of private sector business employment dynamics data, showing job losses and gains due to expansion, contraction, new and closed business. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|||3,4||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
|||4|area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
|||4|version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
|||4|a geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||2|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to 00 where|
|||1,2|periodtype is annual.|
|**8. IndCodeType**|char(2)|Primary Key|Code for the type of industry: 05 =|
|||3|SIC; 10 = NAICS|
|**9. IndCode**|char(10)|Primary Key|The industry code.|
|||3||
|**10. Adjusted**|char(1)|Primary Key|Indicates whether data is seasonally|
||||adjusted: 0 = Not Adjusted; 1 =|
||||Adjusted|
|**11. BEDTypeCode**|char(1)|Primary Key|Indicator of the type of data.|
|**12. BEDEmp**|numeric(12,0)||Amount of employment gain/loss|
|**13. BEDEmpPercent**|numeric(4,1)||Percent employment gain/loss|
|**14. BEDEstabs**|numeric(12,0)||Number of establishments involved|
||||ingain/loss|
|**15. BEDEstabPercent**|<br>numeric(4,1)|||
|**16. Suppress**|char(1)||An indicator that the record contains|
||||confidential data that must be|
||||suppressed for public use: 0 = Not|
||||Suppressed;1 = Suppressed|
|Constraints||||
|1. Foreign Key (BED.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (BED.PeriodType, BED.Period) references (Periods.PeriodType, Periods.Period)||||
|3. Foreign Key (BED.StFips, BED.IndCodeType, BED.IndCode) references (IndustryCodes.StFips,||||
|IndustryCodes.CodeType, IndustryCodes.Code)||||
|4. Foreign Key (BED.StFips, BED.AreaType, BED.AreaTypeVersion, BED.Area) references||||
|(Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area)||||
|5. Foreign Key (BED.BEDTypeCode)references(BEDTypes.BEDTypeCode)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 35 -_ 

## **BuildingPermits** 

Table of building permits awarded per area and time period. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area:  e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
||||a geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||2|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||3|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to '00'|
|||2,3|where periodtype is annual.|
|**8. UnitType**|char(2)|Primary Key|Code for the type of building permit.|
|||1||
|**9. Units**|numeric(7,0)||Number of building permits.|
|**10. UnitCost**|numeric(12,0)||Building construction cost.|
|Constraints||||
|1. Foreign Key (BuildingPermits.StFips, BuildingPermits.UnitType)|||references (UnitTypes.StFips,|
|UnitTypes.UnitType)||||
|2. Foreign Key (BuildingPermits.PeriodYear) references (PeriodYears.PeriodYear)||||
|3. Foreign Key (BuildingPermits.PeriodType,||BuildingPermits.Period) references (Periods.PeriodType,||
|Periods.Period)||||
|4. Foreign Key (BuildingPermits.StFips, BuildingPermits.AreaType,|||BuildingPermits.AreaTypeVersion,|
|BuildingPermits.Area) references (Geographies.StFips, Geographies.AreaType,||||
|Geographies.AreaTypeVersion,Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 36 -_ 

[ICON: APPLE_CORE]

## **CES** 

## Employment estimates as reported by the CES. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|||4||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
|||4|area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
|||4|version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to|
|||4|represent a geographic area.  Front|
||||fill with zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period|
|||2|(e.g. annual, quarterly, monthly,|
||||etc.)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to 00|
|||1,2|where periodtype is annual.|
|**8. SeriesCodeType**|char(2)|Primary Key||
|||3,5||
|**9. SeriesCode**|char(8)|Primary Key|Industrial Series code.|
|||3,5||
|**10. Adjusted**|char(1)|Primary Key|Indicates if record contains|
||||seasonally adjusted data. 1 =|
||||adjusted;0 = not adjusted|
|**11. Benchmark**|char(4)||Benchmark year of the data.|
|**12. Prelim**|char(1)||Preliminary/revised flag. 0 = Not|
||||Preliminary;1 = Preliminary|
|**13. EmpCES**|numeric(9,0)||Number employed by place of|
||||work; actual number, not in|
||||thousands.|
|**14.**|numeric(9,0)||Number of Production workers;|
|**EmpProductionWorkers**||||
|**15. EmpFemaleWorkers**|numeric(9,0)|||
|**16. HoursPerWeek**|numeric(3,1)||Average hours worked per week.|
|**17. EarningsPerWeek**|numeric(8,2)||Average weekly earnings.|
|**18. EarningsPerHour**|numeric(6,2)||Average hourly earnings.|
|**19. SuppRecord**|char(1)||Suppress total record. 1 =|
||||Suppressed;0 = Not Suppressed|
|**20. SuppHoursEarnings**|char(1)|||
|**21. SuppProdWorkers**|char(1)|||
|**22. SuppFemaleWorkers**|char(1)|||
|**23. HoursAllWorkers**|numeric(3,1)||Average hours worked per week|
||||for all workers.|
|**24. EarningsAllWorkers**|numeric(8,2)||Average weekly earnings for all|
||||workers.|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 37 -_ 

|**25.**<br>numeric(6,2)|Average hourly earnings for all|
|---|---|
|**HourlyEarningsAllWorkers**|workers.|
|**26. SuppHEAllWrkr**<br>char(1)|Suppress hours and earnings for all|
||workers: 0 = not suppressed; 1 =|
||suppressed|
|Constraints||
|1. Foreign Key (CES.PeriodYear) references (PeriodYears.PeriodYear)||
|2. Foreign Key (CES.PeriodType, CES.Period) references (Periods.PeriodType, Periods.Period)||
|3. Foreign Key (CES.SeriesCodeType) references (IndCodeTypes.CodeType)||
|4. Foreign Key (CES.StFips, CES.AreaType, CES.AreaTypeVersion, CES.Area) references||
|(Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area)||
|5. Foreign Key (CES.StFips, CES.SeriesCodeType, CES.SeriesCode) references (CESCodes.StFips,||
|CESCodes.SeriesCodeType, CESCodes.SeriesCode)||
|6. Foreign Key (CES.Benchmark)references(Benchmark.Benchmark)||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 38 -_ 

## **Commute** 

Commuting patterns, by and to geographic areas. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|||3||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
|||3|area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
|||3|version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
|||3|a geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||2|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to 00|
|||1,2|where periodtype is annual.|
|**8. WorkStFips**|char(2)|Primary Key|State FIPS Code of workplace.|
|||3||
|**9. WorkAreaType**|char(2)|Primary Key|Area type code for workplace.|
|||3||
|**10.**|char(4)|Primary Key|Code indicating the area type|
|**WorkAreaTypeVersion**||3|version. Default = 0|
|**11. WorkArea**|char(6)|Primary Key|Area code for workplace.|
|||3||
|**12. Workers**|numeric(8,0)||Number of workers.|
|Constraints||||
|1. Foreign Key (Commute.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (Commute.PeriodType, Commute.Period) references (Periods.PeriodType,||||
|Periods.Period)||||
|3. Foreign Key (Commute.WorkStFips, Commute.WorkAreaType, Commute.WorkAreaTypeVersion,||||
|Commute.WorkArea) references (Geographies.StFips, Geographies.AreaType,||||
|Geographies.AreaTypeVersion, Geographies.Area)||||
|4. Foreign Key (Commute.StFips, Commute.AreaType, Commute.AreaTypeVersion, Commute.Area)||||
|references (Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion,||||
|Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 39 -_ 

## **CPI** 

## Table of Consumer Price Indices. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|||3||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
|||3|area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
|||3|version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent a|
|||3|geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||2|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to 00 where|
|||1,2|periodtype is annual.|
|**8. CPIType**|char(2)|Primary Key|Type of CPI measure.|
|||4||
|**9. CPISource**|char(1)|Primary Key|Source of the CPI measure.|
|**10. CPI**|numeric(8,3)|4|CPI measure.|
|**11. PctChangeY2Y**|numeric(6,1)||The percent change in the CPI from|
||||the period exactly one year ago to|
||||the currentperiod.|
|**12. PctChangeM2M**|numeric(6,1)||The percent change in the CPI from|
||||the period exactly one month ago to|
||||the current month.|
|Constraints||||
|1. Foreign Key (CPI.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (CPI.PeriodType, CPI.Period) references (Periods.PeriodType, Periods.Period)||||
|3. Foreign Key (CPI.StFips, CPI.AreaType, CPI.AreaTypeVersion,|||CPI.Area) references|
|(Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area)||||
|4. Foreign Key (CPI.CPIType) references (CPITypes.CPIType)||||
|5. Foreign Key (CPI.StFips,CPI.CPISource)references(CPISources.StFips,CPISources.CPISource)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 40 -_ 

## **CPIPlus** 

Enhanced CPI content with more indices and geographies available. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code|
|||3,4||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
|||3|area: set to ‘31’ for BLS CPI|
||||Geographic Area|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
|||3|version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent a|
|||3|geographic area. Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2006)|
|**6. PeriodType**|char(2)|Primary Key|Code describing the type of period|
|||2|(e.g., annual, quarterly, monthly,|
||||etc.)|
|**7. Period**|char(2)|Primary Key|Period code. Will be set to ‘00’ when|
|||1,2|periodtype is annual.|
|**8. Adjusted**|char(1)|Primary Key|Indicates whether record contains|
||||seasonally adjusted data: 0 = not|
||||adjusted;1 = adjusted|
|**9. CPIIndex**|char(1)|Primary Key|Indicates the index represented: U =|
||||All urban consumers; W = Urban|
||||wage earners and clerical workers|
|**10. CPIItem**|char(9)|Primary Key|Code that identifies market basket|
||||items included in the index|
|**11. CPISource**|char(1)|Primary Key|Source of the CPI measure|
|||4||
|**12. Basis**|char(4)||The 4-digit representation of the|
||||terminal year of the base period for|
||||the index (Current value is ‘1984’|
||||since the index uses 1982-84 as its|
||||baseperiod)|
|**13. CPI**|numeric(8,3)|4|CPI measure|
|**14. PctChangeY2Y**|numeric(6,1)|||
|**15. PctChangeM2M**|numeric(6,1)|||
|Constraints||||
|1. Foreign Key (CPIPlus.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (CPIPlus.PeriodType, CPIPlus.Period) references|||(Periods.PeriodType, Periods.Period)|
|3. Foreign Key (CPIPlus.StFips, CPIPlus.AreaType, CPIPlus.AreaTypeVersion, CPIPlus.Area) references||||
|(Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area)||||
|4. Foreign Key (CPIPlus.StFips, CPIPlus.CPISource) references (CPISources.StFips,||||
|CPISources.CPISource)||||
|5. Foreign Key (CPIPlus.CPIItem)references(CPIItems.CPIItem)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 41 -_ 

## **Demographics** 

Table of population estimates and demographic characteristics by geographic area and time period. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|Six-digit code assigned to represent|
||||a geographic area. Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||2|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||3|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|Period Code. Will be set to '00'|
|||2,3|where periodtype is annual.|
|**8. PopSource**|char(1)|Primary Key|Source Code for population data.|
|||1||
|**9. Population**|numeric(9,0)||Number representing the population|
||||total for the specified geographic|
||||area and timeperiod.|
|**10. Female**|numeric(9,0)||Total female population|
|**11. Male**|numeric(9,0)||Total male population|
|**12. MedianAge**|numeric(4,1)|||
|**13. MedianMale**|numeric(4,1)||Male population median age|
|**14. MedianFemale**|numeric(4,1)||Female population median age|
|**15. Totalunder5**|numeric(9,0)||Total population age under 5|
|**16. Femaleunder5**|numeric(9,0)||Female population age under 5|
|**17. Maleunder5**|numeric(9,0)||Female population age under 5|
|**18. Total5to9**|numeric(9,0)||Total population age 5-9|
|**19. Female5to9**|numeric(9,0)||Female population age 5-9|
|**20. Male5to9**|numeric(9,0)||Male population age 5-9|
|**21. Total10to14**|numeric(9,0)||Total population age 10-14|
|**22. Female10to14**|numeric(9,0)||Female population age 10-14|
|**23. Male10to14**|numeric(9,0)||Female population age 10-14|
|**24. Total15to19**|numeric(9,0)||Total population age 15-19|
|**25. Female15to19**|numeric(9,0)||Female population age 15-19|
|**26. Male15to19**|numeric(9,0)||Male population age 15-19|
|**27. Total15to17**|numeric(9,0)||Total population age 15-17|
|**28. Female15to17**|numeric(9,0)||Female population age 15-17|
|**29. Male15to17**|numeric(9,0)||Male population age 15-17|
|**30. Total18to19**|numeric(9,0)||Total population age 18-19|
|**31. Female18to19**|numeric(9,0)||Female population age 18-19|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 42 -_ 

|**32. Male18to19**|numeric(9,0)|Male population age 18-19|
|---|---|---|
|**33. Total20to24**|numeric(9,0)|Total population age 20-24|
|**34. Female20to24**|numeric(9,0)|Female population age 20-24|
|**35. Male20to24**|numeric(9,0)|Male population age 20-24|
|**36. Total20**|numeric(9,0)|Total population age 20|
|**37. Female20**|numeric(9,0)|Female population age 20|
|**38. Male20**|numeric(9,0)|Male population age 20|
|**39. Total21**|numeric(9,0)|Total population age 21|
|**40. Female21**|numeric(9,0)|Female population age 21|
|**41. Male21**|numeric(9,0)|Male population age 21|
|**42. Total22to24**|numeric(9,0)|Total population age 22-24|
|**43. Female22to24**|numeric(9,0)|Female population age 22-24|
|**44. Male22to24**|numeric(9,0)|Male population age 22-24|
|**45. Total25to34**|numeric(9,0)|Total population age 25-34|
|**46. Female25to34**|numeric(9,0)|Female population age 25-34|
|**47. Male25to34**|numeric(9,0)|Male population age 25-34|
|**48. Total25to29**|numeric(9,0)|Total population age 25-29|
|**49. Female25to29**|numeric(9,0)|Female population age 25-29|
|**50. Male25to29**|numeric(9,0)|Male population age 25-29|
|**51. Total30to34**|numeric(9,0)|Total population age 29-34|
|**52. Female30to34**|numeric(9,0)|Female population age 29-34|
|**53. Male30to34**|numeric(9,0)|Male population age 29-34|
|**54. Total35to44**|numeric(9,0)|Total population age 35-44|
|**55. Female35to44**|numeric(9,0)|Female population age 35-44|
|**56. Male35to44**|numeric(9,0)|Male population age 35-44|
|**57. Total35to39**|numeric(9,0)|Total population age 35-39|
|**58. Female35to39**|numeric(9,0)|Female population age 35-39|
|**59. Male35to39**|numeric(9,0)|Male population age 35-39|
|**60. Total40to44**|numeric(9,0)|Total population age 40-44|
|**61. Female40to44**|numeric(9,0)|Female population age 40-44|
|**62. Male40to44**|numeric(9,0)|Male population age 40-44|
|**63. Total45to54**|numeric(9,0)|Total population age 45-54|
|**64. Female45to54**|numeric(9,0)|Female population age 45-54|
|**65. Male45to54**|numeric(9,0)|Female population age 45-54|
|**66. Total45to49**|numeric(9,0)|Total population age 45-49|
|**67. Female45to49**|numeric(9,0)|Female population age 45-49|
|**68. Male45to49**|numeric(9,0)|Male population age 45-49|
|**69. Total50to54**|numeric(9,0)|Total population age 50-54|
|**70. Female50to54**|numeric(9,0)|Female population age 50-54|
|**71. Male50to54**|numeric(9,0)|Female population age 50-54|
|**72. Total55to59**|numeric(9,0)|Total population age 55-59|
|**73. Female55to59**|numeric(9,0)|Female population age 55-59|
|**74. Male55to59**|numeric(9,0)|Male population age 55-59|
|**75. Total60to64**|numeric(9,0)|Total population age 60-64|
|**76. Female60to64**|numeric(9,0)|Female population age 60-64|
|**77. Male60to64**|numeric(9,0)|Male population age 60-64|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 43 -_ 

|**78. Total60to61**|numeric(9,0)|Total population age 60-61|
|---|---|---|
|**79. Female60to61**|numeric(9,0)|Female population age 60-61|
|**80. Male60to61**|numeric(9,0)|Male population age 60-61|
|**81. Total62to64**|numeric(9,0)|Total population age 62-64|
|**82. Female62to64**|numeric(9,0)|Female population age 62-64|
|**83. Male62to64**|numeric(9,0)|Male population age 62-64|
|**84. Total65to69**|numeric(9,0)|Total population age 65-74|
|**85. Female65to69**|numeric(9,0)|Female population age 65-74|
|**86. Male65to69**|numeric(9,0)|Male population age 65-74|
|**87. Total65to66**|numeric(9,0)|Total population age 65-66|
|**88. Female65to66**|numeric(9,0)|Female population age 65-66|
|**89. Male65to66**|numeric(9,0)|Male population age 65-66|
|**90. Total67to69**|numeric(9,0)|Total population age 67-69|
|**91. Female67to69**|numeric(9,0)|Female population age 67-69|
|**92. Male67to69**|numeric(9,0)|Male population age 67-69|
|**93. Total70to74**|numeric(9,0)|Total population age 70-74|
|**94. Female70to74**|numeric(9,0)|Female population age 70-74|
|**95. Male70to74**|numeric(9,0)|Male population age 70-74|
|**96. Total75to84**|numeric(9,0)|Total population age 75-84|
|**97. Female75to84**|numeric(9,0)|Female population age 75-84|
|**98. Male75to84**|numeric(9,0)|Male population age 75-84|
|**99. Total75to79**|numeric(9,0)|Total population age 75-79|
|**100. Female75to79**|numeric(9,0)|Female population age 75-79|
|**101. Male75to79**|numeric(9,0)|Male population age 75-79|
|**102. Total80to84**|numeric(9,0)|Total population age 80-84|
|**103. Female80to84**|numeric(9,0)|Female population age 80-84|
|**104. Male80to84**|numeric(9,0)|Male population age 80-84|
|**105. Total85xx**|numeric(9,0)|Total population age 85 and over|
|**106. Female85xx**|numeric(9,0)|Female population age 85 and over|
|**107. Male85xx**|numeric(9,0)|Male population age 85 and over|
|**108. Total18xx**|numeric(9,0)|Total population age 18 and over|
|**109. Female18xx**|numeric(9,0)|Female population age 18 and over|
|**110. Male18xx**|numeric(9,0)|Male population age 18 and over|
|**111. Total21xx**|numeric(9,0)|Total population age 21 and over|
|**112. Female21xx**|numeric(9,0)|Female population age 21 and over|
|**113. Male21xx**|numeric(9,0)|Male population age 21 and over|
|**114. Total62xx**|numeric(9,0)|Total population age 62 and over|
|**115. Female62xx**|numeric(9,0)|Female population age 62 and over|
|**116. Male62xx**|numeric(9,0)|Male population age 62 and over|
|**117. Onerace**|numeric(9,0)|Population: one race|
|**118. White**|numeric(9,0)|One race: White|
|**119. Black**|numeric(9,0)|One race: Black or African American|
|**120. NAAN**|numeric(9,0)|One race: American Indian or|
|||Alaskan Native|
|**121. Asian**|numeric(9,0)|One race: Asian|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 44 -_ 

|**122. PacificIslander**|numeric(9,0)|One race: Native Hawaiian and|
|---|---|---|
|||Other Pacific Islander|
|**123. Other**|numeric(9,0)|One race: Other|
|**124. Twomoraces**|numeric(9,0)|Two or more races|
|**125. Hispanic**|numeric(9,0)|Hispanic or Latino|
|**126. Hispanicwhite**|numeric(9,0)|Hispanic or Latino: White Alone|
|**127. Hispanicblack**|numeric(9,0)|Hispanic or Latino: Black or African|
|||American|
|**128. Hispanicnaan**|numeric(9,0)|Hispanic or Latino: American Indian|
|||or Alaskan Native|
|**129. Hispanicasian**|numeric(9,0)|Hispanic or Latino: Asian|
|**130.**|numeric(9,0)||
|**Hispanicpacificisland**|||
|**131. Hispanicother**|numeric(9,0)|Hispanic or Latino: Some other race|
|||alone|
|**132.**|numeric(9,0)|Hispanic or Latino: Two or more|
|**Hispanic2moreraces**||races|
|Constraints|||
|1. Foreign Key (Demographics.StFips, Demographics.PopSource)||references|
|(PopulationSources.StFips, PopulationSources.PopSource)|||
|2. Foreign Key (Demographics.PeriodYear) references (PeriodYears.PeriodYear)|||
|3. Foreign Key (Demographics.PeriodType, Demographics.Period) references (Periods.PeriodType,|||
|Periods.Period)|||
|4. Foreign Key (Demographics.StFips, Demographics.AreaType, Demographics.AreaTypeVersion,|||
|Demographics.Area) references (Geographies.StFips, Geographies.AreaType,|||
|Geographies.AreaTypeVersion,Geographies.Area)|||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 45 -_ 

[ICON: APPLE_CORE]

## **EmpDB** 

The Employer Database, used for data on individual companies, subject to license agreements between States and the vendor. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|5|State FIPS code of employer's|
||||physical location.|
|**2. AreaType**|char(2)|5|Always township code '14' for New|
||||England states; always county code|
||||'04' for all other states.|
|**3. AreaTypeVersion**|char(4)|5|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|5|A 3-digit code assigned to county|
||||(fips) or township where business is|
||||located,front filled with zeroes.|
|**5. UniqueID**|char(9)|Primary Key|Employer's unique id assigned by|
||||InfoGroup.|
|**6. FEIN**|char(9)||Federal Employer Identification|
||||Number|
|**7. LastUpdate**|char(6)||Date of record's most recent update|
||||or verification from Yellow Page|
||||compilation or other sources.|
|**8. Name**|varchar(35)||Name by which the business is|
||||known or under which it conducts|
||||business.|
|**9. AddressP**|varchar(40)|||
|**10. CityP**|varchar(30)|||
|**11. StateP**|char(2)|||
|**12. ZipCodeP**|char(5)|||
|**13. ZipPlusP**|char(4)|||
|**14. Latitude**|numeric(11,6)||Employer's physical location -|
||||latitude|
|**15. Longitude**|numeric(11,6)||Employer's physical location -|
||||longitude|
|**16. GeoPrecisionCode**|<br>char(1)|4|Employer's physical location -|
||||geocode precision level code.  The|
||||precision of the longitude and|
||||latitude coordinates.|
|**17. CensusTract**|char(6)||Census tract - A statistical|
||||subdivision of a county|
|**18. CensusBlockGrp**|char(1)|||
|**19. AddressM**|varchar(40)|||
|**20. CityM**|varchar(30)|||
|**21. StateM**|char(2)|||
|**22. ZipCodeM**|char(5)|||
|**23. ZipPlusM**|char(4)|||
|**24. AddressL**|varchar(40)|||
|**25. CityL**|varchar(30)|||
|**26. StateL**|char(2)|||
|**27. ZipCodeL**|char(5)|||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 46 -_ 

|**28. ZipPlusL**|char(4)|||
|---|---|---|---|
|**29. TeleNum**|char(10)||Employer's telephone number with|
||||area code.|
|**30. ContactLastName**|varchar(30)||Contact's last name.|
|**31. ContactFirstName**|varchar(30)||Contact's first name.|
|**32. ContactTitle**|varchar(35)|1|Contact's title(e.g., HR director,|
||||owner, president)|
|**33. ContactTitleCode**|char(1)|1|Contact's title code|
|**34. ContactProTitle**|char(3)|1|Contact's professional title|
|**35. ContactGender**|char(1)||Contact's gender code|
|**36. ContactEmail**|varchar(60)||Contact's email address|
|**37. TollFreeTele**|char(10)|||
|**38. FaxNumber**|char(10)||Employer's fax number.|
|**39. WebURL**|varchar(40)||Employer's web site address (URL).|
|**40. BusinessDesc**|varchar(45)||Business description(a one-line 'line|
||||of business' identifier).|
|**41. PrimarySIC**|char(6)||Employer's primary SIC code.|
|**42. SIC2**|char(6)||Employer's SIC code #2|
|**43. SIC3**|char(6)||Employer's SIC code #3|
|**44. SIC4**|char(6)||Employer's SIC code #4|
|**45. SIC5**|char(6)||Employer's SIC code #5|
|**46. PrimaryNAICS**|char(8)||Employer's primary NAICS code.|
|**47. NAICS2**|char(8)||Employer's NAICS code #2|
|**48. NAICS3**|char(8)||Employer's NAICS code #3|
|**49. NAICS4**|char(8)||Employer's NAICS code #4|
|**50. NAICS5**|char(8)||Employer's NAICS code #5|
|**51. Ownership**|char(1)|2|Identifies whether the business is a|
||||government orprivate sector entity.|
|**52.**|char(1)|3|Identifies the business location|
|**LocationStatusCode**|||status.|
|**53.**|char(1)|1|Stock exchange code identifies the|
|**StockExchangeCode**|||Stock Exchange where the business|
||||conducts tradingactivity.|
|**54. StockTicker**|char(6)||Stock "TICKER" symbol is shown for|
||||companies that are traded on any|
||||public stock exchange or listed in|
||||the NASDAQ "over the counter"|
||||quotation system or other small|
||||exchanges(i.e.,Chicago Mercantile).|
|**55. WhiteCollarPct**|numeric(4,1)||Percentage of white collar|
||||employment|
|**56. WhiteCollarFlag**|char(1)||White collar indicator: 1 = Over 50%|
||||white collar employment|
|**57. EmpSizeRange**|char(2)|6|Code for the number of employees|
||||that work at this business location,|
||||byrange.|
|**58. EmpSizeValue**|numeric(9,0)||Number of employees who work at|
||||this location of the business.|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 47 -_ 

|**59. EmpSizeFlag**|char(1)|7|Code identifying how the|
|---|---|---|---|
||||employment (empsizval) was|
||||derived.|
|**60.**|char(2)|1||
|**AnnualSalesRange **||||
|**61. AnnualSales**|varchar(15)|1|Estimated annual sales volume of|
||||the business at this location.|
|**62. AnnualSalesFlag**|char(1)|||
|**63. YearEst**|char(4)||Year the business at this location|
||||was established or identified and|
||||added to the database.|
|**64. CreditCode**|char(1)|9|Credit rating code: an indicator of a|
||||business' financial status, or|
||||probable ability to pay.  These are|
||||modeled; they do not reflect actual|
||||payment history.  Users must be|
||||cautioned that these credit rating|
||||indicators should not be the sole|
||||factor in makinga credit decision.|
|**65. HeadQuartersID**|char(9)||The uniqueid of the regional or|
||||subsidiary headquarters of the|
||||business to which this record|
||||pertains.|
|**66. ParentID**|char(9)||The uniqueid of the corporate|
||||parent of the business to which this|
||||record pertains.  This may be the|
||||immediate or a higher level U.S.|
||||corporateparent of the business.|
|**67. UltimateParentID**|char(9)||The uniqueid of the ultimate|
||||corporate parent to which this|
||||record pertains.  This may be a|
||||higher level U.S. or foreign|
||||corporate parent of the business.|
||||Since all locations of a business have|
||||the same ultimate parent number,|
||||this field provides 'corporate|
||||ownership' linkage information.|
|**68. ForeignParentFlag**|char(1)||Foreign parent indicator.  A '1' =|
||||foreign affiliation.|
|**69. ExportImportFlag**|char(1)||Export Import indicator code|
||||Indicates the type of services|
||||provided.|
|**70. BusinessType**|char(1)||Code helps identify if the record|
||||represents a professional individual|
||||versus a firm.|
|**71. WorkAtHome**|char(1)||Work-at-home business.  A '1'|
||||indicates a home business.|
|**72. ReleaseNumber**|char(3)|8|empdb release number|
|Constraints||||
|1. Foreign Key (EmpDB.StockExchangeCode) references|||(StockExchange.StockExchangeCode)|
|2. Foreign Key (EmpDB.Ownership) references (PrivateGovt.Ownership)||||
|3. Foreign Key (EmpDB.LocationStatusCode)references|||(LocationStatuses.LocationStatusCode)|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 48 -_ 

4. Foreign Key (EmpDB.GeoPrecisionCode) references (GeoPrecisionCodes.GeoPrecisionCode) 

5. Foreign Key (EmpDB.StFips, EmpDB.AreaType, EmpDB.AreaTypeVersion, EmpDB.Area) references (Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area) 6. Foreign Key (EmpDB.EmpSizeRange) references (EmpSizeRange.EmpSizeRange) 

7. Foreign Key (EmpDB.EmpSizeFlag) references (EmpSizeFlag.EmpSizeFlag) 

8. Foreign Key (EmpDB.ReleaseNumber) references (EmpDBInf.ReleaseNumber) 

9. Foreign Key (EmpDB.CreditCode) references (CreditCodes.CreditCode) 

10. Foreign Key (EmpDB.ContactTitleCode) references (ContactTitles.ContactTitleCode) 11. Foreign Key (EmpDB.ContactProTitle) references (ContactProTitles.ContactProTitle) 

12. Foreign Key (EmpDB.AnnualSalesRange) references (AnnualSalesRanges.AnnSalesRange) 13. Foreign Key (EmpDB.AnnualSalesFlag) references (AnnualSalesCodes.AnnSalesCode) 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 49 -_ 

## **Income** 

## Income data by geography. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|||3,4||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
||||a geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||2|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to 00 where|
|||1,2|periodtype is annual.|
|**8. IncomeType**|char(2)|Primary Key|Code describing type of income|
|||3|measure.|
|**9. IncomeSource**|char(1)|Primary Key|Source of the income measure.|
|||4||
|**10. Income**|numeric(14,0)|3,4|Value of income measure.|
|**11. IncomeRank**|numeric(3,0)||Rank of income measure.|
|**12. Population**|numeric(10,0)||Population of the geography|
||||referenced. (Related to inctype e.g.,|
||||number of households, number of|
||||families,numberpersons.)|
|**13. ReleaseDate**|char(8)||Release Date (yyyymmdd)|
|Constraints||||
|1. Foreign Key (Income.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (Income.PeriodType, Income.Period) references (Periods.PeriodType, Periods.Period)||||
|3. Foreign Key (Income.StFips, Income.IncomeType) references (IncomeTypes.StFips,||||
|IncomeTypes.IncomeType)||||
|4. Foreign Key (Income.StFips, Income.IncomeSource) references (IncomeSources.StFips,||||
|IncomeSources.IncomeSource)||||
|5. Foreign Key (Income.StFips, Income.AreaType, Income.AreaTypeVersion, Income.Area) references||||
|(Geographies.StFips,Geographies.AreaType,Geographies.AreaTypeVersion,Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 50 -_ 

## **Industry** 

[ICON: APPLE_CORE]

Table of covered employment by industry collected for the ES-202/QCEW report. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|||4||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
||||a geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||2|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to 00 where|
|||1,2|periodtype is annual.|
|**8. IndCodeType**|char(2)|Primary Key|Code describing the industry code|
|||4|type.|
|**9. IndCode**|char(10)|Primary Key|A code used in the classification of|
|||4|establishments by type of activity in|
||||which they are engaged. For codes|
||||not 6 characters long, left justify and|
||||blank (ASCII 32) fill.  Either SIC or|
||||NAICS code can be used.|
|**10. Ownership**|char(2)|Primary Key|Ownership is a 2-digit indicator that|
|||3|identifies the employer by public or|
||||private ownership.|
|**11. Prelim**|char(1)||Preliminary/revised flag: 0 = Not|
||||Preliminary;1 = Preliminary|
|**12. Firms**|numeric(8,0)||The number of firms in the industry.|
|**13. Establishments**|numeric(8,0)||The number of employer|
||||establishments (reporting units) in|
||||the industry.|
|**14. QuarterAvgEmp**|numeric(9,0)||The number of workers employed in|
||||the industry.|
|**15. Month1Emp**|numeric(9,0)||Employment on the first month of|
||||thequarter.|
|**16. Month2Emp**|numeric(9,0)||Employment on the second month of|
||||thequarter.|
|**17. Month3Emp**|numeric(9,0)||Employment on the third month of|
||||thequarter.|
|**18.**|numeric(9,0)||Average employment for the quarter|
|**TopEmployerAvgEmp**|||or year of the top employer for the|
||||specified geography and industry|
||||code.|
|**19. TotalWages**|numeric(14,0)||The total wages paid to all workers in|
||||the industryfor theperiod.|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 51 -_ 

|**20. AvgWeeklyWage**|numeric(8,0)<br>Average weekly wage per worker.|
|---|---|
|**21. TaxableWages**|numeric(14,0)<br>Total taxable wages.|
|**22. UIContributions**|numeric(12,0)<br>Employer contributions to the UI|
||fund.|
|**23. Suppress**|char(1)<br>An indicator that the record contains|
||confidential data that must be|
||suppressed for public use: 0 = Not|
||Suppressed; 1 = Suppress|
||employment & wage data|
|Constraints||
|1. Foreign Key (Industry.PeriodYear) references (PeriodYears.PeriodYear)||
|2. Foreign Key (Industry.PeriodType, Industry.Period) references (Periods.PeriodType, Periods.Period)||
|3. Foreign Key (Industry.Ownership) references (Ownerships.Ownership)||
|4. Foreign Key (Industry.StFips, Industry.IndCodeType, Industry.IndCode) references||
|(IndustryCodes.StFips,|IndustryCodes.CodeType, IndustryCodes.Code)|
|5. Foreign Key (Industry.StFips, Industry.AreaType, Industry.AreaTypeVersion, Industry.Area)||
|references (Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion,||
|Geographies.Area)||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 52 -_ 

[ICON: APPLE_CORE]

## **IOWage** 

Table of wages by industry and occupation, from any source, including OEWS. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1,5,6||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area : e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to|
||||represent a geographic area.  Front|
||||fill with zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||3|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period|
|||4|(e.g. annual, quarterly, monthly,|
||||etc.).|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to '00'|
|||3,4|where periodtype is annual.|
|**8. IndCodeType**|char(2)|Primary Key|Code describing the industry code|
|||6|type.|
|**9. IndCode**|char(10)|Primary Key|A code used in the classification of|
|||6|establishments by type of activity|
||||in which they are engaged. For|
||||codes not 6 characters long, left|
||||justify and blank (ASCII 32) fill.|
||||Either SIC or NAICS code can be|
||||used.|
|**10. OccCodeType**|char(2)|Primary Key|Code describing the type of|
|||5|occupational coding system.|
|**11. OccCode**|char(10)|Primary Key|The occupational classification|
|||5|code used by the state for this|
||||data element. This code could be a|
||||DOT, OEWS, SOC, CENSUS, etc. For|
||||codes not 10 characters long, left|
||||justifyand blank(ASCII 32)fill.|
|**12. WageSource**|char(1)|Primary Key|A code representing the source of|
|||1|the wage data.|
|**13. EmpCount**|numeric(10,0)||Total employment.|
|**14. RateType**|char(1)|Primary Key|Code indicating what the rate|
|||2|shown is|
|**15. ResponseRate**|numeric(6,0)||Response rate for the occupation's|
||||actual or real survey data.  Does|
||||NOT include imputed data in the|
||||rate calculation.|
|**16. MeanWage**|numeric(9,2)||Mean wage for the occupation.|
|**17. EntryWage**|numeric(9,2)||Entry level wage for the|
||||occupation, mean of the first third|
||||(ALC definition).|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 53 -_ 

|**18. ExperiencedWage**|numeric(9,2)|Experienced level wage for the|
|---|---|---|
|||occupation, mean of upper two|
|||thirds(ALC definition).|
|**19. Percentile10Wage**|numeric(9,2)|Wage at tenth percentile.|
|**20. Percentile25Wage**|numeric(9,2)|Wage at twenty-fifth percentile.|
|**21. MedianWage**|numeric(9,2)|Median wage of the occupation;|
|||by definition, also the wage at|
|||fiftiethpercentile.|
|**22. Percentile75Wage**|numeric(9,2)|Wage at seventy-fifth percentile.|
|**23. Percentile90Wage**|numeric(9,2)|Wage at ninetieth percentile.|
|**24. UserDefinedPct**|numeric(3,0)|User defined percentile.|
|**25.**|numeric(9,2)||
|**UserDefinedPctWage **|||
|**26.**|numeric(3,0)||
|**UserDefinedRangeLoPct**|||
|**27.**|numeric(3,0)||
|**UserDefinedRangeHiPct**|||
|**28.**|numeric(9,2)||
|**UserDefinedRangeMean**|||
|**29.**|numeric(6,2)|Relative percent error on wage.|
|**WageRelativePctError**|||
|**30. EmpRelativePctError**|numeric(6,2)|Relative percent error on|
|||employment.|
|**31. PanelCode**|char(6)|Reference panel code (yyyymm)|
|**32. SuppressWage**|char(1)|An indicator that the wage values|
|||should be suppressed.|
|**33. SuppressAll**|char(1)|An indicator that the record|
|||contains confidential data.|
|**34. SuppressEmp**|char(1)|An indicator that the employment|
|||values should be suppressed.|
|Constraints|||
|1. Foreign Key (IOWage.StFips, IOWage.WageSource) references (WageSources.StFips,|||
|WageSources.WageSource)|||
|2. Foreign Key (IOWage.RateType) references (WageRateTypes.RateType)|||
|3. Foreign Key (IOWage.PeriodYear) references (PeriodYears.PeriodYear)|||
|4. Foreign Key (IOWage.PeriodType, IOWage.Period) references (Periods.PeriodType, Periods.Period)|||
|5. Foreign Key (IOWage.StFips, IOWage.OccCodeType, IOWage.OccCode) references|||
|(OccupationCodes.StFips,|OccupationCodes.CodeType, OccupationCodes.Code)||
|6. Foreign Key (IOWage.StFips, IOWage.IndCodeType, IOWage.IndCode) references|||
|(IndustryCodes.StFips, IndustryCodes.CodeType, IndustryCodes.Code)|||
|7. Foreign Key (IOWage.StFips, IOWage.AreaType, IOWage.AreaTypeVersion, IOWage.Area)|||
|references (Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion,|||
|Geographies.Area)|||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 54 -_ 

## **JOLTS** 

The Job Openings and Labor Turnover Survey program provides national estimates of rates and levels for job openings, hires, and total separations.  Total separations are further broken out into quits, layoffs and discharges, and other separations. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code|
|||4||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area: e.g county service delivery area,|
||||MSA_x000D_|
||||BLS Region = 06|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type version.|
||||Default = 0|
|**4. Area**|char(6)|Primary Key|Six-digit code assigned to represent a|
||||geographic area. Front fill with zeroes|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of calendar|
|||1|year|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||2|annual, quarterly, monthly)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to 00 where|
|||1,2|periodtype is annual.|
|**8. IndCodeType**|char(2)|Primary Key|Code for the type industry:_x000D_|
|||4|05 = SIC_x000D_|
||||10 = NAICS|
|**9. IndCode**|char(10)|Primary Key|Industry code|
|||4||
|**10. Adjusted**|char(1)|Primary Key|Indicates whether data is seasonally|
||||adjusted_x000D_|
||||0 = Not Adjusted_x000D_|
||||1 = Adjusted|
|**11. JOLTSTypeCode**|char(2)|Primary Key|Indicator of the type of data:|
|||3||
|**12. Prelim**|char(2)||0 = Not prelim_x000D_|
||||1 = Prelim|
|**13. Value**|numeric(9,0)||In thousands|
|**14. Rate**|numeric(8,2)||Percent|
|Constraints||||
|1. Foreign Key (JOLTS.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (JOLTS.PeriodType, JOLTS.Period) references (Periods.PeriodType, Periods.Period)||||
|3. Foreign Key (JOLTS.JOLTSTypeCode) references (JOLTSTypes.JOLTSTypeCode)||||
|4. Foreign Key (JOLTS.StFips, JOLTS.IndCodeType, JOLTS.IndCode) references (IndustryCodes.StFips,||||
|IndustryCodes.CodeType, IndustryCodes.Code)||||
|5. Foreign Key (JOLTS.StFips, JOLTS.AreaType,||JOLTS.AreaTypeVersion, JOLTS.Area) references||
|(Geographies.StFips,Geographies.AreaType,Geographies.AreaTypeVersion,Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 55 -_ 

[ICON: APPLE_CORE]

## **LaborForce** 

Employment and unemployment estimates reported from the Local Area Unemployment Statistics. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|||3||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
|||3|area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
|||3|version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent a|
|||3|geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||2|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to 00 where|
|||1,2|periodtype is annual.|
|**8. Adjusted**|char(1)|Primary Key|Indicates if record contains|
||||seasonally adjusted data. 1 =|
||||adjusted;0 = not adjusted|
|**9. Prelim**|char(1)||Indicates preliminary data: 0 = Not|
||||preliminary;1 =preliminary|
|**10. Benchmark**|char(4)||Benchmark year of the data.|
|**11. LaborForce**|numeric(9,0)||Civilian labor force.|
|**12. Employed**|numeric(9,0)||Number employed by place of|
||||residence.|
|**13. Unemployed**|numeric(9,0)||Number unemployed.|
|**14. UnemployedRate**|numeric(5,1)||Unemployment rate.|
|**15. CLFPRate**|numeric(5,1)||Civilian labor force participation rate|
|**16. EmpPopRatio**|numeric(5,1)||Employment to population ratio|
|Constraints||||
|1. Foreign Key (LaborForce.PeriodYear)||references (PeriodYears.PeriodYear)||
|2. Foreign Key (LaborForce.PeriodType,||LaborForce.Period)|references (Periods.PeriodType,|
|Periods.Period)||||
|3. Foreign Key (LaborForce.StFips, LaborForce.AreaType, LaborForce.AreaTypeVersion,||||
|LaborForce.Area) references (Geographies.StFips, Geographies.AreaType,||||
|Geographies.AreaTypeVersion, Geographies.Area)||||
|4. Foreign Key (LaborForce.Benchmark)||references(Benchmark.Benchmark)||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 56 -_ 

[ICON: APPLE_CORE]

## **License** 

Table of individual licenses authorized by a state. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|||9||
|**2. AreaType**|char(2)|9|Code describing type of geographic|
||||area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|9|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|9|A 6-digit code assigned to represent|
||||a geographic area.  Front fill with|
||||zeroes.|
|**5. LicenseID**|char(10)|Primary Key|A code that identifies the license for|
||||the specific occupational title.|
|**6. LicAuthID**|char(3)||A unique code that identifies the|
||||licensingauthority.|
|**7. LicenseTitle**|varchar(200)|||
|**8. LicenseDesc**|varchar(max)|||
|**9. LicenseType**|char(1)|2|An indicator of the type of license|
|**10. Exam**|char(1)|5|An indicator of the exam|
||||requirements of the license|
|**11. Education**|char(1)|6|An indicator of the education|
||||requirements of the license|
|**12. ContinuingEdu**|char(1)||An indicator of the continuing|
||||education requirements of the|
||||license|
|**13. Certification**|char(1)|8|An indicator of the certification|
||||requirements of the license|
|**14. Experience**|char(1)|4||
|**15. Criminal**|char(1)|7|An indicator of restrictions on|
||||criminal records|
|**16. PhysicalReq**|char(1)|3||
|**17. Veteran**|char(1)|1|An indicator of the type of veteran’s|
||||preference|
|**18. Inactive**|char(1)|1|An indicator of the active status of|
||||the license|
|**19. LicenseURL**|char(200)||Uniform Resource Locator for license|
||||information|
|**20. LicenseUpdated**|char(8)||Last date the license information was|
||||updated. Format:yyyymmdd|
|Constraints||||
|1. Foreign Key (License.Veteran) references (LicenseVeteran.LicenseVeteran)||||
|2. Foreign Key (License.LicenseType) references (LicenseTypes.LicenseType)||||
|3. Foreign Key (License.PhysicalReq) references (LicensePhysicalReqs.LicensePhysicalReq)||||
|4. Foreign Key (License.Experience) references (LicenseExperience.LicenseExperience)||||
|5. Foreign Key (License.Exam) references (LicenseExams.LicenseExam)||||
|6. Foreign Key (License.Education) references (LicenseEducation.LicenseEducation)||||
|7. Foreign Key (License.Criminal) references||(LicenseCriminal.LicenseCriminal)||
|8. Foreign Key (License.Certification)references(LicenseCertifications.LicenseCertification)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 57 -_ 

9. Foreign Key (License.StFips, License.AreaType, License.AreaTypeVersion, License.Area) references (Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area) 10. Foreign Key (License.Inactive) references (LicenseActiveStatuses.LicenseActiveStatus) 11. Foreign Key (License.ContinuingEdu) references (LicenseContinuingEdu.LicenseContinuingEdu) 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 58 -_ 

[ICON: APPLE_CORE]

## **LicenseAuthorities** 

## Table of licensing authorities for the state. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
||||a geographic area.  Front fill with|
||||zeroes.|
|**5. LicAuthID**|char(3)|Primary Key|A unique identifier for the licensing|
||||authority.|
|**6. Department**|varchar(255)||The State department responsible|
||||for regulation.|
|**7. Division**|varchar(255)||The division, office, or section of the|
||||department responsible for|
||||regulation(maybe NULL).|
|**8. Board**|varchar(255)||Complete name of regulatory board.|
|**9. Address1**|varchar(75)||Address of licensing authority.|
|**10. Address2**|varchar(75)||Second line for address.|
|**11. City**|varchar(30)||City.|
|**12. State**|char(2)|||
|**13. ZipCode**|char(5)||Postal zip code for the business|
||||address.|
|**14. ZipExt**|char(4)||Four digit zip code extension.|
|**15. Latitude**|numeric(11,6)||Physical location - latitude|
|**16. Longitude**|numeric(11,6)||Physical location - longitude|
|**17.**|char(1)|1|Physical location - geocode precision|
|**GeoPrecisionCode**|||level code.  The precision of the|
||||longitude and latitude coordinates.|
|**18. Telephone**|varchar(10)||Phone number.|
|**19. TeleExt**|varchar(10)||Phone number extension.|
|**20. Fax**|varchar(10)||Licensing agency fax number.|
|**21. Contact**|varchar(50)||Name of person to contact at the|
||||licensingagency.|
|**22. Email**|varchar(70)||Licensing agency e-mail address.|
|**23. URL**|varchar(200)||Uniform Resource Locator for the|
||||licensingagency.|
|Constraints||||
|1. Foreign Key (LicenseAuthorities.GeoPrecisionCode) references||||
|(GeoPrecisionCodes.GeoPrecisionCode)||||
|2. Foreign Key (LicenseAuthorities.StFips,||LicenseAuthorities.AreaType,||
|LicenseAuthorities.AreaTypeVersion, LicenseAuthorities.Area) references (Geographies.StFips,||||
|Geographies.AreaType,Geographies.AreaTypeVersion,Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 59 -_ 

## **LicenseHistory** 

Table of the number of licenses awarded for a selected occupation. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||4||
|**2. AreaType**|char(2)||Code describing type of geographic|
||||area:  e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)||Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)||A 6-digit code assigned to represent|
||||a geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||2|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to '00'|
|||1,2|where periodtype is annual.|
|**8. LicenseID**|char(10)|Primary Key|A code that identifies the license for|
|||4|the specific occupational title.|
|**9.**|char(2)|Primary Key|A code that identifies the type of|
|**LicenseNumberType **||3|license information.|
|**10. LicenseNumber**|numeric(9,0)|3|A numerical value that represents|
||||the number of licenses awarded.|
|Constraints||||
|1. Foreign Key (LicenseHistory.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (LicenseHistory.PeriodType, LicenseHistory.Period)|||references (Periods.PeriodType,|
|Periods.Period)||||
|3. Foreign Key (LicenseHistory.LicenseNumberType) references||||
|(LicenseNumberTypes.LicenseNumberType)||||
|4. Foreign Key (LicenseHistory.StFips, LicenseHistory.LicenseID) references (License.StFips,||||
|License.LicenseID)||||
|5. Foreign Key (LicenseHistory.StFips, LicenseHistory.AreaType, LicenseHistory.AreaTypeVersion,||||
|LicenseHistory.Area)|references (Geographies.StFips, Geographies.AreaType,|||
|Geographies.AreaTypeVersion,Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 60 -_ 

## **Population** 

This table contains population estimates for a geographic area and time period. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|<br>State FIPS code.|
|||1||
|**2. AreaType**|char(2)|Primary Key|<br>Code describing type of geographic|
||||area:  e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|<br>Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|<br>A 6-digit code assigned to represent|
||||a geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|<br>Character representation of the|
|||2|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|<br>Code describing type of period (e.g.|
|||3|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|<br>Period Code.  Will be set to '00'|
|||2,3|where periodtype is annual.|
|**8. PopSource**|char(1)|Primary Key|<br>Source Code for population data.|
|||1||
|**9. Population**|numeric(10,0)||Number representing the population|
||||total for the specified geographic|
||||area and timeperiod.|
|**10. ReleaseDate**|char(8)||Release Date (yyyymmdd)|
|Constraints||||
|1. Foreign Key (Population.StFips, Population.PopSource) references (PopulationSources.StFips,||||
|PopulationSources.PopSource)||||
|2. Foreign Key (Population.PeriodYear)||references (PeriodYears.PeriodYear)||
|3. Foreign Key (Population.PeriodType,||Population.Period)|references (Periods.PeriodType,|
|Periods.Period)||||
|4. Foreign Key (Population.StFips, Population.AreaType, Population.AreaTypeVersion,||||
|Population.Area) references (Geographies.StFips, Geographies.AreaType,||||
|Geographies.AreaTypeVersion,Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 61 -_ 

## **ProgramCompleters** 

This table contains information about program completers. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1,4,5||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
|||1,5|area:  e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
|||1,5|version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
|||1,5|a geographic area.  Front fill with|
||||zeroes.|
|**5. InstitutionCode**|char(10)|Primary Key||
|||1||
|**6. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||2|calendar year (e.g. 2006).|
|**7. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||3|annual, quarterly, monthly, etc.).|
|**8. Period**|char(2)|Primary Key|Period Code.  Will be set to '00'|
|||2,3|where periodtype is annual.|
|**9. CodeType**|char(2)|Primary Key|Code describing the type of|
|||4|occupation or training  code.|
|**10. Code**|char(10)|Primary Key|The classification code used by the|
|||1,4|state for this data element. This code|
||||could be DOT, OEWS, CIP, Cluster,|
||||SOC, Census, etc. For codes not 10|
||||character long, left justify and blank|
||||(ASCII 32)fill.|
|**11. CompleterType**|char(2)|Primary Key|A 2-digit code representing type of|
||||program completer.|
|**12. Completers**|numeric(8,0)||Number of program completers.|
|**13. Placements**|varchar(max)|||
|Constraints||||
|1. Foreign Key (ProgramCompleters.StFips, ProgramCompleters.AreaType,||||
|ProgramCompleters.AreaTypeVersion, ProgramCompleters.Area,||||
|ProgramCompleters.InstitutionCode) references (Schools.StFips, Schools.AreaType,||||
|Schools.AreaTypeVersion, Schools.Area, Schools.InstitutionCode)||||
|2. Foreign Key (ProgramCompleters.PeriodYear) references (PeriodYears.PeriodYear)||||
|3. Foreign Key (ProgramCompleters.PeriodType, ProgramCompleters.Period) references||||
|(Periods.PeriodType, Periods.Period)||||
|4. Foreign Key (ProgramCompleters.StFips, ProgramCompleters.CodeType, ProgramCompleters.Code)||||
|references (OccupationCodes.StFips, OccupationCodes.CodeType, OccupationCodes.Code)||||
|5. Foreign Key (ProgramCompleters.StFips, ProgramCompleters.AreaType,||||
|ProgramCompleters.AreaTypeVersion, ProgramCompleters.Area) references (Geographies.StFips,||||
|Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area)||||
|6. Foreign Key (ProgramCompleters.StFips, ProgramCompleters.CompleterType) references||||
|(CompleterTypes.StFips,CompleterTypes.CompleterType)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 62 -_ 

## **Programs** 

This table contains information about occupational training 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1,2,3,4,5,6||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
|||1,5|area:  e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
|||1,5|version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
|||1,5|a geographic area.  Front fill with|
||||zeroes.|
|**5. InstitutionCode**|char(10)|Primary Key||
|||1||
|**6. CodeType**|char(2)|Primary Key|Code describing the type of|
|||2,3|occupation or training code.|
|**7. Code**|char(10)|Primary Key|The classification code used by the|
|||1,2,3|state for this data element. This code|
||||could be DOT, OEWS, CIP, Cluster,|
||||SOC, Census, etc. For codes not 10|
||||characters long, left justify and blank|
||||(ASCII 32)fill.|
|**8. CompleterType**|char(2)|Primary Key|A 2-digit code representing type of|
|||6|program completed.|
|**9. Length**|numeric(8,2)|4|The length of the training program at|
||||the institution (years, months,|
||||weeks,etc.)|
|**10. LengthType**|char(2)|4|The identifying code assigned to the|
||||program length.|
|**11. ProgCost**|numeric(6,0)||The cost of the program.|
|**12. ProgTitle**|varchar(200)||Title used by the training provider for|
||||theprogram.|
|**13. ProgDesc**|varchar(max)||A narrative summary of the program|
||||objective.|
|**14. CIPCodeType**|char(2)|2|Code describing the CIP version|
||||used.|
|**15. CIPCode**|char(10)|2|A 10-digit code assigned to a|
||||Classification of Institutional|
||||Programs(CIP) program title.|
|**16. URL**|varchar(200)||Uniform Resource Locator for the|
||||program.|
|**17. Classroom**|char(1)||Classroom instruction: 1 = yes; 0 = no|
|**18. Online**|char(1)||Online instruction: 1 = yes; 0 = no|
|**19. ClassTime**|char(1)||Times class is held; see field values|
|**20. ETPLApproval**|char(1)||ETPL approved program: 1 =|
||||approved;0 = not approved|
|Constraints||||
|1. Foreign Key (Programs.StFips, Programs.AreaType, Programs.AreaTypeVersion, Programs.Area,||||
|Programs.InstitutionCode)references||(Schools.StFips,Schools.AreaType,Schools.AreaTypeVersion,||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 63 -_ 

Schools.Area, Schools.InstitutionCode) 

2. Foreign Key (Programs.StFips, Programs.CIPCodeType, Programs.CIPCode) references (OccupationCodes.StFips, OccupationCodes.CodeType, OccupationCodes.Code) 

3. Foreign Key (Programs.StFips, Programs.CodeType, Programs.Code) references 

(OccupationCodes.StFips, OccupationCodes.CodeType, OccupationCodes.Code) 

4. Foreign Key (Programs.StFips, Programs.LengthType) references (LengthTypes.StFips, LengthTypes.LengthType) 

5. Foreign Key (Programs.StFips, Programs.AreaType, Programs.AreaTypeVersion, Programs.Area) references (Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area) 

6. Foreign Key (Programs.StFips, Programs.CompleterType) references (CompleterTypes.StFips, CompleterTypes.CompleterType) 

7. Foreign Key (Programs.ClassTime) references (ClassTime.ClassTime) 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 64 -_ 

[ICON: APPLE_CORE]

## **ProjectionsMatrix** 

Table of the industry-occupation employment matrix. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|||5||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic area:|
||||e.g. county, service delivery area, MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type version.|
||||Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent a|
||||geographic area.  Front fill with zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the calendar|
|||1,3,4|year (e.g. 2021).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g. Long-|
|||2,3,4|term projections, short-term projections,|
||||etc.)|
|**7. Period**|char(2)|Primary Key|Period Code. Will be set to 00 where|
|||1,2,3,4|periodtype in annual based.|
|**8. MatrixIndCode**|char(15)|Primary Key|Matrix industry code from Micro Matrix.|
|||4||
|**9. MatrixOccCode**|char(10)|Primary Key|Matrix occupation code from Micro|
|||3|Matrix.  For codes not 10 characters long,|
||||leftjustifyand blank(ASCII 32)fill.|
|**10. ProjectedYear**|char(4)|Primary Key|Character representation of the projected|
|||3,4|calendar year (e.g. 2031).|
|**11. EstimatedEmp**|numeric(9,0)||The base-year employment estimate.|
|**12. ProjectedEmp**|numeric(9,0)||The projected-year employment|
||||estimate.|
|**13. PctEstInd**|numeric(6,2)||The percentage of the base-year|
||||employment estimate for the indicated|
||||industry represented by the base-year|
||||employment estimate for the indicated|
||||occupation within that industry.|
|**14. PctEstOcc**|numeric(6,2)||The percentage of the base-year|
||||employment estimate for the indicated|
||||occupation represented by the base-year|
||||employment estimate for the indicated|
||||industrywithin that occupation.|
|**15. PctProjInd**|numeric(6,2)||The percentage of projected employment|
||||for the indicated industry represented by|
||||projected employment for the indicated|
||||occupation within that industry.|
|**16. PctProjOcc**|numeric(6,2)||The percentage of the projected|
||||employment estimate for the indicated|
||||occupation represented by the projected|
||||employment estimate for the indicated|
||||industrywithin that occupation.|
|**17. NumericChange **|numeric(9,0)|||
|**18. PercentChange**|numeric(9,4)||Percent change over period.((projemp-|
||||estemp)/estemp)*100|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 65 -_ 

|**19. GrowthRate**|numeric(8,4)||A value representing the annualized|
|---|---|---|---|
||||percentage growth. This value is|
||||calculated by dividing the Projected year|
||||by the Base year. Taking the results to the|
||||1/n power, where n is the number of|
||||years in the  projection period,|
||||subtracting 1 from the result and|
||||multiplying that result by 100. Ie.|
||||grrate=(((projemp/estemp)^1/n)-1)*100|
|**20. GrowthCode**|char(2)|5|A descriptor to allow for state specific|
||||interpretation of the industry or|
||||occupation.|
|**21. Exits**|numeric(9,0)||The number of exits from the labor force.|
|**22. AnnualExits**|numeric(9,0)||The annual number of exits from the|
||||labor force.|
|**23. Transfers**|numeric(9,0)||The number of transfers from one|
||||occupation to another.|
|**24. AnnualTransfers**|numeric(9,0)||The annual number of transfers from one|
||||occupation to another.|
|**25. Change**|numeric(9,0)||Numeric Change between the projected|
||||estimate and the base estimate.|
|**26. AnnualChange**|numeric(9,0)||The annual change in employment from|
||||estimated toprojected.|
|**27. Openings**|numeric(9,0)||Total openings = Exits+Transfers+Change|
|**28. AnnualOpenings **|numeric(9,0)||Annual openings|
|**29. Suppress**|char(1)||An indicator that the record contains|
||||confidential data that must be|
||||suppressed for public use: 0 = Not|
||||Confidential;1 = Confidential|
|Constraints||||
|1. Foreign Key (ProjectionsMatrix.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (ProjectionsMatrix.PeriodType, ProjectionsMatrix.Period) references||||
|(Periods.PeriodType, Periods.Period)||||
|3. Foreign Key (ProjectionsMatrix.MatrixOccCode, ProjectionsMatrix.PeriodYear,||||
|ProjectionsMatrix.PeriodType, ProjectionsMatrix.Period, ProjectionsMatrix.ProjectedYear) references||||
|(OccDirectories.MatrixOccCode, OccDirectories.PeriodYear, OccDirectories.PeriodType,||||
|OccDirectories.Period, OccDirectories.ProjectedYear)||||
|4. Foreign Key (ProjectionsMatrix.MatrixIndCode, ProjectionsMatrix.PeriodYear,||||
|ProjectionsMatrix.PeriodType, ProjectionsMatrix.Period, ProjectionsMatrix.ProjectedYear) references||||
|(IndDirectories.MatrixIndCode, IndDirectories.PeriodYear,|||IndDirectories.PeriodType,|
|IndDirectories.Period,|IndDirectories.ProjectedYear)|||
|5. Foreign Key (ProjectionsMatrix.StFips, ProjectionsMatrix.GrowthCode) references||||
|(GrowthCodes.StFips, GrowthCodes.GrowthCode)||||
|6. Foreign Key (ProjectionsMatrix.StFips, ProjectionsMatrix.AreaType,||||
|ProjectionsMatrix.AreaTypeVersion, ProjectionsMatrix.Area) references (Geographies.StFips,||||
|Geographies.AreaType,Geographies.AreaTypeVersion,Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 66 -_ 

## **SalesRevenue** 

## Revenue from retail sales. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area:  e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
||||a geographic area. Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||2|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||3|annual, quarterly, monthly, etc.).|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to '00'|
|||2,3|where periodtype is annual.|
|**8. SalesType**|char(2)|Primary Key|Code describing the type of sales|
|||1|statistic. (e.g. Retail sales are coded|
||||01)|
|**9. Sales**|numeric(15,0)|1|Sales Dollar amount.|
|Constraints||||
|1. Foreign Key (SalesRevenue.StFips, SalesRevenue.SalesType) references (SalesTypes.StFips,||||
|SalesTypes.SalesType)||||
|2. Foreign Key (SalesRevenue.PeriodYear)||references (PeriodYears.PeriodYear)||
|3. Foreign Key (SalesRevenue.PeriodType,||SalesRevenue.Period) references (Periods.PeriodType,||
|Periods.Period)||||
|4. Foreign Key (SalesRevenue.StFips, SalesRevenue.AreaType, SalesRevenue.AreaTypeVersion,||||
|SalesRevenue.Area) references (Geographies.StFips, Geographies.AreaType,||||
|Geographies.AreaTypeVersion,Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 67 -_ 

## **Schools** 

## Table of training providers in the state. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area:  e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
||||a geographic area.   Front fill with|
||||zeroes.|
|**5. InstitutionCode**|char(10)|Primary Key||
|**6. InstitutionType **|char(2)|1||
|**7.**|char(1)|||
|**InstitutionOwnership**||||
|**8. InstName1**|varchar(80)||Primary name of the training|
||||institution.|
|**9. InstName2**|varchar(80)||Secondary name of the training|
||||institution.|
|**10. Address1**|varchar(35)||Address of the training institution.|
|**11. Address2**|varchar(35)||Address of the training institution.|
|**12. City**|varchar(30)||City.|
|**13. State**|char(2)|||
|**14. ZipCode**|char(5)||Postal zip code for the business|
||||address.|
|**15. ZipExt**|char(4)||Zip code extension.|
|**16. Latitude**|numeric(11,6)||Physical location - latitude|
|**17. Longitude**|numeric(11,6)||Physical location - longitude|
|**18. GeoPrecisionCode**|char(1)|2|Physical location - geocode precision|
||||level code.  The precision of the|
||||longitude and latitude coordinates.|
|**19. Telephone**|char(10)||Training institution telephone|
||||number.|
|**20. TeleExt**|varchar(10)||Training institution telephone|
||||extension.|
|**21. Fax**|char(10)||Training institution fax number.|
|**22. URL**|varchar(200)||Uniform Resource Locator.|
|**23. Contact**|varchar(50)||Name of person to contact at the|
||||traininginstitution.|
|**24. DistanceLearn**|char(1)||Distance Learn: 1=distance learn or|
||||online or correspondence ONLY;|
||||0=otherwise|
|**25. SatelliteCampus**|char(1)||Satellite Campus: 1=yes; 0=no (main|
||||campus)|
|Constraints||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 68 -_ 

1. Foreign Key (Schools.StFips, Schools.InstitutionType) references (InstitutionTypes.StFips, InstitutionTypes.InstitutionType) 

2. Foreign Key (Schools.GeoPrecisionCode) references (GeoPrecisionCodes.GeoPrecisionCode) 

3. Foreign Key (Schools.StFips, Schools.AreaType, Schools.AreaTypeVersion, Schools.Area) references 

(Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area) 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 69 -_ 

## **Supply** 

Completer data for all occupational training providers in the state. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||3,4,6||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
|||6|area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
|||6|version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
|||6|a geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||1|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||2|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to '00'|
|||1,2|where periodtype is annual.|
|**8. InstitutionType**|char(2)|Primary Key||
|||4||
|**9.**|char(1)|Primary Key||
|**InstitutionOwnership**||5||
|**10. CodeType**|char(2)|Primary Key|Code describing the type of|
|||3|occupation or training code.|
|**11. Code**|char(10)|Primary Key|The classification code used by the|
|||3|state for this data element. This|
||||code could be DOT, OEWS, CIP,|
||||Cluster, SOC, Census, etc. For codes|
||||not 10 characters long, left justify|
||||and blank(ASCII 32)fill.|
|**12. CompleterType**|char(2)|Primary Key|A 2-digit code representing type of|
||||program completer. (e.g.|
||||Completers with Associates Degree|
||||are coded as 03)|
|**13. Completers**|numeric(8,0)||Number of program completers.|
|Constraints||||
|1. Foreign Key (Supply.PeriodYear) references||(PeriodYears.PeriodYear)||
|2. Foreign Key (Supply.PeriodType, Supply.Period) references (Periods.PeriodType, Periods.Period)||||
|3. Foreign Key (Supply.StFips, Supply.CodeType, Supply.Code) references (OccupationCodes.StFips,||||
|OccupationCodes.CodeType, OccupationCodes.Code)||||
|4. Foreign Key (Supply.StFips, Supply.InstitutionType) references (InstitutionTypes.StFips,||||
|InstitutionTypes.InstitutionType)||||
|5. Foreign Key (Supply.InstitutionOwnership) references (InstitutionOwnerships.InstitutionOwnership)||||
|6. Foreign Key (Supply.StFips, Supply.AreaType, Supply.AreaTypeVersion, Supply.Area) references||||
|(Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area)||||
|7. Foreign Key (Supply.StFips, Supply.CompleterType) references (CompleterTypes.StFips,||||
|CompleterTypes.CompleterType)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 70 -_ 

## **TaxRevenues** 

|**TaxRevenues**|**TaxRevenues**|||
|---|---|---|---|
|Revenues from taxes.||||
|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area:  e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
||||a geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||2|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||3|annual, quarterly, monthly, etc.).|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to '00'|
|||2,3|where periodtype is annual.|
|**8. TaxType**|char(2)|Primary Key|A 2-digit code identifying type of tax.|
|||1||
|**9. TaxRevenue**|numeric(15,0)||Tax revenues.|
|Constraints||||
|1. Foreign Key (TaxRevenues.StFips, TaxRevenues.TaxType) references (TaxTypes.StFips,||||
|TaxTypes.TaxType)||||
|2. Foreign Key (TaxRevenues.PeriodYear)||references (PeriodYears.PeriodYear)||
|3. Foreign Key (TaxRevenues.PeriodType,||TaxRevenues.Period) references (Periods.PeriodType,||
|Periods.Period)||||
|4. Foreign Key (TaxRevenues.StFips, TaxRevenues.AreaType, TaxRevenues.AreaTypeVersion,||||
|TaxRevenues.Area) references (Geographies.StFips, Geographies.AreaType,||||
|Geographies.AreaTypeVersion,Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 71 -_ 

## **TransferPayments** 

Table of Government Transfer Payments. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area:  e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
||||a geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||2|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||3|annual, quarterly, monthly, etc.)|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to '00'|
|||2,3|where periodtype is annual.|
|**8. PaymentType**|char(2)|Primary Key|A 2-digit code indicating type of|
|||1|payment.|
|**9. AmountPaid**|numeric(10,0)||Amount paid in thousands.|
|Constraints||||
|1. Foreign Key (TransferPayments.StFips, TransferPayments.PaymentType) references||||
|(TransferPaymentTypes.StFips, TransferPaymentTypes.PaymentType)||||
|2. Foreign Key (TransferPayments.PeriodYear) references (PeriodYears.PeriodYear)||||
|3. Foreign Key (TransferPayments.PeriodType, TransferPayments.Period) references||||
|(Periods.PeriodType, Periods.Period)||||
|4. Foreign Key (TransferPayments.StFips, TransferPayments.AreaType,||||
|TransferPayments.AreaTypeVersion, TransferPayments.Area)|||references (Geographies.StFips,|
|Geographies.AreaType,Geographies.AreaTypeVersion,Geographies.Area)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 72 -_ 

## **UIClaims** 

Table of the numbers of Unemployment Insurance Claims by geographic area. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||4,5,6||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
|||6|area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type version.|
|||6|Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent a|
|||6|geographic area.  Front fill with|
||||zeroes.|
|**5. PeriodYear**|char(4)|Primary Key|Character representation of the|
|||2|calendar year (e.g. 2006).|
|**6. PeriodType**|char(2)|Primary Key|Code describing type of period (e.g.|
|||3|annual, quarterly, monthly, etc.).|
|**7. Period**|char(2)|Primary Key|Period Code.  Will be set to '00'|
|||2,3|where periodtype is annual.|
|**8. ClaimType**|char(1)|Primary Key|Code describing the type of claim: 1 =|
||||Initial; 2 = Continued; 9 = Unknown|
|**9. OccCodeType**|char(2)|Primary Key|Code describing the type of|
|||4|occupational code.|
|**10. OccCode**|char(10)|Primary Key|The occupational classification code|
|||4|used by the state for this data|
||||element. This code could be DOT,|
||||OEWS, SOC, Census, etc. For codes|
||||not 10 characters long, left justify and|
||||blank(ASCII 32)fill.|
|**11. IndCodeType**|char(2)|Primary Key|Code describing the type of industry|
|||5|classification code.|
|**12. IndCode**|char(10)|Primary Key|The industry classification code used|
|||5|by the state for this data element.|
||||This code could be SIC or NAICS. For|
||||codes not 6 characters long, left|
||||justifyand blank(ASCII 32)fill.|
|**13. AgeGroup**|char(2)|Primary Key|Code identifying the age group.|
|**14. RaceCode**|char(2)|Primary Key|Code indicating race of claimants.|
|||1||
|**15. Ethnicity**|char(1)|Primary Key|Hispanice (H), or Not Hispanic (N)|
|||8||
|**16. GenderCode**|char(1)|Primary Key|Gender code.|
|||7||
|**17. Claimants**|numeric(8,0)||Number of UI claimants.|
|**18. WeeksComp**|numeric(8,0)||Weeks compensated|
|**19. FirstPayments**|numeric(8,0)||Number of first payments|
|**20. Duration**|numeric(4,1)||Average number of weeks of current|
||||unemployment.|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 73 -_ 

Constraints 1. Foreign Key (UIClaims.RaceCode) references (RaceCodes.RaceCode) 2. Foreign Key (UIClaims.PeriodYear) references (PeriodYears.PeriodYear) 3. Foreign Key (UIClaims.PeriodType, UIClaims.Period) references (Periods.PeriodType, Periods.Period) 4. Foreign Key (UIClaims.StFips, UIClaims.OccCodeType, UIClaims.OccCode) references (OccupationCodes.StFips, OccupationCodes.CodeType, OccupationCodes.Code) 5. Foreign Key (UIClaims.StFips, UIClaims.IndCodeType, UIClaims.IndCode) references (IndustryCodes.StFips, IndustryCodes.CodeType, IndustryCodes.Code) 6. Foreign Key (UIClaims.StFips, UIClaims.AreaType, UIClaims.AreaTypeVersion, UIClaims.Area) references (Geographies.StFips, Geographies.AreaType, Geographies.AreaTypeVersion, Geographies.Area) 7. Foreign Key (UIClaims.GenderCode) references (Genders.GenderCode) 8. Foreign Key (UIClaims.Ethnicity) references (EthnicityCodes.EthnicityCode) 9. Foreign Key (UIClaims.AgeGroup) references (AgeGroups.AgeGroup) 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 74 -_ 

## **Crosswalk Tables** 

## **IndustryXIndustry** 

|**Crosswalk Tables**<br>**IndustryXIndustry**|**Crosswalk Tables**<br>**IndustryXIndustry**|**Crosswalk Tables**<br>**IndustryXIndustry**|**Crosswalk Tables**<br>**IndustryXIndustry**|
|---|---|---|---|
|Table mappingindcodes and codetypes to related indcodes and codetypes||||
|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|**1. StFips**|char(2)|Primary Key|State FIPS code|
|||1||
|**2. CodeType**|char(2)|Primary Key|Code describing the type of industry|
|||1|classification code.|
|**3. Code**|char(10)|Primary Key|The classification codes used by the|
|||1|state for this data element. This could|
||||be SIC,NAICS,or other industrycode.|
|**4. CodeType2**|char(2)|Primary Key|Code describing the type of industry|
|||1|classification code.|
|**5. Code2**|char(10)|Primary Key|The classification codes used by the|
|||1|state for this data element. This could|
||||be SIC,NAICS,or other industrycode.|
|Constraints||||
|1. Foreign Key (IndustryXIndustry.StFips, IndustryXIndustry.CodeType2, IndustryXIndustry.Code2)||||
|references (IndustryCodes.StFips, IndustryCodes.CodeType, IndustryCodes.Code)||||
|2. Foreign Key (IndustryXIndustry.StFips, IndustryXIndustry.CodeType, IndustryXIndustry.Code)||||
|references(IndustryCodes.StFips,IndustryCodes.CodeType,IndustryCodes.Code)||||



## **LayTitleXOcc** 

Table of lay titles and the associated occupation codes. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1||
|**2. OccCodeType**|char(2)|Primary Key|Code describing the occupational|
|||1|code.|
|**3. OccCode**|char(10)|Primary Key|The occupational classification code|
|||1|used by the state for this data|
||||element. This code could be DOT,|
||||OEWS, SOC, Census, etc. For codes|
||||not 10 characters long, left justify and|
||||blank(ASCII 32)fill.|
|**4. LayTitleCode**|char(5)|Primary Key|Code associated with a particular lay|
||||title.|
|Constraints||||
|1. Foreign Key (LayTitleXOcc.StFips, LayTitleXOcc.OccCodeType, LayTitleXOcc.OccCode) references||||
|(OccupationCodes.StFips, OccupationCodes.CodeType, OccupationCodes.Code)||||
|2. Foreign Key (LayTitleXOcc.LayTitleCode)||references(LayTitles.LayTitleCode)||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 75 -_ 

## **LicenseXLicense** 

## Table of re-licensing 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code|
|||1||
|**2. LicenseID**|char(10)|Primary Key|Unique ID for license|
|||1||
|**3. ReLicenseID**|char(10)|Primary Key|Unique ID of the secondary license|
|||1||
|Constraints||||
|1. Foreign Key (LicenseXLicense.StFips,||LicenseXLicense.ReLicenseID) references (License.StFips,||
|License.LicenseID)||||
|2. Foreign Key (LicenseXLicense.StFips,||LicenseXLicense.LicenseID) references (License.StFips,||
|License.LicenseID)||||



[ICON: APPLE_CORE]

## **LicenseXOcc** 

## Table mapping state licenses to associated occupations. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||1||
|**2. LicenseID**|char(10)|Primary Key|A code that identifies the license for|
||||the specific occupational title.|
|**3. OccCodeType**|char(2)|Primary Key|Code for Occupation code type.|
|||1||
|**4. OccCode**|char(10)|Primary Key|Occupation code.|
|||1||
|Constraints||||
|1. Foreign Key (LicenseXOcc.StFips,||LicenseXOcc.OccCodeType, LicenseXOcc.OccCode) references||
|(OccupationCodes.StFips, OccupationCodes.CodeType, OccupationCodes.Code)||||
|2. Foreign Key (LicenseXOcc.StFips,||LicenseXOcc.LicenseID) references (License.StFips,||
|License.LicenseID)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 76 -_ 

[ICON: APPLE_CORE]

## **MatrixXInd** 

This table crosswalks the MicroMatrix industry codes to other industry codes, such as NAICS or SIC. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|||3||
|**2. MatrixIndCode**|char(15)|Primary Key|Industry matrix code from Micro|
||||Matrix.|
|**3. PeriodYear**|char(4)|Primary Key||
|||1||
|**4. PeriodType**|char(2)|Primary Key||
|||2||
|**5. Period**|char(2)|Primary Key||
|||1,2||
|**6. ProjectedYear**|char(4)|Primary Key||
|**7. IndCodeType**|char(2)|Primary Key|Code describing the type of industry|
|||3|classification code.|
|**8. IndCode**|char(10)|Primary Key|The classification code used by the|
|||3|state for this data element. This could|
||||be a SIC or NAICS code.|
|**9. SubTotal**|char(1)||Sum level of the information.|
|Constraints||||
|1. Foreign Key (MatrixXInd.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (MatrixXInd.PeriodType, MatrixXInd.Period) references (Periods.PeriodType,||||
|Periods.Period)||||
|3. Foreign Key (MatrixXInd.StFips, MatrixXInd.IndCodeType, MatrixXInd.IndCode) references||||
|(IndustryCodes.StFips, IndustryCodes.CodeType, IndustryCodes.Code)||||
|4. Foreign Key (MatrixXInd.SubTotal)references(IndSubLevels.SubTotal)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 77 -_ 

[ICON: APPLE_CORE]

## **MatrixXOcc** 

This table crosswalks MicroMatrix occupation codes to other occuptation codes, such as SOC or OES. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code.|
|**2. MatrixOccCode**|char(15)|Primary Key|Occupation matrix code from Micro|
||||Matrix.  For codes not 10 characters|
||||long, left justify and blank (ASCII 32)|
||||fill.|
|**3. PeriodYear**|char(4)|Primary Key||
|||1||
|**4. PeriodType**|char(2)|Primary Key||
|||2||
|**5. Period**|char(2)|Primary Key||
|||1,2||
|**6. ProjectedYear**|char(4)|Primary Key||
|**7. OccCodeType**|char(2)|Primary Key|Code describing the type of|
||||occupation or training classification|
||||code.|
|**8. OccCode**|char(10)|Primary Key|The classification code used by the|
||||state for this data element.|
|**9. SubTotal**|char(1)|3|Sum level of the information.|
|Constraints||||
|1. Foreign Key (MatrixXOcc.PeriodYear) references (PeriodYears.PeriodYear)||||
|2. Foreign Key (MatrixXOcc.PeriodType, MatrixXOcc.Period) references (Periods.PeriodType,||||
|Periods.Period)||||
|3. Foreign Key (MatrixXOcc.SubTotal) references (OccSubLevels.SubTotal)||||
|4. Foreign Key (MatrixXOcc.StFips, MatrixXOcc.OccCodeType, MatrixXOcc.OccCode) references||||
|(OccupationCodes.StFips,OccupationCodes.CodeType,OccupationCodes.Code)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 78 -_ 

## **OccupationXOccupation** 

Table mapping occupation codes and codetypes to related occupation codes and codetypes 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS code|
|||1||
|**2. CodeType**|char(2)|Primary Key|Code describing the type of|
|||1|occupation or training classification|
||||code.|
|**3. Code**|char(10)|Primary Key|The classification code used by the|
|||1|state for this data element. This code|
||||could be a CIP, DOT, SOC, or other|
||||occupational code.|
|**4. CodeType2**|char(2)|Primary Key|Code describing the type of|
|||1|occupation or training classification|
||||code.|
|**5. Code2**|char(10)|Primary Key|The classification code used by the|
|||1|state for this data element. This code|
||||could be a CIP, DOT, SOC, or other|
||||occupational code.|
|Constraints||||
|1. Foreign Key|(OccupationXOccupation.StFips, OccupationXOccupation.CodeType2,|||
|OccupationXOccupation.Code2) references (OccupationCodes.StFips, OccupationCodes.CodeType,||||
|OccupationCodes.Code)||||
|2. Foreign Key|(OccupationXOccupation.StFips, OccupationXOccupation.CodeType,|||
|OccupationXOccupation.Code) references||(OccupationCodes.StFips, OccupationCodes.CodeType,||
|OccupationCodes.Code)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 79 -_ 

## **Administrative Tables** 

## **IndustrySums** 

|**IndustrySums**||||
|---|---|---|---|
|Count of employers|for each industry,|with detailed source.||
|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|**1. StFips**|char(2)|Primary Key|State FIPS Code|
|||1||
|**2. AreaType**|char(2)|Primary Key|Code describing type of geographic|
||||area: e.g. county, service delivery|
||||area,MSA.|
|**3. AreaTypeVersion**|char(4)|Primary Key|Code indicating the area type|
||||version. Default = 0|
|**4. Area**|char(6)|Primary Key|A 6-digit code assigned to represent|
||||a geographic area.  Front fill with|
||||zeroes.|
|**5. IndCodeType**|char(2)|Primary Key|Code describing the type of industry|
|||1|classification code.|
|**6. IndCode**|char(10)|Primary Key|The classification code used by the|
|||1|state for this data element. This|
||||could be a SIC or NAICS code. For|
||||codes not 6 characters long, left|
||||justifyand blank(ASCII 32)fill.|
|**7. IndSource**|char(1)|Primary Key|Detail source of industry aggregates:|
||||E = empdb; S = stfirms|
|**8. Employers**|numeric(6,0)||Count of employers|
|Constraints||||
|1. Foreign Key (IndustrySums.StFips, IndustrySums.IndCodeType, IndustrySums.IndCode) references||||
|(IndustryCodes.StFips, IndustryCodes.CodeType, IndustryCodes.Code)||||
|2. Foreign Key (IndustrySums.StFips, IndustrySums.AreaType, IndustrySums.AreaTypeVersion,||||
|IndustrySums.Area) references (Geographies.StFips, Geographies.AreaType,||||
|Geographies.AreaTypeVersion,Geographies.Area)||||



## **TableList** 

This table contains one record for each table in the Workforce Information Database. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. TableName**|varchar(32)|Primary Key|Name of Workforce Information|
||||table.|
|**2. TableDesc**|varchar(200)||Description of table.|
|**3. TableType**|char(1)||A code indicating the type of|
||||Workforce Information table.|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 80 -_ 

## **TableSource** 

Table describing how and from whom to obtain source data for the WID tables. 

|**FieldName**|**FieldType **|**Constraint**|**FieldDesc**|
|---|---|---|---|
|**1. StFips**|char(2)|Primary Key|State FIPS Code.|
|**2. TableName**|varchar(32)|Primary Key|Name of Workforce Information|
|||1|table.|
|**3. Supplier**|varchar(100)||Name of department or office.|
|**4. Contact**|varchar(200)||Individual to contact.|
|**5. Telephone**|char(10)||Telephone number.|
|**6. TeleExt**|varchar(10)||Telephone extension.|
|**7. LastUpdate**|date||Date this source data was last|
||||updated.|
|**8. NextUpdate**|date||Date this source data will be updated|
||||next.|
|**9. FileType **|varchar(10)||File format of source data.|
|**10. Info**|varchar(max)||Narrative text describing any other|
||||relevant information regarding this|
||||source data.|
|Constraints||||
|1. Foreign Key (TableSource.TableName)||references (TableList.TableName)||
|2. Foreign Key (TableSource.StFips)references(StateFips.StFips)||||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 81 -_ 

## **Workforce Information Database Version 3.0** 

Standard Field Values (alphabetic by field name) 

agegrouptype 

annsalesrange 

Tables Referenced: agegroups 

Tables Referenced: empdb 

Values: 

Values: 

01             Current Population Survey 02             Census Population 

## agegroup 

Tables Referenced: agegroups, uiclaims Values: 

|01|01|16 and over|
|---|---|---|
|01|02|16 to 19|
|01|03|20 and over|
|01|04|20 to 24|
|01|05|25 to 34|
|01|06|35 to 44|
|01|07|45 to 54|
|01<br>01<br>02<br>02<br>02|08<br>09<br>10<br>11<br>12|55 to 64<br>65 and over<br>Less than 5 years<br>5 to 9 years<br>10 to 14 years|
|02|13|15 to 19 years|
|02|14|20 to 24 years|
|02|15|25 to 29 years|
|02|16|30 to 34 years|
|02<br>02|17<br>18|35 to 44 years<br>45 to 54 years|
|02|19|55 to 59 years|
|02|20|60 to 64 years|
|02|21|65 to 69 years|
|02|22|70 to 74 years|
|02|23|75 to 79 years|
|02<br>02|24<br>25|80 to 84 years<br>85years and over|



A 1-499 B 500-999 C            1000 – 2499 D            2500 – 4999 E             5000 – 9999 F             10,000  - 19,999 G            20,000  - 49,999 H            50,000  - 99,999 I             100,000 - 499,999 J             500,000 - 999,999 K            1,000,000+ 

## areatype 

Tables Referenced: areatype, bed, buildingpermits, ces, 

commute, cpi, cpiplus, demographics, empdb, geographies, income, projectionsmatrix, iowage, industry, industrysum, jvs, laborforce, license, licenseauthorities, licensehistory, population, programcompleters, programs, salesrevenue, schools, subgeographies, supply, taxrevenues, transferpayments, uiclaims 

Values: 

00           US 

01           State 03           SDA 04           County 05           Minor Civil Division 

06           BLS Region 

07           Broad Geographic Area (BGA) 

- 08           Economic Development Region 

09           Planning Region 

10           Labor Market Area 

11           City 

12           Town 

- 13           Township 

- 14           Municipality/Suburb 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 82 -_ 

15           Workforce Investment Region 16           One Stop Area 17           Workforce Development Area 18           Job Center Area 19           Congressional District 20           Census Places 25           Metropolitan New England City and Town Area (NECTA) 26 Micropolitan New England City and Town Area (NECTA) 27 New England City and Town Area (NECTA) Divisions 28 Combined New England City and Town Area (NECTA) 30 Balance of State 31 Metropolitan Statistical Area 32 Micropolitan Statistical Area 33 Metropolitan Division 34 Combined Statistical Area 35 EEO County Group 41 BLS CPI areas 

## areatypeversion 

- - surveys/metro micro/about/omb bulletins.html) Programs began implementing 2301 in 2025. 

businesstype 

Tables Referenced: empdb 

Values: 1             Individual 2             Firm 

ciplevel 

Tables Referenced: cipcodes 

Values: 

2             2-digit CIP 4             4-digit CIP 6             6-digit CIP 

Tables Referenced: areatype, bed, 

buildingpermits, ces, commute, cpi, cpiplus, demographics, empdb, geographies, income, projectionsmatrix, iowage, industry, industrysum, laborforce, license, licenseauthorities, licensehistory, population, programcompleters, programs, salesrevenue, schools, subgeographies, supply, taxrevenues, transferpayments, uiclaims 

Values: 

classtime 

Tables Referenced: classtime, programs Values: 

1             Day 2             Night 3             Weekend 4             Day/Night 

5             Day/Weekend 6 Night/Weekend 7 Day/Night/Weekend 

0000 Default (used for 00, 04, etc) 2010 2010 vintage (CPI areas) 2001 MSA definitions released in March 2020 2301     MSA definitions released in July 2023 

In general, these are the vintage of area types that can change and the above will depend on how much historical data states include. 

MSA versions come from OMB bulletin numbers (available here: - https://www.census.gov/programs 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 83 -_ 

completertype Tables Referenced: completertypes, programcompleters, programs, supply Values: 

- 00 Sum of all types 

- 01 Postsec. Awards/Cert./Diplomas; < 1 yr 

- 02 Postsec. Awards/Cert./Diplomas; 1-2 yrs 

- 03 Associate’s Degree 04 Postsec. Awards/Cert./Diplomas; 2-4 yrs 

- 05           Bachelor’s Degree 

- 06           Postbaccalaureate Certificates 

- 07           Master’s Degree 08           Post-Master’s Certificates 09           Doctor’s Degrees 10           First-professional Degrees 11           First-professional Cert. (Post-Degree) 17           All Postsecondary Certificates 21           Secondary 

- 22           Postsec. Awards/Cert./Diplomas; <4 yrs. 

- 23           Graduate degrees combined 30           OJT = on-the-job training 31           Employment & training program completers 

- 32           Military separatees 

- 33           Apprenticeship completers 

- 34           Job Corps completers 40           Certificates < 2 yrs. 

- 50-70      State-Defined Completion Types 99           Unidentified 

contactgender Tables Referenced: empdb Values: Blank      Unknown F             Female M            Male 

contactprotitle Tables Referenced: contactprotitles, empdb 

_These are added and changed by the provider_ 

Values: 

- AGT       Insurance and real estate agents CPA       Certified Public Accountant DC          Doctor of Chiropractic Medicine DDS       Doctor of Dental Surgery DO         Doctor of Osteopathic Medicine DPM      Doctor of Podiatry DVM      Doctor of Veterinary Medicine MD         Doctor of Medicine APN Advanced Practice Nurse AUD Audiologist CFP Certified Financial Planner CNM  CERTIFIED NURSE MIDWIFE CNP CERTIFIED NURSE PRACTITIONER CNS Certified Nurse Specialist DMD  DOCTOR OF DENTAL MEDICNE DNP Doctor Of Nursing Practice DPT DOCTOR OF PHYSICAL THERAPY EA Enrolled Agent FNP Family Nurse Practitioner LAC Licensed Acupuncturist LMT Licensed Massage Therapist LO Loan Officer LPC Licensed Professional Counselor LPN LICENSED PRACTICAL NURSE MA Master Of Arts MFT Marriage And Family Therapist MLO MORTGAGE LOAN ORIGINATOR MSN Master Of Science In Nursing MSW   Master Of Social Work ND NATUROPATHIC DOCTOR NP NURSE PRACTITIONER OD DOCTOR OF OPTOMETRY OT OCCUPATIONAL THERAPY OTR Occupational Therapist Register PA PHYSICIAN ASSISTANT PAC Physician Assistant - Certified PE PROFESSIONAL ENGINEER PHD DOCTOR OF PHILOSOPHY PNP Pediatric Nurse Practitioner PT PHYSICAL THERAPIST RD Registered Dietitian RDH Registered Dental Hygienist RN REGISTERED NURSE 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 84 -_ 

contacttitlecode Tables Referenced: contacttitles, empdb Values: blank      Unknown ! IT # Finance $ Chief Administrative Officer (CAO) % Chief Marketing Officer (CMO) & Business Development ( Director ) Executive + Facilities - Sales . International / Manufacturing : Educator = Engineering/Technical > General Manager ? Office Manager @ CIO/CTO [ Operations \ Marketing ] Other ^ Human Resources _ Site Manager { Regional Manager 1 Owner 2 President 3 Manager 4 Executive Director 5 Principal 6 Publisher 7 Administrator 8 Religious Leader 9 Partner A Chairman B Vice Chairman C            Chief Executive Officer D            Director (Public Co) E             Chief Operating Officer (COO) F             Chief Financial Officer (CFO) G            Treasurer H            Controller I              Executive Vice President J           Senior Vice President K            Vice President 

L             Administration Executive M            Corporate Communications Executive N            Data Processing Executive O            Finance Executive P             Human  Resources Executive Q            Telecommunications Executive R            Marketing Executive S             Operations Executive T             Sales Executive U            Corporate Secretary V            General Counsel W           Executive Officer X            Plant Manager Y            Purchasing Agent Z             Minister 

cpisource 

Tables Referenced: cpi, cpiplus, cpisources Values: 

1             Bureau of Labor Statistics 6-8          State Defined 

cpitype 

Tables Referenced: cpi, cpitype Values: 

01           CPI-U all items 1982-84=100, not seasonally adjusted 02           CPI-U all items 1982-84=100, seasonally adjusted 50-70 StateDefined 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 85 -_ 

educategory Tables Referenced: blseducation Values: 0             All Education Levels 1 Less than high school 2 High school diploma or equivalent 3 Some college, no degree 4 Postsecondary non-degree award 5 Associate's degree 6 Bachelor's degree 7 Master's degree 8 Doctoral or professional degree N Not available 

geoprecisioncode Tables Referenced: empdb, geographies, geoprecisioncodes, licenseauthorities, schools, statefirms 

Values: 

0 Address 2             Zip+2 centroid 4             Zip+4 centroid X            ZIP code centroid 

growthcode 

Tables Referenced: growthcodes, indoccmatrix 

empsizeflag 

Tables Referenced: empdb, empsizeflag 

Values: 

1             Collected from source 

2             Estimated by the Employer Database supplier 

empsizerange 

Values: 

50-70      State-Defined Growth Types DD         Declining GG         Growing SS           Stable 

incomesource 

Tables Referenced: income, incomesources 

Values: 

Tables Referenced: empdb, empsizerange Values: 

A            1-4 B            5-9 C            10-19 D            20-49 E             50-99 F             100-249 G            250-499 H            500-999 I              1,000-4,999 J              5,000-9,999 K            10,000+ 

1             Census 2             HUD 3             BEA 6 – 8      State Defined Income Sources 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 86 -_ 

institutiontype Tables Referenced: institutiontypes, schools, supply Values: 00           All Institutions 01           Secondary Schools 02           Public Adult Schools with occupational programs 03           Two-year, Technical, and Community Colleges 04           Four-year Colleges and Universities 05           Private Business and Technical Schools 06           JTPA Programs 07           Apprenticeship Programs 08           Hospital or Health Programs 09           Other education and training institutions 20           Law Enforcement Academies 21           Aviation and Flight Schools 22           WIA Providers 23           Department of Defense 50-70      State-Defined Institution Types 99           Not Available 

licenseeducation Tables Referenced: licenseeducation Values: 0 No education required 1 Specific course required 2 Degree required 9 Undetermined 

licenseexam 

Tables Referenced: licenseexams 

Values: 

0              no exam 1              state exam required 2              third-party exam required 3              both state and third-party exams required 4             choice of state or third-party exam 9              undetermined 

licenseexperience 

lengthtype Tables Referenced: lengthtypes, programs Values: 01           Years 02           Semesters 03           Trimesters 04           Quarters 05           Months 06           Weeks 07           Days 08 Hours 09 Semester hours 10 Credit hours 99 Unknown 

Tables Referenced: licenseexperience 

Values: 

0              no experience 

1              affidavit or referral 2              experience 3              current employment 9              undetermined 

licensephysicalreq 

Tables Referenced: licensephysicalreqs 

Values: 

0              no physical requirements 

1              vision test required 

2              physical exam 

3              more significant physical requirements 9              undetermined 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 87 -_ 

licenseactivestatus Tables Referenced: licenseactivestatus 

Values: 

0            active 1            no new licenses issued 2 replaced 3 no longer licensed 

locationstatuscode 

Tables Referenced: empdb, locationstatuses 

Values: 

naicsuper Tables Referenced: naicssectors, naicssupersectors Values: 

- 10           Total All Industries 

- 101         Goods-Producing 

- 1011       Natural Resources and Mining Sectors 11, 21 

- 1012       Construction Sector 23 1013       Manufacturing Sectors 31-33 

- 102         Service-Providing 1021       Trade, Transportation, and Utilities Sectors 42, 44-45, 48-49, 22 

- 1022       Information Sector 51 

0 Single location firm 1 Headquarters/home office 2             Branch office 3             Subsidiary headquarters 

naicslevel 

Tables Referenced: naicscodes, naicslevels 

Values: 

- 1023       Financial Activities Sectors 52, 53 1024       Professional and Business Services Sectors 54, 55, 56 

- 1025       Education and Health Services Sectors 61, 62 

- 1026       Leisure and Hospitality Sectors 71, 72 1027       Other Services Sector 81 1028       Public Administration Sector 92 1029       Unclassified Sector 99 1030       Tribal employment 

0             Total (000000) 1             Supersector 2             Sector (2 digit) 3             Subsector (3 digit) 4             Industry Group (4 digit) 5             Industry (5 digit) 6             US Industry (6 digit) 9             Not Specified D            Domain 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 88 -_ 

period 

Tables Referenced: 

bed, buildingpermits, ces, commute, cpi, cpiplus, demographics, income, iowage, industry, laborforce, licensehistory, periods, periodtypes, population, programcompleters, salesrevenue, supply, taxrevenues, transferpayments, uiclaims 

Values: 

- 00 Annual 01 First Quarter 02 Second Quarter 03 Third Quarter 04 Fourth Quarter 01 January 02 February 03 March 04 April 05 May 06 June 07 July 08 August 09 September 10 October 11 November 12 December 00 Decennial 00 1-year ACS 00 5-year ACS 

periodtype Tables Referenced: 

bed, buildingpermits, ces, 

commute, cpi, cpiplus, demographics, income, iowage, industry, laborforce, licensehistory, periods, periodtypes, population, programcompleters, salesrevenue, supply, taxrevenues, transferpayments, uiclaims 

Values: 

- 01           Annual 02           Quarter 03           Monthly 04           Weekly 

- 05           Decennial 

- 06           Bimonthly 07           Semiannually (twice a year) 08           Biennially (every two years) 

- 10           ACS 1-year estimates 

- 36           ACS 3-year estimates 50-70      State-Defined Period Types (EXCEPT 60) 

- 60           ACS 5-year estimates 99           Not Applicable 

- subtotal (occsublevels) Tables Referenced: matrixxocc, occdirectory, occsublevels Values: 1 Total all occupations 2             Summary, major group 3             Summary, minor / intermediate group 4             Broad occupation 5             detailed occupation 6             Roll up 7             Collapsed 8 Outside the SOC/OES structure (invented code) 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 89 -_ 

subtotal (indsublevels) Tables Referenced: matrixxind, inddirectory, indsublevels 

Values: 

1 Total all industries 2 Industry division 3 Major industry group 4 Industry 5 Rollup 6 Outside the SIC structure (invented code) A Total all industries B Domain C Super-Sector D Industry sector (2-digit) E Industry subsector (3-digit) F Industry group (4-digit) G Industry (5-digit) H U.S. Industry (6-digit) I Roll up J Outside the NAICS structure (invented code) 

suppress 

Tables Referenced: bed, projectionsmatrix, industry 

Values: 

0             Data is not confidential 

1             Data is confidential 

tabletype 

Tables Referenced: tablelist 

Values: 

A            Administrative table D            Data table L             Lookup table X            Crosswalk table 

taxtype Tables Referenced: taxrevenues, taxtype 

Values: 

- 01           Sales tax 

- 02           Real property tax 

- 03           Personal property tax 04           Mineral tax 05           Use tax 06           Excise tax 07           Income tax 

- 08           Ad Valorem 

- 09           Local option sales taxes 

- 10           State property taxes 

- 11           State general sales and gross receipts sales 

- 12           State alcoholic beverages sales tax 

unittype 

Tables Referenced: buildingpermits, unittype Values: 

- 00 Total all types construction permits 01 Total new construction 

- 02 Total remodels/alterations 

- 03 Single family residential new construction 

- 04 Single family residential remodels/alterations 

- 05 Multi family residential new construction 

- 06 Multi family residential remodel/alterations 

- 07 Residential, not specified new construction 

- 08 Residential, not specified remodels/alterations 

- 09 Commercial new construction 10 Commercial remodels/alterations 99 Unspecified 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 90 -_ 

wagesource Tables Referenced: iowage, wagesources Values: 

- 1             BLS Area Wage Survey 2             State Wage Survey 3             BLS Occupational Employment Statistics Survey 

- 4             BLS/Census Current Population Survey 5             Davis Bacon Wage Survey 6-9          State-Defined Wage Types 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 91 -_ 

## **Appendix A** 

## Workforce Information Database, Version 3.0 – Recommended Load Order 

|**Order**|**Table Name**|**Type**|**Order**|**Table Name**|**Type**|
|---|---|---|---|---|---|
|1|StateFips|L|43|IndDirectories|L|
|2|AreaTypes|L|44|TransferPaymentTypes|L|
|3|AreaTypeVersions|L|45|SOCCodes|L|
|4|GeoPrecisionCodes|L|46|StateProgramCode|L|
|5|Geographies|L|47|OccupationCodes|L|
|6|SubGeographies|L|48|IndustryCodes|L|
|7|CodeFlags|L|49|TaxTypes|L|
|8|PeriodYears|L|50|AgeGroupTypes|L|
|9|PeriodTypes|L|51|AgeGroups|L|
|10|Periods|L|52|AnnualSalesCodes|L|
|11|OccCodeTypes|L|53|AnnualSalesRanges|L|
|12|CareerClusters|L|54|Benchmark|L|
|13|CareerPaths|L|55|CPITypes|L|
|14|BLSEducation|L|56|CPISources|L|
|15|Experience|L|57|CreditCodes|L|
|16|BLSTrainingCodes|L|58|EmpSizeFlag|L|
|17|CPIItems|L|59|EmpSizeRange|L|
|18|OccSubLevels|L|60|GrowthCodes|L|
|19|OccDirectories|L|61|IncomeSources|L|
|20|IndCodeTypes|L|62|IncomeTypes|L|
|21|WageRateTypes|L|63|LayTitles|L|
|22|SalesTypes|L|64|LicenseNumberTypes|L|
|23|WageSources|L|65|LocationStatuses|L|
|24|IndSubLevels|L|66|PrivateGovt|L|
|25|Ownerships|L|67|BEDTypes|L|
|26|PopulationSources|L|68|ClassTime|L|
|27|UnitTypes|L|69|ContactProTitles|L|
|28|CESCodes|L|70|ContactTitles|L|
|29|CIPCodes|L|71|SpecialIDs|L|
|30|CompleterTypes|L|72|StockExchange|L|
|31|Genders|L|73|IndOccSpecialIDs|L|
|32|InstitutionOwnerships|L|74|BED|D|
|33|InstitutionTypes|L|75|BuildingPermits|D|
|34|LengthTypes|L|76|CES|D|
|35|NAICSDomains|L|77|Commute|D|
|36|NAICSSuperSectors|L|78|CPI|D|
|37|NAICSSectors|L|79|CPIPlus|D|
|38|NAICSLevels|L|80|Demographics|D|
|39|NAICSCodes|L|81|EmpDBInf|L|
|40|EthnicityCodes|L|82|EmpDB|D|
|41|RaceCodes|L|83|Income|D|
|42|ONETCodes|L|84|JOLTSTypes|L|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 92 -_ 

|**Order**|**Table Name**|**Type**|**Order**|**Table Name**|**Type**|
|---|---|---|---|---|---|
||||103|LicenseHistory|D|
|85|IndustrySums|A|104|IOWage|D|
|86|JOLTS|D|105|TransferPayments|D|
|87|Industry|D|106|Population|D|
|88|ProjectionsMatrix|D|107|Schools|D|
|89|LaborForce|D|108|ProgramCompleters|D|
|90|LicenseTypes|L|109|Programs|D|
|91|LicenseExams|L|110|SalesRevenue|D|
|92|LicenseEducation|L|111|Supply|D|
|93|LicenseContinuingEdu|L|112|TaxRevenues|D|
|94|LicenseCertifications|L|113|UIClaims|D|
|95|LicenseExperience|L|114|OccupationXOccupation|X|
|96|LicenseCriminal|L|115|IndustryXIndustry|X|
|97|LicensePhysicalReqs|L|116|LayTitleXOcc|X|
|98|LicenseActiveStatuses|L|117|LicenseXOcc|X|
|99|LicenseVeteran|L|118|MatrixXInd|X|
|100|LicenseAuthorities|D|119|MatrixXOcc|X|
|101|License|D|120|TableList|A|
|102|LicenseXLicense|X|121|TableSource|A|



Notes for Suggested Load Order 

Table Types: A=Admin; D=Data; L=Look-up; X=Crosswalk 

OCCCODES is normally loaded by triggers. If triggers are not used, content for this table should be appended from the related lookup tables: CENSCODE, CIDSCODE, CIPCODE, CLUSCODE, DOTCODE, OCCDIR, OESCODE, ONETCODE, SOCCODE and STPROGCD. 

INDCODES is normally loaded by triggers. If triggers are not used, content for this table should be appended from the related lookup tables: CENIND, SICCODE and NAICCODE. 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 93 -_ 

## **Appendix B** 

Workforce Information Database, Version 3.0 – Core Tables List 

## **Data Tables** 

**Data Tables** LicenseContinuingEdu CES LicenseActiveStatuses EmpDB LicenseCertifications ProjectionsMatrix LicenseCriminal IOWage LicenseEducation Industry LicenseExams LaborForce LicenseExperience License LicensePhysicalReqs LicenseAuthorities LicenseTypes **Look-up Tables** LicenseVeteran AnnualSalesCodes LocationStatuses AnnualSalesRanges OccDirectories AreaTypes OccSubLevels AreaTypeVersions OccCodeTypes Benchmark Ownership CESCodes Periods ContactProTitles PeriodTypes ContactTitles PeriodYears CreditCodes PrivateGovt EmpDBInf WageRateType EmpSizeFlag StFips EmpSizeRange WageSources Geographies **Crosswalk Tables** GeoPrecisionCodes MatrixXInd GrowthCodes MatrixXOcc IndCodeTypes IndDirectories IndSubLevels IndustryCodes 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 94 -_ 

## **Appendix C** 

## Workforce Information Database, Version 3.0 – Changes from Version 2.8 

Basic Changes to the Geography and Time Period fields: The areatypeversion field has been added, to indicate which version of the area type is being used. This is especially for Metropolitan Statistical Area and any other area type that is changed regularly. The areatypeversion field has also been added to data tables where applicable. 

Table periods: the periodyear field has been taken out of periods and placed in the new table periodyears.  This eliminates duplicate period types and periods in the periods table. This also makes updating for the next year’s data as easy as adding that year to the periodyears table. The field structure of data tables is unchanged, however, the periodyear foreign key reference now points to the periodyears table. 

Name Changes: In the earliest versions of ALMIS/WID, field and tables names were restricted in length by the software of the time. Allowable name lengths are now much longer, allowing readable names to be used. All table and field names have been expanded from the abbreviations used in versions 2.x of the WID.  The goal is to make such names more meaningful to users. 

Changes to Industry and Occupation Lookup tables: In the WID structure there are two places for each industry and occupation to be stored. The IndustryCodes and OccupationCodes tables exist to power foreign keys and allow a single table to reference multiple codes.  However, the descriptive content that goes along with a taxonomy is not always consistent, so those tables would either become bloated or lose a lot of information in order to store all taxonomies.  Further, sometimes it’s helpful to have a “complete” taxonomy – there are many exceptions made for things like OEWSspecific codes and when states are trying to make a tool that connects data across data sources having a clean taxonomy to relate back to can be necessary. The taxonomy tables -  SOCCodes, NAICSCodes, CESCodes, CIPCodes, ONETCodes, etc –have been standardized for WID 3.0.  The IndustryCodes code field has been expanded to char(10) to allow the inclusion of CES codes. The lengths of the Title fields have also been expanded and the order of the fields made consistent. 

Tables added in v3.0: 

|Tables added inv3.0:||
|---|---|
|**TableName**|**Description**|
|AgeGroupTypes|Thesourceofthe agegroup listed in agegroups.|
|CodeFlags|This is a reference for what used to be the OESFlag field in SOCCodes.  The<br>Flag field is now present in both SOCCodes and NAICSCodes and states can<br>define more exceptions than just for OEWS.|
|JOLTS|TheJobOpeningsand Labor Turnover Surveyprogram provides<br>national estimates ofratesandlevels forjob openings, hires,and<br>totalseparations. Total separationsarefurtherbroken outinto quits,<br>layoffsanddischarges,and otherseparations.|
|JOLTSTypes|TypesoftheJOLTSnumbers|
|LicenseXLicense|Table of re-licensing|
|PeriodYears|Alist of valid yearsfordata.|
|RaceCodes|Table ofraces|
|EthnicityCodes|Table ofethnicities|
|SpecialIDs|This is a table forgroupingconcepts for state analysis|
|TransferPayments|Transferpayments|
|TransferPaymentType|Transferpayment types|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 95 -_ 

## **Appendix D** 

Workforce Information Database, Version 3.0 – Previous Table Names 

||**WID 3.0 Name**<br>**WID 2.8 Name**<br>**Associated Program**||
|---|---|---|
||AgeGroups<br>agegroup<br>UIClaims<br>AgeGroupTypes<br>UIClaims<br>AnnualSalesCodes<br>annslflg<br>EmpDB<br>AnnualSalesRanges<br>annslrng<br>EmpDB<br>AreaTypes<br>areatype<br>Structural<br>AreaTypeVersions<br>Structural<br>BED<br>bed<br>BED<br>BEDTypes<br>bedtypes<br>BED<br>Benchmark<br>benmark<br>LAUS,CES<br>BLSEducation<br>education<br>OEWS,Structural<br>BLSTrainingCodes<br>training<br>OEWS,Structural<br>BuildingPermits<br>blding<br>State Analysis<br>CareerClusters<br>careerclust<br>Education<br>CareerPaths<br>careerpaths<br>Education<br>CES<br>ces<br>CES<br>CESCodes<br>cescode<br>CES<br>CIPCodes<br>cipcode<br>Education<br>ClassTime<br>classtime<br>Education<br>CodeFlags<br>Structural<br>Commute<br>commute<br>Census<br>CompleterTypes<br>compltyp<br>Education<br>ContactProTitles<br>contactpro<br>EmpDB<br>ContactTitles<br>contacttitle<br>EmpDB<br>CPI<br>cpi<br>CPI<br>CPIItems<br>CPI<br>CPIPlus<br>cpiplus<br>CPI<br>CPISources<br>cpisource<br>CPI<br>CPITypes<br>cpitype<br>CPI<br>CreditCodes<br>creditcd<br>EmpDB<br>Demographics<br>demographics<br>Census<br>EmpDB<br>empdb<br>EmpDB<br>EmpDBInf<br>empdbinf<br>EmpDB<br>EmpSizeFlag<br>empszflg<br>EmpDB<br>EmpSizeRange<br>empszrng<br>EmpDB<br>EthnicityCodes<br>Structural<br>Experience<br>experience<br>OEWS,Structural<br>Genders<br>gender<br>UIClaims<br>Geographies<br>geog<br>Structural<br>GeoPrecisionCodes<br>geocode<br>Structural<br>GrowthCodes<br>growcode<br>Projections<br>Income<br>income<br>Census/BEA||



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 96 -_ 

|IncomeSources|incsourc|Census/BEA|
|---|---|---|
|IncomeTypes|incomtyp|Census/BEA|
|IndCodeTypes|indtypes|Structural|
|IndDirectories|inddir|Projections|
|IndOccSpecialIDs|iospecialid|State Analysis|
|IndSubLevels|indsub|Projections|
|Industry|industry|QCEW|
|IndustryCodes|indcodes|Structural|
|IndustrySums|indsum|State Analysis|
|IndustryXIndustry|indxind|Structural|
|InstitutionOwnerships|instown|EmpDB|
|InstitutionTypes|insttype|EmpDB|
|IOWage|iowage|OEWS|
|JOLTS||JOLTS|
|JOLTSTypes||JOLTS|
|LaborForce|labforce|LAUS|
|LayTitles|laytitle|State Analysis|
|LayTitleXOcc|laytxocc|State Analysis|
|LengthTypes|lentype|Education|
|License|license|License|
|LicenseActiveStatuses|licenseactivestatus|License|
|LicenseAuthorities|licauth|License|
|LicenseCertifications|licensecertification|License|
|LicenseContinuingEdu|liccontinuingedu|License|
|LicenseCriminal|licensecriminal|License|
|LicenseEducation|licenseeducation|License|
|LicenseExams|licenseexams|License|
|LicenseExperience|licenseexperience|License|
|LicenseHistory|lichist|License|
|LicenseNumberTypes|licnumty|License|
|LicensePhysicalReqs|licensephysicalreqs|License|
|LicenseTypes|licensetypes|License|
|LicenseVeteran|licenseveteran|License|
|LicenseXLicense||License|
|LicenseXOcc|licxocc|License|
|LocationStatuses|locstat|EmpDB|
|MatrixXInd|matxind|Projections|
|MatrixXOcc|matxocc|Projections|
|NAICSCodes|naiccode|QCEW,Structural|
|NAICSDomains|naicdom|QCEW,Structural|
|NAICSLevels|naicslvl|QCEW,Structural|
|NAICSSectors|naicsect|QCEW,Structural|
|NAICSSuperSectors|naicsupr|QCEW,Structural|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 97 -_ 

|OccCodeTypes|occtypes|Structural|
|---|---|---|
|OccDirectories|occdir|Projections|
|OccSubLevels|occsub|Projections|
|OccupationCodes|occcodes|Structural|
|OccupationXOccupation|occxocc|Structural|
|ONETCodes|onetcode|License|
|Ownerships|ownershp|QCEW|
|Periods|period|Structural|
|PeriodTypes|periodty|Structural|
|PeriodYears||Structural|
|Population|populatn|Census|
|PopulationSources|popsource|Census|
|PrivateGovt|prvgovst|EmpDB|
|ProgramCompleters|progcomp|Education|
|Programs|programs|Education|
|ProjectionsMatrix|iomatrix|Projections|
|RaceCodes|raceethn|UIClaims|
|SalesRevenue|sales|EmpDB|
|SalesTypes|salestyp|EmpDB|
|Schools|schools|Education|
|SOCCodes|soccode|OEWS,Structural|
|SpecialIDs||State Analysis|
|StateFips|stfipstb|Structural|
|StateProgramCode|stprogcd|Education|
|StockExchange|stockexch|EmpDB|
|SubGeographies|subgeog|Structural|
|Supply|supply|Education|
|TableList|tabllist|Structural|
|TableSource|tablsrce|Structural|
|TaxRevenues|tax|State Analysis|
|TaxTypes|taxtype|State Analysis|
|TransferPayments||State Analysis|
|TransferPaymentTypes||State Analysis|
|UIClaims|uiclaims|UIClaims|
|UnitTypes|unittype|State Analysis|
|WageRateTypes|ratetype|OEWS|
|WageSources|wgsource|OEWS|



_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 98 -_ 

Additionally, some tables were removed and have no equivalent in version 3.0. 

## **Retired tables:** 

cidscode mpc stfirms cluscode oohtrntm svc esdata payment worksite eventtyp paytype edulevel inddiv periodid explevel itemcpi qwichar benefit jvs qwidata lengthopen jvsaddit qwisup meeicode leveltyp qwitype stattype moccode schgrade mocxocc sizeclas 

_WID Database Structure – Version 3.0_ 

_December 12, 2024_ 

_- 99 -_ 

