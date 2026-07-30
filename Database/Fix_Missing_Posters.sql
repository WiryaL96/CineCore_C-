-- ═══════════════════════════════════════════════════════════════════════
--  FIX: poster film yang nggak muncul (Gone Girl, Inside Out).
--  Film ini kamu tambah lewat Admin, tapi poster_path-nya kosong/rusak.
--  URL di bawah sudah diverifikasi bisa diakses (TMDB, HTTP 200, JPEG).
--
--  Cara pakai: buka database `cinecore` (Laragon/phpMyAdmin/HeidiSQL), jalankan.
--  Aman diulang.
-- ═══════════════════════════════════════════════════════════════════════
USE cinecore;

UPDATE movies
SET poster_path = 'https://image.tmdb.org/t/p/w500/ts996lKsxvjkO2yiYG0ht4qAicO.jpg'
WHERE title = 'Gone Girl';

UPDATE movies
SET poster_path = 'https://image.tmdb.org/t/p/w500/2H1TmgdfNtsKlU9jKdeNyYL5y8T.jpg'
WHERE title = 'Inside Out';

-- Untuk film "test" buatanmu: kasih poster apa saja, contoh (opsional) —
-- ganti URL-nya sesuai selera, atau edit langsung di Admin Panel:
-- UPDATE movies SET poster_path = 'https://image.tmdb.org/t/p/w500/xxxx.jpg' WHERE title = 'test';

-- Cek hasil:
SELECT id, title, poster_path FROM movies ORDER BY id;
