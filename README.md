# CineCore – WPF Cinema Ticket Booking App

> **Stack:** C# · .NET 8 · WPF · MVVM · SQL Server (or MySQL)  
> **Pattern:** MVVM with manual ICommand implementation — zero third-party MVVM frameworks required.

---

## 📁 Project Structure

```
CineCore/
├── CineCore.csproj
├── App.xaml / App.xaml.cs
├── MainWindow.xaml / .cs          ← Shell window + custom title bar
│
├── Models/
│   ├── User.cs
│   ├── Movie.cs
│   └── CinemaModels.cs            ← Cinema, Showtime, Booking, BookingSeat, Seat
│
├── ViewModels/
│   ├── Base/
│   │   └── ViewModelBase.cs       ← INotifyPropertyChanged + RelayCommand + AsyncRelayCommand
│   ├── LoginViewModel.cs
│   ├── RegisterViewModel.cs
│   ├── DashboardViewModel.cs
│   ├── MovieDetailViewModel.cs
│   ├── SeatSelectionViewModel.cs
│   └── PaymentViewModel.cs
│
├── Views/
│   ├── LoginView.xaml / .cs
│   ├── RegisterView.xaml / .cs
│   ├── DashboardView.xaml / .cs
│   ├── MovieDetailView.xaml / .cs
│   ├── SeatSelectionView.xaml / .cs
│   └── PaymentView.xaml / .cs
│
├── Services/
│   ├── NavigationService.cs       ← Page routing
│   ├── DatabaseService.cs         ← All DB operations
│   └── AuthService.cs             ← Password hashing + Session
│
├── Converters/
│   └── Converters.cs              ← All value converters
│
└── Database/
    ├── CineCore_Schema.sql        ← SQL Server schema + seed data
    └── CineCore_MySQL.sql         ← MySQL alternative
```

---

## 🚀 Step-by-Step: How to Run

### Prerequisites
| Tool | Version |
|------|---------|
| Visual Studio 2022 | 17.8+ (with .NET desktop workload) |
| .NET SDK | 8.0+ |
| SQL Server | 2019+ (or MySQL 8.0+) |
| SQL Server Management Studio | Optional but recommended |

---

### Step 1 — Clone / Copy the Project

```bash
# If using git
git clone https://github.com/yourname/CineCore.git
cd CineCore

# Or simply open the CineCore/ folder in Visual Studio 2022
```

---

### Step 2 — Set Up the Database

#### Option A: SQL Server (recommended)

1. Open **SQL Server Management Studio (SSMS)**
2. Connect to your SQL Server instance (e.g., `localhost` or `.\SQLEXPRESS`)
3. Open `Database/CineCore_Schema.sql`
4. Press **F5** (Execute All) — this creates the DB, tables, and seed data

#### Option B: MySQL

1. Open MySQL Workbench or any MySQL client
2. Run `Database/CineCore_MySQL.sql`
3. Install MySqlConnector instead of SqlClient (see Step 3B)

---

### Step 3A — Configure Connection String (SQL Server)

Open `Services/DatabaseService.cs` and update line 15:

```csharp
private const string ConnectionString =
    "Server=localhost;Database=CineCore;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;";
```

**Common SQL Server connection strings:**
```
// Windows Authentication (no password needed)
"Server=localhost;Database=CineCore;Integrated Security=True;TrustServerCertificate=True;"

// SQL Express named instance
"Server=.\SQLEXPRESS;Database=CineCore;Integrated Security=True;TrustServerCertificate=True;"

// SQL Server with username/password
"Server=localhost;Database=CineCore;User Id=sa;Password=YourPass;TrustServerCertificate=True;"
```

### Step 3B — Switch to MySQL (optional)

1. In `CineCore.csproj`, replace the PackageReference:
```xml
<!-- Remove this: -->
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.2.1" />
<!-- Add this: -->
<PackageReference Include="MySqlConnector" Version="2.3.5" />
```

2. In `Services/DatabaseService.cs`:
   - Add `using MySqlConnector;` at the top
   - Replace all `SqlConnection` → `MySqlConnection`
   - Replace all `SqlCommand` → `MySqlCommand`  
   - Replace all `SqlDataReader` → `MySqlDataReader`
   - Replace `GETDATE()` → `NOW()` in SQL strings
   - Replace `OUTPUT INSERTED.Id` → use `ExecuteNonQuery() + LastInsertedId`
   - Update connection string: `"Server=localhost;Database=CineCore;User=root;Password=pass;"`

---

### Step 4 — Restore NuGet Packages

Open **Package Manager Console** (Tools → NuGet Package Manager):
```powershell
# Restore all packages
dotnet restore

# Or manually install
Install-Package Microsoft.Data.SqlClient
```

Or simply **Build → Rebuild Solution** — Visual Studio auto-restores.

---

### Step 5 — Build & Run

```
Press F5 in Visual Studio 2022
— or —
dotnet run
```

**Demo credentials (from seed data):**
| Email | Password |
|-------|----------|
| `demo@cinecore.id` | `demo1234` |

---

## 🔑 Architecture Notes

### MVVM Pattern
```
View (XAML)
  ↕ Data Binding
ViewModel (C#)   ← ICommand, ObservableCollection, INotifyPropertyChanged
  ↕ Service calls
Service (C#)     ← DatabaseService, AuthService, NavigationService
  ↕ ADO.NET
Database (SQL)
```

### Navigation Flow
```
MainWindow (shell)
  └── LoginView
       ├── RegisterView
       └── DashboardView
            └── MovieDetailView
                 └── SeatSelectionView
                      └── PaymentView
                           └── (back to DashboardView on success)
```

### Key Commands

| ViewModel | Command | Action |
|-----------|---------|--------|
| LoginViewModel | `LoginCommand` | Authenticate + navigate |
| DashboardViewModel | `BuyTicketCommand<Movie>` | Open movie detail |
| MovieDetailViewModel | `SelectSeatsCommand` | Open seat map |
| SeatSelectionViewModel | `ToggleSeatCommand<Seat>` | Toggle seat state |
| PaymentViewModel | `ConfirmCommand` | Create booking (transaction) |

### Double-Booking Prevention
The app uses two layers:
1. **Database:** `SERIALIZABLE` transaction + `SELECT COUNT(*)` check before insert
2. **UI:** Booked seats are disabled (grayed out) immediately on load

---

## 🎨 UI Dark Theme Colors

| Token | Hex | Usage |
|-------|-----|-------|
| `BgDeep` | `#0A0B14` | App background |
| `BgCard` | `#12141F` | Cards, panels |
| `BgInput` | `#1A1C2A` | Input fields |
| `Accent` | `#007AFF` | Buttons, highlights |
| `TextPrimary` | `#F0F2FF` | Headings |
| `TextSecondary` | `#8B8FA8` | Labels, subtitles |
| `Border` | `#252736` | Dividers |
| `Danger` | `#FF3B30` | Errors |

---

## 🔒 Security Notes (Production Hardening)

1. **Password hashing:** Replace SHA-256 with BCrypt:
   ```csharp
   // Install-Package BCrypt.Net-Next
   // Hash: BCrypt.Net.BCrypt.HashPassword(password)
   // Verify: BCrypt.Net.BCrypt.Verify(password, hash)
   ```

2. **Connection string:** Store in `appsettings.json` or Windows Credential Manager, not hardcoded.

3. **SQL injection:** All queries use parameterized `SqlCommand` — no string concatenation.

4. **Transactions:** Booking uses `IsolationLevel.Serializable` to prevent race conditions.

---

## 📦 NuGet Packages Used

| Package | Purpose |
|---------|---------|
| `Microsoft.Data.SqlClient` 5.2.1 | SQL Server data access |
| *(Optional)* `MySqlConnector` 2.3.5 | MySQL alternative |
| *(Optional)* `BCrypt.Net-Next` 4.0.3 | Secure password hashing |

---

## 🐛 Troubleshooting

| Problem | Solution |
|---------|----------|
| "Cannot connect to database" | Check connection string; ensure SQL Server is running |
| "Login failed for user 'sa'" | Enable SQL Server Authentication in SSMS |
| Build error on `SqlDataReader.GetTimeSpan` | Ensure `Microsoft.Data.SqlClient` 5.x is installed (not `System.Data.SqlClient`) |
| White/blank window | Check App.xaml `StartupUri` points to `MainWindow.xaml` |
| Movie images not loading | App uses URLs from TMDB — requires internet; replace with local image paths if offline |
