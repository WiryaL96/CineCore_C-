using System;

namespace CineCore.Models
{
    // Satu baris di tabel activity_logs (lihat UntukDatabase.sql).
    public class ActivityLog
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;  // LOGIN | REGISTER | LOGOUT | PURCHASE
        public string Description { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;     // kode booking (khusus PURCHASE)
        public DateTime CreatedAt { get; set; }
    }
}
