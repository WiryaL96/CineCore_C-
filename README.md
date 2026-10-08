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
- **Log Activities**: siapa yang login/register/logout + siapa yang beli tiket apa (butuh `UntukDatabase.sql`, lihat bawah)
- **Export PDF** laporan penjualan (pakai QuestPDF)

**Window & tampilan:**
- Title bar custom: tombol minimize / maximize-restore / close, drag, double-click, klik kanan (system menu), Aero snap (`Win+Arrow`)

---

## 🧱 Teknologi (bahan presentasi)

### Ringkasan satu tabel

| Lapisan | Teknologi | Versi | Dipakai untuk |
|--------|-----------|-------|---------------|
| Bahasa | C# | 12 (.NET 8 SDK) | Seluruh kode aplikasi |
| Framework UI | WPF (`UseWPF`) | .NET 8 (`net8.0-windows`) | Tampilan desktop: XAML + code-behind |
| Arsitektur | MVVM (tanpa library tambahan) | — | `ViewModels/` + `ViewModels/Base/` (`ViewModelBase`, `RelayCommand`, `AsyncRelayCommand`) |
| Dependency injection | `ServiceLocator` (buatan sendiri) | — | Menyediakan `IDatabaseService` ke semua ViewModel |
| Database | MySQL 8 (dijalankan via Laragon) | 8.x | Semua data: user, film, bioskop, jadwal, booking, activity log |
| Driver database | `MySqlConnector` (NuGet) | 2.5.0 | Akses async C# → MySQL |
| Password hashing | `BCrypt.Net-Next` (NuGet) | 4.1.0 | Hash & verifikasi password (format hash kompatibel Laravel) |
| Export PDF | `QuestPDF` (NuGet) | 2026.7.2 | Laporan penjualan PDF + preview per halaman |
| Window custom | `System.Windows.Shell.WindowChrome` (bawaan WPF) | — | Title bar custom: minimize / maximize-restore / close, drag, double-click, Aero snap |
| Ikon | Vektor XAML (`Path`) | — | Semua ikon digambar pakai geometri — tajam di resolusi berapa pun |
| Download tiket | `RenderTargetBitmap` + `PngBitmapEncoder` (bawaan WPF) | — | Simpan E-Ticket jadi gambar PNG/JPG |
| Gambar poster | URL internet (TMDB) + upload file lokal | — | Poster film (makanya butuh online) |
| Tools | Visual Studio 2022, .NET 8 SDK, Laragon, HeidiSQL/phpMyAdmin | — | Ngoding, run, dan kelola database |

### Kalau ditanya "kenapa pakai ini?" (jawaban singkat)

- **Kenapa WPF, bukan WinForms?** Karena UI-nya dideklarasikan di XAML (pisah tampilan vs logika),
  gampang bikin dark mode + template custom (tombol, ComboBox, DatePicker, Calendar).
- **Kenapa MVVM?** Supaya `Views` (XAML) cuma binding ke `ViewModels`, dan semua akses data
  lewat `Services`. Ganti database / ganti tampilan nggak saling merusak.
- **Kenapa MySQL?** Gratis, gampang jalan di Laragon, dan skemanya mirip database Laravel
  (kolom `created_at`/`updated_at`, nama tabel jamak) — gampang disambung ke backend web kalau mau.
- **Kenapa BCrypt?** Password tidak pernah disimpan polos; yang disimpan cuma hash.
  Login = bandingkan hash, bukan bandingkan password. Formatnya sama dengan Laravel
  (`$2a$...`) jadi user bisa dipakai bareng aplikasi web.
- **Kenapa QuestPDF?** Bikin PDF pakai kode C# (fluent API), nggak perlu template Word/HTML.
  Dipakai untuk export laporan + preview per halaman sebelum diunduh.
- **Kenapa transaksi `SERIALIZABLE`?** Supaya dua orang tidak bisa membeli kursi yang sama
  di detik yang sama (anti double-booking) — dicek dulu, baru insert, semua dalam 1 transaksi.

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
4. Jalankan file **`UntukDatabase.sql`** (di root repo) → bikin tabel `activity_logs`
   buat halaman **Log Activities** di admin + isi data awal dari user/booking lama.
5. (Opsional) Jalankan **`Database/Fix_Posters_And_Showtimes.sql`** → pastikan poster & jadwal
   7 hari ke depan terisi.
6. (Opsional) Jalankan **`Database/Fix_Missing_Posters.sql`** → benerin poster film yang kosong.

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
├── UntukDatabase.sql              ← query tabel activity_logs (copy-paste sekali)
├── Models/                        ← Movie, User, Cinema, Showtime, Booking, ActivityLog, dll
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
                 ↘ ⚙ Admin → Movie Catalog / Sales & Report / Log Activities → Export PDF
```

---

## 🐛 Kalau Ada Masalah

| Masalah | Solusi |
|---------|--------|
| Poster film nggak muncul | Butuh internet (poster dari TMDB). Atau edit film di Admin → **Choose File** pilih gambar dari komputer / paste URL gambar. Bisa juga jalanin `Fix_Missing_Posters.sql` |
| "Unknown column ..." saat buka Admin/Report | Database-mu belum punya kolom tertentu. Jalankan `Database/Fix_Add_Laravel_Columns.sql` (opsional) |
| Log Activities kosong ("No activity yet") | Tabel `activity_logs` belum dibuat. Jalankan `UntukDatabase.sql`, lalu login / beli tiket sekali |
| Nggak bisa connect database | Pastikan MySQL nyala & connection string di `DatabaseService.cs` benar |
| Window nggak bisa di-resize | Sudah didukung — tarik pinggir window, atau klik tombol □ (maximize) |
| Jendela putih / kosong | Pastikan `App.xaml` StartupUri → `MainWindow.xaml` |

---

## 📄 Catatan

- Database asli lebih sederhana dari skema Laravel. Beberapa fitur (Format film, tipe & kota
  bioskop, kolom `is_admin`) butuh kolom tambahan — kalau mau mengaktifkannya, jalankan
  `Database/Fix_Add_Laravel_Columns.sql`.
- Anti double-booking: pakai transaksi `SERIALIZABLE` + kursi yang sudah dibeli langsung dikunci di UI.
