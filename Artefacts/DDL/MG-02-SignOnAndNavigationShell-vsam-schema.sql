-- MG-02 Sign-on and Navigation Shell: VSAM-derived schema (postgresql)
-- Source: CSD DEFINE FILE(USRSEC); DUSRSECJ.jcl IDCAMS DEFINE CLUSTER KEYS(8,0) RECORDSIZE(80,80); CSUSR01Y
CREATE TABLE usrsec (
    sec_usr_id    CHAR(8)  NOT NULL,  -- SEC-USR-ID    PIC X(08) offset 0  (KSDS key)
    sec_usr_fname CHAR(20) NOT NULL,  -- SEC-USR-FNAME PIC X(20) offset 8
    sec_usr_lname CHAR(20) NOT NULL,  -- SEC-USR-LNAME PIC X(20) offset 28
    sec_usr_pwd   CHAR(8)  NOT NULL,  -- SEC-USR-PWD   PIC X(08) offset 48
    sec_usr_type  CHAR(1)  NOT NULL,  -- SEC-USR-TYPE  PIC X(01) offset 56
    -- SEC-USR-FILLER PIC X(23) offset 57: no column
    CONSTRAINT usrsec_pk PRIMARY KEY (sec_usr_id)
);

-- Seed: DUSRSECJ.jcl STEP01 SYSUT1 (10 synthetic users)
INSERT INTO usrsec (sec_usr_id, sec_usr_fname, sec_usr_lname, sec_usr_pwd, sec_usr_type) VALUES
('ADMIN001','MARGARET','GOLD','PASSWORD','A'),
('ADMIN002','RUSSELL','RUSSELL','PASSWORD','A'),
('ADMIN003','RAYMOND','WHITMORE','PASSWORD','A'),
('ADMIN004','EMMANUEL','CASGRAIN','PASSWORD','A'),
('ADMIN005','GRANVILLE','LACHAPELLE','PASSWORD','A'),
('USER0001','LAWRENCE','THOMAS','PASSWORD','U'),
('USER0002','AJITH','KUMAR','PASSWORD','U'),
('USER0003','LAURITZ','ALME','PASSWORD','U'),
('USER0004','AVERARDO','MAZZI','PASSWORD','U'),
('USER0005','LEE','TING','PASSWORD','U');
