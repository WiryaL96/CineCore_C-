using CineCore.Models;
using System;
using System.IO;

namespace CineCore.Services
{
    public static class SessionService
    {
        public static User? CurrentUser { get; set; }

        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CineCore", "remember.txt");

        public static void Logout()
        {
            CurrentUser = null;
        }

        public static void SaveRememberedEmail(string email)
        {
            var dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(SettingsPath, email);
        }

        public static string? LoadRememberedEmail()
        {
            if (!File.Exists(SettingsPath)) return null;
            var email = File.ReadAllText(SettingsPath).Trim();
            return string.IsNullOrEmpty(email) ? null : email;
        }

        public static void ClearRememberedEmail()
        {
            if (File.Exists(SettingsPath)) File.Delete(SettingsPath);
        }
    }
}