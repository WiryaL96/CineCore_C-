-- ═══════════════════════════════════════════════════════════════════════
--  FIX: menambahkan kolom `rating` yang hilang di tabel movies
--  Jalankan di database cinecore (phpMyAdmin / HeidiSQL / MySQL CLI)
-- ═══════════════════════════════════════════════════════════════════════
USE cinecore;

-- Tambah kolom rating kalau belum ada (aman dijalankan berulang)
ALTER TABLE movies
    ADD COLUMN IF NOT EXISTS rating DOUBLE NOT NULL DEFAULT 0 AFTER trailer_url;

-- Isi rating untuk film-film yang sudah ada
UPDATE movies SET rating = 8.8 WHERE title = 'Inception';
UPDATE movies SET rating = 8.5 WHERE title = 'Dune: Part Two';
UPDATE movies SET rating = 8.6 WHERE title = 'Oppenheimer';
UPDATE movies SET rating = 9.0 WHERE title = 'The Dark Knight';
UPDATE movies SET rating = 8.3 WHERE title = 'Spider-Man: No Way Home';
UPDATE movies SET rating = 8.7 WHERE title = 'Interstellar';
