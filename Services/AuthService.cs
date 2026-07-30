using System;

namespace CineCore.Services
{
    public static class AuthService
    {
        // ── DAFTAR EMAIL ADMIN ──
        // Akun dengan email di daftar ini yang boleh lihat & buka Admin Panel.
        // (DB belum punya kolom is_admin, jadi peran admin ditentukan di sini.)
        // Tambah / ganti email admin-mu di bawah:
        private static readonly HashSet<string> AdminEmails = new(StringComparer.OrdinalIgnoreCase)
        {
            "test@example.com",
            // tambah email admin lain di sini kalau perlu:
            // "admin2@contoh.com",
        };

        public static bool IsAdminEmail(string? email) =>
            !string.IsNullOrWhiteSpace(email) && AdminEmails.Contains(email.Trim());


        // Fungsi ini dipanggil saat REGISTER untuk membuat Hash
        public static string HashPassword(string password)
        {
            // Menghasilkan hash Bcrypt standar yang kompatibel 100% dengan Laravel
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Fungsi ini dipanggil saat LOGIN untuk mengecek kebenaran Password
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                // Membandingkan password inputan dengan hash dari Database
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch
            {
                return false; // Kalau string hash-nya aneh, anggap salah
            }
        }
    }
}