using System;

namespace CineCore.Services
{
    public static class AuthService
    {
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