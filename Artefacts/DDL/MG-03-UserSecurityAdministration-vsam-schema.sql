-- MG-03 User Security Administration: VSAM-derived schema (postgresql)
-- MG-03 shares VSAM USRSEC with MG-02 (COSGN00C reads it; COUSR00C-03C browse, write, rewrite and delete it).
-- The table is created by MG-02-SignOnAndNavigationShell-vsam-schema.sql; this script is idempotent
-- and adds no columns, so both groups stay on one table definition.
-- Source: CSD DEFINE FILE(USRSEC) ADD/BROWSE/DELETE/READ/UPDATE(YES); DUSRSECJ.jcl KEYS(8,0) RECORDSIZE(80,80); CSUSR01Y
CREATE TABLE IF NOT EXISTS usrsec (
    sec_usr_id    CHAR(8)  NOT NULL,  -- SEC-USR-ID    PIC X(08) offset 0  (KSDS key; STARTBR/READNEXT/READPREV order)
    sec_usr_fname CHAR(20) NOT NULL,  -- SEC-USR-FNAME PIC X(20) offset 8
    sec_usr_lname CHAR(20) NOT NULL,  -- SEC-USR-LNAME PIC X(20) offset 28
    sec_usr_pwd   CHAR(8)  NOT NULL,  -- SEC-USR-PWD   PIC X(08) offset 48
    sec_usr_type  CHAR(1)  NOT NULL,  -- SEC-USR-TYPE  PIC X(01) offset 56
    CONSTRAINT usrsec_pk PRIMARY KEY (sec_usr_id)
);
-- Browse order for CU00 paging (PF7/PF8) is the primary-key B-tree; no extra index is needed.
