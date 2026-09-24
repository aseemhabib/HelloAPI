select current_database()
select version()

CREATE TABLE users
(
    id uuid PRIMARY KEY,
    first_name varchar(100) NOT NULL,
    last_name varchar(100) NOT NULL,
    email varchar(255) NOT NULL UNIQUE,
    created_utc timestamptz NOT NULL
);

select * from users;

insert into users (id, first_name, last_name, email, created_utc) values (gen_random_uuid(), 'Aseem', 'Habib', 'aseemhabib@gmail.com', now());

--drop table users;