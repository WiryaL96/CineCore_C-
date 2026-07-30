-- ============================================================================
-- OPSIONAL: Tambah kolom ala-Laravel yang belum ada di CineCore_MySQL.sql.
-- Jalankan sekali kalau kamu MAU fitur Format film, tipe & kota bioskop jadi
-- data beneran (bukan default). Setelah ini, minta re-enable persistence di
-- AddMovieAsync/UpdateMovieAsync + query report by cinema type.
--
-- Cara pakai (Laragon/phpMyAdmin/HeidiSQL): pilih database `cinecore`, jalankan.
-- Catatan: MySQL lama tidak dukung "ADD COLUMN IF NOT EXISTS". Kalau kolomnya
-- sudah ada, baris itu akan error — abaikan saja / hapus barisnya.
-- ============================================================================

USE cinecore;

-- movies: format film + tanggal turun layar
ALTER TABLE movies ADD COLUMN format   VARCHAR(20) NOT NULL DEFAULT '2D' AFTER duration_minutes;
ALTER TABLE movies ADD COLUMN end_date DATE        NULL                  AFTER release_date;

-- cinemas: tipe (XXI/IMAX/Premiere) + kota
ALTER TABLE cinemas ADD COLUMN type VARCHAR(50)  NULL AFTER name;
ALTER TABLE cinemas ADD COLUMN city VARCHAR(100) NULL AFTER address;

-- users: penanda admin (untuk gating tombol/menu Admin)
ALTER TABLE users ADD COLUMN is_admin TINYINT(1) NOT NULL DEFAULT 0 AFTER password;

-- Contoh isi data (sesuaikan sendiri):
-- UPDATE cinemas SET type = 'XXI',      city = 'Bandung' WHERE id = 1;
-- UPDATE cinemas SET type = 'IMAX',     city = 'Bandung' WHERE id = 2;
-- UPDATE cinemas SET type = 'Premiere', city = 'Jakarta' WHERE id = 3;
-- UPDATE users   SET is_admin = 1 WHERE email = 'admin@cinecore.test';
