-- ═══════════════════════════════════════════════════════════════════════
--  CineCore — UNTUK DATABASE (Log Activities)
--  Cara pakai : copy SEMUA isi file ini → paste ke HeidiSQL / phpMyAdmin
--               (tab SQL database "cinecore") → Run / Go.
--  Cukup dijalankan SEKALI. Aman diulang (tidak bikin tabel dobel).
-- ═══════════════════════════════════════════════════════════════════════

USE cinecore;

-- ─────────────────────────────────────────────────────────────────────
-- 1) Tabel log : siapa yang LOGIN / REGISTER / LOGOUT,
--    dan siapa yang BELI TIKET (PURCHASE + kode booking).
-- ─────────────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS activity_logs (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    user_id       BIGINT UNSIGNED NULL,           -- NULL = akun sudah dihapus.
                                                  -- SENGAJA tanpa FOREIGN KEY supaya cocok dengan
                                                  -- users.id apapun (INT maupun BIGINT UNSIGNED).
    activity_type VARCHAR(20)  NOT NULL,          -- LOGIN | REGISTER | LOGOUT | PURCHASE
    description   VARCHAR(500) NOT NULL,          -- mis. "Test User bought 2 ticket(s) ..."
    reference     VARCHAR(100) NULL,              -- kode booking (khusus PURCHASE)
    created_at    DATETIME     NOT NULL DEFAULT NOW(),
    KEY IX_activity_user (user_id),
    KEY IX_activity_time (created_at)
);

-- ─────────────────────────────────────────────────────────────────────
-- 2) (OPSIONAL, sekali saja) Isi log awal dari data LAMA biar halaman
--    Log Activities tidak kosong : anggap semua user = REGISTER,
--    semua booking lama = PURCHASE.
--    Kalau tidak mau, SKIP blok ini.
-- ─────────────────────────────────────────────────────────────────────
INSERT INTO activity_logs (user_id, activity_type, description, reference, created_at)
SELECT id,
       'REGISTER',
       CONCAT(name, ' (', email, ') registered a new account.'),
       NULL,
       created_at
FROM users;

INSERT INTO activity_logs (user_id, activity_type, description, reference, created_at)
SELECT b.user_id,
       'PURCHASE',
       CONCAT('Bought ticket ', b.booking_reference, ' — Rp ', FORMAT(b.total_price, 0)),
       b.booking_reference,
       b.created_at
FROM bookings b;

-- ─────────────────────────────────────────────────────────────────────
-- 3) Cek hasil (harus muncul baris-baris log) :
-- ─────────────────────────────────────────────────────────────────────
SELECT a.created_at            AS waktu,
       u.name                  AS user,
       u.email                 AS email,
       a.activity_type         AS aktivitas,
       a.description           AS detail,
       a.reference             AS referensi
FROM activity_logs a
LEFT JOIN users u ON u.id = a.user_id
ORDER BY a.created_at DESC
LIMIT 20;
