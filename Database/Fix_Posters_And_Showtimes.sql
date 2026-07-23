-- ═══════════════════════════════════════════════════════════════════════
--  FIX: poster semua film + jadwal (showtimes) 7 hari ke depan
--  Jalankan di database cinecore (phpMyAdmin / HeidiSQL / MySQL CLI)
--  Aman dijalankan berulang (idempotent).
-- ═══════════════════════════════════════════════════════════════════════
USE cinecore;

-- ── 1. Pastikan poster_path terisi URL lengkap (bukan path relatif Laravel) ──
UPDATE movies SET poster_path = 'https://image.tmdb.org/t/p/w500/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg', rating = 8.8, is_showing = 1 WHERE title = 'Inception';
UPDATE movies SET poster_path = 'https://image.tmdb.org/t/p/w500/8b8R8l88Qje9dn9OE8PY05Nxl1X.jpg', rating = 8.5, is_showing = 1 WHERE title = 'Dune: Part Two';
UPDATE movies SET poster_path = 'https://image.tmdb.org/t/p/w500/8Gxv8gSFCU0XGDykEGv7zR1n2ua.jpg', rating = 8.6, is_showing = 1 WHERE title = 'Oppenheimer';
UPDATE movies SET poster_path = 'https://image.tmdb.org/t/p/w500/qJ2tW6WMUDux911r6m7haRef0WH.jpg', rating = 9.0, is_showing = 0 WHERE title = 'The Dark Knight';
UPDATE movies SET poster_path = 'https://image.tmdb.org/t/p/w500/1g0dhYtq4irTY1GPXvft6k4YLjm.jpg', rating = 8.3, is_showing = 1 WHERE title = 'Spider-Man: No Way Home';
UPDATE movies SET poster_path = 'https://image.tmdb.org/t/p/w500/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg', rating = 8.7, is_showing = 1 WHERE title = 'Interstellar';

-- ── 2. Generate jadwal 7 hari ke depan (hari ini + 6), 3 sesi/hari ──
--     Untuk semua film yang tayang (is_showing = 1) × semua bioskop.
--     WHERE NOT EXISTS bikin ini aman diulang tanpa jadwal dobel.
INSERT INTO showtimes (movie_id, cinema_id, start_time, price)
SELECT x.movie_id, x.cinema_id, x.start_time, x.price
FROM (
    SELECT m.id AS movie_id, c.id AS cinema_id,
           DATE_ADD(CURDATE(), INTERVAL d.offset DAY) + INTERVAL t.hr HOUR AS start_time,
           t.price AS price
    FROM movies m
    CROSS JOIN cinemas c
    CROSS JOIN (SELECT 0 AS offset UNION SELECT 1 UNION SELECT 2 UNION SELECT 3
                UNION SELECT 4 UNION SELECT 5 UNION SELECT 6) d
    CROSS JOIN (SELECT 10 AS hr, 50000 AS price UNION ALL
                SELECT 14, 65000 UNION ALL
                SELECT 19, 75000) t
    WHERE m.is_showing = 1
) x
WHERE NOT EXISTS (
    SELECT 1 FROM showtimes s
    WHERE s.movie_id = x.movie_id
      AND s.cinema_id = x.cinema_id
      AND s.start_time = x.start_time
);

-- ── Cek hasil ──
SELECT COUNT(*) AS total_jadwal_mulai_hari_ini
FROM showtimes WHERE start_time >= CURDATE();
