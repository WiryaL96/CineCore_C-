# 🎬 CineCore — Aplikasi Pesan Tiket Bioskop (Desktop)

CineCore adalah aplikasi **desktop** buat pesan tiket bioskop, dibuat pakai **C# + WPF (.NET 8)**.
Ada dua sisi: **user biasa** (lihat film, pilih kursi, bayar, dapat e-ticket) dan **admin**
(kelola film + lihat laporan penjualan + export PDF).

Tampilannya dark mode, dan datanya disimpan di **database MySQL**.

---

## ✨ Fitur

**Untuk user:**
- Daftar akun & login (password di-hash pakai BCrypt)
- Dashboard: film **Now Showing** & **Coming Soon**
- Detail film: pilih **tanggal → bioskop → jam tayang**, tonton trailer
- Pilih kursi (kursi yang sudah dibeli otomatis terkunci)
- Bayar → dapat **E-Ticket** lengkap dengan **QR code**
- **Download tiket** jadi gambar PNG/JPG
- **My Tickets**: riwayat semua booking

**Untuk admin** (menu ⚙ Admin di kanan atas dashboard):
- **Movie Catalog**: tambah / edit / hapus film + auto-generate jadwal tayang
- **Sales & Report**: ringkasan pendapatan, tiket terjual, film terlaris, transaksi terbaru
- **Export PDF** laporan penjualan (pakai QuestPDF)

---

## 🧱 Teknologi

| Bagian | Dipakai |
|--------|---------|
| Bahasa & Framework | C#, .NET 8, WPF |
| Pola | MVVM (tanpa library MVVM tambahan) |
| Database | MySQL (via `MySqlConnector`) |
| Password | `BCrypt.Net-Next` |
| PDF report | `QuestPDF` |

---

## 🚀 Cara Menjalankan (langkah demi langkah)

### 1. Yang harus terpasang dulu
- **Visual Studio 2022** (centang workload *.NET desktop development*)
- **.NET 8 SDK**
- **MySQL** yang jalan — paling gampang pakai **Laragon** atau **XAMPP**

### 2. Siapkan database
1. Nyalakan MySQL (Laragon → *Start All*).
2. Buka phpMyAdmin / HeidiSQL / MySQL CLI.
3. Jalankan file **`Database/CineCore_MySQL.sql`** → ini otomatis bikin database `cinecore`,
   semua tabel, dan data contoh (film, bioskop, user demo).
4. (Opsional) Jalankan **`Database/Fix_Posters_And_Showtimes.sql`** → pastikan poster & jadwal
   7 hari ke depan terisi.
5. (Opsional) Jalankan **`Database/Fix_Missing_Posters.sql`** → benerin poster film yang kosong.

### 3. Cek connection string
Buka **`Services/DatabaseService.cs`** baris paling atas. Defaultnya sudah pas untuk Laragon
(user `root`, password kosong):

```csharp
private const string ConnectionString =
    "Server=127.0.0.1;Port=3306;Database=cinecore;Uid=root;Pwd=;SslMode=None;";
```

Kalau MySQL-mu pakai password, ganti bagian `Pwd=` sesuai punyamu.

### 4. Jalankan
- Buka `CineCore.sln` di Visual Studio → tekan **F5**.
- (atau lewat terminal: `dotnet run`)

> Catatan: poster film diambil dari internet (TMDB), jadi pas dijalankan sebaiknya **online**.

---

## 👤 Akun untuk login

| Tipe | Email | Keterangan |
|------|-------|-----------|
| Admin | `test@example.com` | Bisa lihat menu **⚙ Admin**. Daftar admin diatur di `Services/AuthService.cs` |
| User demo | `demo@cinecore.id` | Akun contoh dari data seed |

Mau jadikan email lain sebagai admin? Tinggal tambahkan di **`Services/AuthService.cs`**:

```csharp
private static readonly HashSet<string> AdminEmails = new(StringComparer.OrdinalIgnoreCase)
{
    "test@example.com",
    // "email-kamu@gmail.com",
};
```

Atau daftar akun baru sendiri lewat tombol **Register** di aplikasi.

---

## 🗂️ Struktur Folder (ringkas)

```
CineCore/
├── App.xaml / MainWindow.xaml     ← window utama + title bar custom
├── Models/                        ← Movie, User, Cinema, Showtime, Booking, dll
├── ViewModels/                    ← logika tiap halaman (MVVM)
├── Views/                         ← tampilan XAML (Login, Dashboard, MovieDetail,
│                                     SeatSelection, Payment, E-Ticket, MyTickets, Admin)
├── Services/
│   ├── DatabaseService.cs         ← semua akses database
│   ├── AuthService.cs             ← hash password + daftar admin
│   ├── NavigationService.cs       ← pindah antar halaman
│   ├── SessionService.cs          ← user yang sedang login
│   └── ReportPdfService.cs        ← bikin PDF laporan
├── Converters/                    ← converter buat binding (poster, tanggal, dll)
└── Database/                      ← file .sql (schema + data + perbaikan)
```

---

## 🧭 Alur Halaman

```
Login → Dashboard → Detail Film → Pilih Kursi → Bayar → E-Ticket
                 ↘ My Tickets
                 ↘ ⚙ Admin → Movie Catalog / Sales & Report → Export PDF
```

---

## 🐛 Kalau Ada Masalah

| Masalah | Solusi |
|---------|--------|
| Poster film nggak muncul | Butuh internet (poster dari TMDB). Atau edit film di Admin → **Choose File** pilih gambar dari komputer / paste URL gambar. Bisa juga jalanin `Fix_Missing_Posters.sql` |
| "Unknown column ..." saat buka Admin/Report | Database-mu belum punya kolom tertentu. Jalankan `Database/Fix_Add_Laravel_Columns.sql` (opsional) |
| Nggak bisa connect database | Pastikan MySQL nyala & connection string di `DatabaseService.cs` benar |
| Window nggak bisa di-resize | Sudah didukung — tarik pinggir window, atau klik tombol □ (maximize) |
| Jendela putih / kosong | Pastikan `App.xaml` StartupUri → `MainWindow.xaml` |

---

## 📄 Catatan

- Database asli lebih sederhana dari skema Laravel. Beberapa fitur (Format film, tipe & kota
  bioskop, kolom `is_admin`) butuh kolom tambahan — kalau mau mengaktifkannya, jalankan
  `Database/Fix_Add_Laravel_Columns.sql`.
- Anti double-booking: pakai transaksi `SERIALIZABLE` + kursi yang sudah dibeli langsung dikunci di UI.
