# ----------------------------------------------------------------
#
# Copyright (C) 2001-2002, Siebel Systems, Inc., All rights reserved.
#
# File: sampledbcfg.sql
# Date: 11/18/02
# Purpose: Sample script demonstrates how to configure an oracle dB for Siebel applications
# Execution: Run the script from SQL* plus- "sqlplus", e.g.

# sqlplus /nolog
# SQL>connect /as sysdba
# SQL>@sampledbcreationoracle

#
# General flow:
# 1) fire up the instance
# 2) create the redo log files and system tablespace
# 3) create a dummy rollback segment
# 4) create the other tablespaces
# 5) bring the dummy rollback segment online
# 6) create the other rollback segments
# 7) cleanup by dropping the dummy rollback segment
# 8) restart
#
# modify the database name, parameter
# file(bdump/udump/controlfile), datafile path, logfile path,controlfile
# Edit the parameters below to reflect the default oracle installation on Windows and Unix
# ----------------------------------------------------------------------------------------

spool crdbsiebel.log;
set echo on
#define group1='d:\tmp\redodmst_01.dbf'
#define group2='d:\tmp\redodmst_02.dbf'
#define group3='d:\tmp\redodmst_03.dbf'
#define syslogfile='d:\tmp\dmstSYS_01.dbf'
#define RBS='d:\tmp\dmstRBS_01.dbf'
#define TEMP='d:\tmp\dmstTMP_01.dbf'
#define TOOLS='d:\tmp\dmstTOOL_01.dbf'
#define loader_data='d:\tmp\dmstLDR_01.dbf'
#define users_data='d:\dmstUSR_01.dbf'

#
# fire up the instance
#
connect / as sysdba;

startup nomount
startup nomount pfile=initsiebel.ora
#
# create the SYSTEM tablespace
#
create database siebel
controlfile reuse
logfile group 1 ('&group1') size 10M reuse,
group 2 ('&group2') size 10M reuse,
group 3 ('&group3') size 10M reuse
datafile '&syslogfile' size 180M reuse
archivelog
maxlogfiles 32
maxdatafiles 256
character set AL32UTF8;

# the character set for UNICODE database is "AL32UTF8", for ENU database is

# "WE8MSWIN1252"

alter tablespace SYSTEM
default storage (initial 8K
next 8K
minextents 1
pctincrease 0);
#
# create a dummy rbs
#
create rollback segment R0 storage (initial 8K next 8K) tablespace system;
alter rollback segment R0 online;

create tablespace RBS
datafile '&RBS' size 1990M reuse
default storage (initial 48K
next 80K
minextents 1
pctincrease 0);

create tablespace TEMP
datafile '&TEMP' size 1990M reuse
default storage (initial 48K
next 80K
minextents 1
pctincrease 0);

create tablespace TOOLS
datafile '&TOOLS' size 100M reuse
default storage (initial 8K
next 16K
minextents 1
pctincrease 0);

create tablespace loader_data
datafile '&loader_data' size 1980M reuse
default storage (initial 48K
next 80K
minextents 1
pctincrease 0);

create tablespace users_data
datafile '&users_data' size 100M reuse
default storage (initial 8K
next 16K
minextents 1
pctincrease 0);

create tablespace siebeldata
datafile '/datadb/siebel/donnee/siebeldata.dbf' size 2000M reuse
default storage (initial 48K
next 80K
minextents 1
pctincrease 50);

create tablespace siebelindex
datafile '/datadb/siebel/index/siebelindex.dbf' size 1000M reuse
default storage (initial 48K
next 80K
minextents 1
pctincrease 50);

#
# run the appropriate initialization script(s)
#
set stoponerror off
rem
rem catalog.sql also runs the following scripts
rem
rem cataudit.sql
rem catexp.sql
rem catldr.sql
rem

@?/rdbms/admin/catalog

rem
rem catproc.sql also runs the following scripts
rem
rem catprc.sql
rem catsnap.sql
rem catrpc.sql
rem standard.sql
rem dbmsstdx.sql
rem pipidl.sql
rem pidian.sql
rem diutil.sql
rem pistub.sql
rem dbmsutil.sql
rem dbmssnap.sql
rem dbmslock.sql
rem dbmspipe.sql
rem dbmsalrt.sql
rem dbmsotpt.sql
rem dbmsdesc.sql
rem

@?/rdbms/admin/catproc

#
# create the non-system rollback segment(s)
#

create rollback segment R01
storage (initial 80K
next 80K
optimal 5M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R01 online;

create rollback segment R02
storage (initial 80K
next 80K
optimal 5M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R02 online;

create rollback segment R03
storage (initial 48K
next 80K
optimal 6M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R03 online;

create rollback segment R04
storage (initial 48K
next 80K
optimal 6M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R04 online;

create rollback segment R05
storage (initial 48K
next 80K
optimal 6M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R05 online;

create rollback segment R06
storage (initial 48K
next 80K
optimal 6M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R06 online;

create rollback segment R07
storage (initial 48K
next 80K
optimal 6M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R07 online;

create rollback segment R08
storage (initial 48K
next 80K
optimal 6M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R08 online;

create rollback segment R09
storage (initial 48K
next 80K
optimal 6M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R09 online;

create rollback segment R10
storage (initial 48K
next 80K
optimal 6M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R10 online;

create rollback segment R11
storage (initial 48K
next 80K
optimal 6M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R11 online;

create rollback segment R12
storage (initial 48K
next 80K
optimal 6M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment R12 online;

create rollback segment large
storage (initial 10M
next 10M
optimal 200M
minextents 5
maxextents 2000)
tablespace RBS;
alter rollback segment large online;

#
# drop the dummy rbs
#
alter rollback segment R0 offline;
drop rollback segment R0;
#
# create the ops$ user and run catdbsyn as that user
#
create user ops$oracle identified externally;
grant dba to ops$oracle with admin option;
alter user ops$oracle temporary tablespace temp;
#
# connect as system and run pupbld
#
connect system/manager;
@?/sqlplus/admin/pupbld.sql
@?/rdbms/admin/catdbsyn.sql;
#
# perform the final cleanup sequence
#
alter user system default tablespace tools temporary tablespace temp;
alter user ops$oracle default tablespace tools temporary tablespace temp;
disconnect;
exit;