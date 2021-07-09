-- SEQUENCE: public.micah_user__id_seq

-- DROP SEQUENCE public.micah_user__id_seq;

CREATE SEQUENCE public.micah_user__id_seq
    INCREMENT 1
    START 1
    MINVALUE 1
    MAXVALUE 2147483647
    CACHE 1;

ALTER SEQUENCE public.micah_user__id_seq
    OWNER TO micah;

-- Table: public.micah_user

-- DROP TABLE public.micah_user;

CREATE TABLE public.micah_user
(
    _id integer NOT NULL DEFAULT nextval('micah_user__id_seq'::regclass),
    user_name character varying(100) COLLATE pg_catalog."default" NOT NULL,
    last_logged_in timestamp without time zone,
    CONSTRAINT micah_user_pkey PRIMARY KEY (_id)
)

TABLESPACE pg_default;

ALTER TABLE public.micah_user
    OWNER to micah;
