-- Table: public.config

-- DROP TABLE public.config;

CREATE TABLE public.config
(
    name character varying(500) COLLATE pg_catalog."default" NOT NULL,
    value character varying(10000) COLLATE pg_catalog."default" NOT NULL,
    last_updated timestamp without time zone,
    CONSTRAINT config_pkey PRIMARY KEY (name)
)

TABLESPACE pg_default;

ALTER TABLE public.config
    OWNER to micah;