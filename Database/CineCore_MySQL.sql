-- ═══════════════════════════════════════════════════════════════════════
--  CineCore  –  MySQL 8.0+ Schema (matches runtime C# code exactly)
--  Connection: Server=127.0.0.1;Port=3306;Database=cinecore;Uid=root;Pwd=;SslMode=None;
-- ═══════════════════════════════════════════════════════════════════════

CREATE DATABASE IF NOT EXISTS cinecore
  CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE cinecore;

-- ─────────────────────────────────────────────────────────────────────
-- 1. USERS (kolom: name, email, password, created_at, updated_at)
-- ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS users (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    name        VARCHAR(120)  NOT NULL,
    email       VARCHAR(200)  NOT NULL UNIQUE,
    password    VARCHAR(256)  NOT NULL,
    created_at  DATETIME      NOT NULL DEFAULT NOW(),
    updated_at  DATETIME      NOT NULL DEFAULT NOW()
);

-- ─────────────────────────────────────────────────────────────────────
-- 2. MOVIES (kolom: title, genre, duration_minutes, description, 
--    poster_path, trailer_url, rating, is_showing, release_date)
-- ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS movies (
    id               INT AUTO_INCREMENT PRIMARY KEY,
    title            VARCHAR(200) NOT NULL,
    genre            VARCHAR(80)  NOT NULL,
    duration_minutes INT          NOT NULL,
    description      TEXT         NULL,
    poster_path      VARCHAR(500) NULL,
    trailer_url      VARCHAR(500) NULL,
    rating           DOUBLE       NOT NULL DEFAULT 0,
    is_showing       TINYINT(1)   NOT NULL DEFAULT 1,
    release_date     DATE         NULL,
    created_at       DATETIME     NOT NULL DEFAULT NOW(),
    updated_at       DATETIME     NOT NULL DEFAULT NOW()
);

-- ─────────────────────────────────────────────────────────────────────
-- 3. CINEMAS (kolom: name, address, total_rows, total_columns)
-- ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS cinemas (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    name          VARCHAR(80)  NOT NULL,
    address       VARCHAR(200) NULL,
    total_rows    INT NOT NULL DEFAULT 5,
    total_columns INT NOT NULL DEFAULT 8,
    created_at    DATETIME     NOT NULL DEFAULT NOW(),
    updated_at    DATETIME     NOT NULL DEFAULT NOW()
);

-- ─────────────────────────────────────────────────────────────────────
-- 4. SHOWTIMES (kolom: start_time sebagai DATETIME, price)
-- ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS showtimes (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    movie_id    INT            NOT NULL,
    cinema_id   INT            NOT NULL,
    start_time  DATETIME       NOT NULL,
    price       DECIMAL(12,2)  NOT NULL,
    created_at  DATETIME       NOT NULL DEFAULT NOW(),
    updated_at  DATETIME       NOT NULL DEFAULT NOW(),
    FOREIGN KEY (movie_id)  REFERENCES movies(id)  ON DELETE CASCADE,
    FOREIGN KEY (cinema_id) REFERENCES cinemas(id) ON DELETE CASCADE,
    UNIQUE KEY UQ_showtime (movie_id, cinema_id, start_time)
);

-- ─────────────────────────────────────────────────────────────────────
-- 5. BOOKINGS (kolom: total_price, payment_method, booking_reference)
-- ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS bookings (
    id                INT AUTO_INCREMENT PRIMARY KEY,
    user_id           INT            NOT NULL,
    showtime_id       INT            NOT NULL,
    total_price       DECIMAL(12,2)  NOT NULL,
    payment_method    VARCHAR(50)    NOT NULL,
    status            VARCHAR(20)    NOT NULL DEFAULT 'paid',
    booking_reference VARCHAR(30)    NOT NULL UNIQUE,
    created_at        DATETIME       NOT NULL DEFAULT NOW(),
    updated_at        DATETIME       NOT NULL DEFAULT NOW(),
    FOREIGN KEY (user_id)     REFERENCES users(id),
    FOREIGN KEY (showtime_id) REFERENCES showtimes(id)
);
CREATE INDEX IX_bookings_user     ON bookings(user_id);
CREATE INDEX IX_bookings_showtime ON bookings(showtime_id);

-- ─────────────────────────────────────────────────────────────────────
-- 6. BOOKING SEATS (kolom: seat_number, showtime_id)
-- ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS booking_seats (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    booking_id  INT         NOT NULL,
    showtime_id INT         NOT NULL,
    seat_number VARCHAR(5)  NOT NULL,
    created_at  DATETIME    NOT NULL DEFAULT NOW(),
    updated_at  DATETIME    NOT NULL DEFAULT NOW(),
    FOREIGN KEY (booking_id) REFERENCES bookings(id) ON DELETE CASCADE,
    UNIQUE KEY UQ_seat_per_showtime (showtime_id, seat_number)
);

-- ═══════════════════════════════════════════════════════════════════════
-- SEED DATA
-- ═══════════════════════════════════════════════════════════════════════

-- Demo user (password: demo1234 — BCrypt hash)
INSERT INTO users (name, email, password) VALUES
('Demo User', 'demo@cinecore.id',
 '$2a$11$K4ePxQpZt1Fz1x0xVbK7rOWg5YqVwR2vJzH0HdW5qXq3h4Kz1F2Hy');

-- Cinemas
INSERT INTO cinemas (name, address, total_rows, total_columns) VALUES
('XXI',      'Bandung Indah Plaza Lt. 3',  5, 8),
('Premiere', 'Paris Van Java Mall Lt. 4',  6, 10),
('IMAX',     'Trans Studio Mall Lt. 5',    5, 8);

-- Movies
INSERT INTO movies (title, genre, duration_minutes, description, poster_path, trailer_url, rating, is_showing, release_date) VALUES
('Inception',
 'Sci-Fi / Thriller', 148,
 'A thief who steals corporate secrets through the use of dream-sharing technology is given the inverse task of planting an idea into the mind of a C.E.O.',
 'https://image.tmdb.org/t/p/w500/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg',
 'https://www.youtube.com/watch?v=YoHD9XEInc0', 8.8, 1, '2010-07-16'),

('Dune: Part Two',
 'Sci-Fi / Adventure', 166,
 'Paul Atreides unites with Chani and the Fremen while on a path of revenge against the conspirators who destroyed his family.',
 'https://image.tmdb.org/t/p/w500/8b8R8l88Qje9dn9OE8PY05Nxl1X.jpg',
 'https://www.youtube.com/watch?v=U2Qp5pL3ovA', 8.5, 1, '2024-03-01'),

('Oppenheimer',
 'Biography / Drama', 180,
 'The story of American scientist J. Robert Oppenheimer and his role in the development of the atomic bomb during World War II.',
 'https://image.tmdb.org/t/p/w500/8Gxv8gSFCU0XGDykEGv7zR1n2ua.jpg',
 'https://www.youtube.com/watch?v=uYPbbksJxIg', 8.6, 1, '2023-07-21'),

('The Dark Knight',
 'Action / Crime', 152,
 'When the menace known as the Joker wreaks havoc on Gotham, Batman must accept one of the greatest tests of his ability to fight injustice.',
 'https://image.tmdb.org/t/p/w500/qJ2tW6WMUDux911r6m7haRef0WH.jpg',
 'https://www.youtube.com/watch?v=EXeTwQWrcwY', 9.0, 0, '2025-12-01'),

('Spider-Man: No Way Home',
 'Action / Fantasy', 148,
 'With Spider-Man identity now revealed, Peter asks Doctor Strange for help. Dangerous foes from other worlds start to appear.',
 'https://image.tmdb.org/t/p/w500/1g0dhYtq4irTY1GPXvft6k4YLjm.jpg',
 'https://www.youtube.com/watch?v=JfVOs4VSpmA', 8.3, 1, '2021-12-17'),

('Interstellar',
 'Sci-Fi / Drama', 169,
 'A team of explorers travel through a wormhole in space in an attempt to ensure humanity survival.',
 'https://image.tmdb.org/t/p/w500/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg',
 'https://www.youtube.com/watch?v=zSWdZVtXT7E', 8.7, 1, '2014-11-07');

-- Showtimes: generate for today + 6 days (7 hari, samain dengan date picker di app),
-- all movies x all cinemas, 3 shows/day. Hanya film yang tayang (is_showing = 1).
INSERT INTO showtimes (movie_id, cinema_id, start_time, price)
SELECT m.id, c.id, DATE_ADD(CURDATE(), INTERVAL d.offset DAY) + INTERVAL t.hour HOUR, t.price
FROM movies m
CROSS JOIN cinemas c
CROSS JOIN (SELECT 0 AS offset UNION SELECT 1 UNION SELECT 2 UNION SELECT 3
            UNION SELECT 4 UNION SELECT 5 UNION SELECT 6) d
CROSS JOIN (
    SELECT 10 AS hour, 50000 AS price UNION ALL
    SELECT 14, 65000 UNION ALL
    SELECT 19, 75000
) t
WHERE m.is_showing = 1
ON DUPLICATE KEY UPDATE price = VALUES(price);
