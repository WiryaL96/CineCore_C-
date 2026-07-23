using CineCore.Models;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace CineCore.Services
{
    public class DatabaseService : IDatabaseService
    {
        // ── Connection String standar Laragon (User: root, Password: kosong) ──
        private const string ConnectionString = "Server=127.0.0.1;Port=3306;Database=cinecore;Uid=root;Pwd=;SslMode=None;";

        private MySqlConnection GetConnection() => new(ConnectionString);

        // ──────────────────────────────────────────────────────────────────────
        // AUTHENTICATION
        // ──────────────────────────────────────────────────────────────────────
        public async Task<User?> LoginAsync(string email, string plainPassword)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            // Kita cuma cari berdasarkan email dulu, ngecek password-nya nanti
            const string sql = "SELECT id, name, email, created_at, password FROM users WHERE email=@Email";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null; // User ga ketemu

            // Ambil Hash Password dari Database
            var hashedPasswordFromDb = reader.GetString(4);

            // Cek kecocokan password menggunakan Bcrypt
            if (!AuthService.VerifyPassword(plainPassword, hashedPasswordFromDb))
            {
                return null; // Password salah
            }

            // Kalau benar, kembalikan data User
            return new User
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Email = reader.GetString(2),
                CreatedAt = reader.GetDateTime(3)
            };
        }

        public async Task<bool> RegisterAsync(string fullName, string email, string passwordHash)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            // Check duplicate
            await using var checkCmd = new MySqlCommand("SELECT COUNT(*) FROM users WHERE email=@Email", conn);
            checkCmd.Parameters.AddWithValue("@Email", email);
            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            if (count > 0) return false;

            // MySQL pakai NOW() bukan GETDATE()
            const string sql = "INSERT INTO users (name, email, password, created_at, updated_at) VALUES (@FN, @Email, @Hash, NOW(), NOW())";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FN", fullName);
            cmd.Parameters.AddWithValue("@Email", email);
            cmd.Parameters.AddWithValue("@Hash", passwordHash);

            await cmd.ExecuteNonQueryAsync();
            return true;
        }

        // ──────────────────────────────────────────────────────────────────────
        // MOVIES
        // ──────────────────────────────────────────────────────────────────────
        public async Task<List<Movie>> GetMoviesAsync()
        {
            var list = new List<Movie>();
            await using var conn = GetConnection();
            await conn.OpenAsync();

            const string sql = "SELECT * FROM movies ORDER BY release_date ASC";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            // Kumpulkan nama kolom yang benar-benar ADA di result set,
            // biar aman kalau schema DB beda (mis. kolom Laravel yang belum dimigrasi).
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                columns.Add(reader.GetName(i));

            while (await reader.ReadAsync())
            {
                list.Add(new Movie
                {
                    Id = Convert.ToInt32(reader["id"]),
                    Title = reader["title"].ToString() ?? "",
                    Genre = reader["genre"].ToString() ?? "",
                    // Ambil dari kolom duration_minutes
                    DurationMinutes = GetInt(reader, columns, "duration_minutes"),

                    PosterUrl = GetString(reader, columns, "poster_path"),
                    TrailerUrl = GetString(reader, columns, "trailer_url"),
                    IsShowing = GetBool(reader, columns, "is_showing", defaultValue: true),
                    ReleaseDate = GetDateTime(reader, columns, "release_date"),
                    Description = GetString(reader, columns, "description"),
                    Rating = GetDouble(reader, columns, "rating")
                });
            }
            return list;
        }

        // ── Helper baca kolom yang aman: balikin default kalau kolom nggak ada / NULL ──
        private static bool Has(HashSet<string> columns, string name, IDataRecord reader) =>
            columns.Contains(name) && reader[name] != DBNull.Value;

        private static string GetString(IDataRecord reader, HashSet<string> columns, string name) =>
            Has(columns, name, reader) ? reader[name].ToString() ?? "" : "";

        private static int GetInt(IDataRecord reader, HashSet<string> columns, string name, int defaultValue = 0) =>
            Has(columns, name, reader) ? Convert.ToInt32(reader[name]) : defaultValue;

        private static double GetDouble(IDataRecord reader, HashSet<string> columns, string name, double defaultValue = 0) =>
            Has(columns, name, reader) ? Convert.ToDouble(reader[name]) : defaultValue;

        private static bool GetBool(IDataRecord reader, HashSet<string> columns, string name, bool defaultValue = false) =>
            Has(columns, name, reader) ? Convert.ToBoolean(reader[name]) : defaultValue;

        private static DateTime GetDateTime(IDataRecord reader, HashSet<string> columns, string name) =>
            Has(columns, name, reader) ? Convert.ToDateTime(reader[name]) : DateTime.MinValue;

        // ──────────────────────────────────────────────────────────────────────
        // CINEMAS
        // ──────────────────────────────────────────────────────────────────────
        public async Task<List<Cinema>> GetCinemasAsync()
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            // SELECT * + baca per-nama kolom, biar AMAN di schema apapun.
            // Schema Laravel: id, name, type, address, city (TANPA total_rows/total_columns).
            const string sql = "SELECT * FROM cinemas ORDER BY id";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();

            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < reader.FieldCount; i++)
                columns.Add(reader.GetName(i));

            var list = new List<Cinema>();
            while (await reader.ReadAsync())
            {
                var name = GetString(reader, columns, "name");
                var type = GetString(reader, columns, "type"); // XXI / Premiere / IMAX (khusus schema Laravel)
                list.Add(new Cinema
                {
                    Id = GetInt(reader, columns, "id"),
                    // Gabung type biar bioskop yang senama tetap kebedain
                    Name = string.IsNullOrEmpty(type) ? name : $"{name} ({type})",
                    Location = GetString(reader, columns, "address"),
                    // total_rows/total_columns nggak ada di schema Laravel → pakai default (5 baris x 8 kolom)
                    TotalRows = GetInt(reader, columns, "total_rows", 5),
                    TotalColumns = GetInt(reader, columns, "total_columns", 8)
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────────────────────────────
        // SHOWTIMES
        // ──────────────────────────────────────────────────────────────────────

        // Pastikan SELALU ada jadwal untuk 7 hari ke depan (hari ini + 6),
        // 3 sesi/hari (10:00, 14:00, 19:00) untuk semua film tayang × semua bioskop.
        // Idempotent: aman dipanggil berkali-kali, nggak bikin jadwal dobel.
        public async Task EnsureShowtimesAsync()
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO showtimes (movie_id, cinema_id, start_time, price)
                SELECT x.movie_id, x.cinema_id, x.start_time, x.price
                FROM (
                    SELECT m.id AS movie_id, c.id AS cinema_id,
                           DATE_ADD(CURDATE(), INTERVAL d.offset DAY) + INTERVAL t.hr HOUR AS start_time,
                           t.price AS price
                    FROM movies m
                    CROSS JOIN cinemas c
                    CROSS JOIN (SELECT 0 AS offset UNION SELECT 1 UNION SELECT 2 UNION SELECT 3
                                UNION SELECT 4 UNION SELECT 5 UNION SELECT 6) d
                    CROSS JOIN (SELECT 10 AS hr, 50000 AS price UNION ALL
                                SELECT 14, 65000 UNION ALL
                                SELECT 19, 75000) t
                    WHERE m.is_showing = 1
                ) x
                WHERE NOT EXISTS (
                    SELECT 1 FROM showtimes s
                    WHERE s.movie_id = x.movie_id
                      AND s.cinema_id = x.cinema_id
                      AND s.start_time = x.start_time
                );";

            await using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<List<Showtime>> GetShowtimesAsync(int movieId, int cinemaId, DateTime date)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            // MySQL: mengekstrak DATE dan TIME dari start_time
            const string sql = @"
                SELECT s.id, s.movie_id, s.cinema_id, DATE(s.start_time), TIME(s.start_time), s.price, c.name
                FROM showtimes s
                JOIN cinemas c ON c.id = s.cinema_id
                WHERE s.movie_id=@MovieId AND s.cinema_id=@CinemaId
                  AND DATE(s.start_time) = DATE(@Date)
                ORDER BY s.start_time";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MovieId", movieId);
            cmd.Parameters.AddWithValue("@CinemaId", cinemaId);
            cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));

            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<Showtime>();
            while (await reader.ReadAsync())
            {
                list.Add(new Showtime
                {
                    Id = reader.GetInt32(0),
                    MovieId = reader.GetInt32(1),
                    CinemaId = reader.GetInt32(2),
                    ShowDate = reader.GetDateTime(3),
                    StartTime = (TimeSpan)reader.GetValue(4),
                    PricePerSeat = reader.GetDecimal(5),
                    CinemaName = reader.GetString(6),
                });
            }
            return list;
        }

        // ──────────────────────────────────────────────────────────────────────
        // BOOKED SEATS
        // ──────────────────────────────────────────────────────────────────────
        public async Task<List<string>> GetBookedSeatsAsync(int showtimeId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            // Menyesuaikan nama kolom Laravel: seat_number, booking_id
            const string sql = @"
                SELECT bs.seat_number FROM booking_seats bs
                JOIN bookings b ON b.id = bs.booking_id
                WHERE b.showtime_id = @ShowtimeId AND b.status != 'cancelled'";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@ShowtimeId", showtimeId);

            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<string>();
            while (await reader.ReadAsync()) list.Add(reader.GetString(0));
            return list;
        }

        // ──────────────────────────────────────────────────────────────────────
        // BOOKING (with transaction)
        // ──────────────────────────────────────────────────────────────────────
        public async Task<(bool Success, string BookingCode)> CreateBookingAsync(
            int userId, int showtimeId, List<string> seats, decimal total, string paymentMethod)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var transaction = await conn.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                // Double-booking guard
                var placeholders = string.Join(",", seats.Select((_, i) => $"@S{i}"));
                var checkSql = $@"
                    SELECT COUNT(*) FROM booking_seats bs
                    JOIN bookings b ON b.id = bs.booking_id
                    WHERE b.showtime_id = @ShowtimeId
                      AND b.status != 'cancelled'
                      AND bs.seat_number IN ({placeholders})";

                await using var checkCmd = new MySqlCommand(checkSql, conn, transaction);
                checkCmd.Parameters.AddWithValue("@ShowtimeId", showtimeId);
                for (int i = 0; i < seats.Count; i++)
                    checkCmd.Parameters.AddWithValue($"@S{i}", seats[i]);

                var conflict = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (conflict > 0) { await transaction.RollbackAsync(); return (false, ""); }

                // Create booking (Menyesuaikan kolom Laravel)
                var code = "CC" + DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(100, 999);
                const string bookingSql = @"
                    INSERT INTO bookings (user_id, showtime_id, total_price, payment_method, status, booking_reference, created_at, updated_at)
                    VALUES (@UserId, @ShowtimeId, @Total, @Payment, 'paid', @Code, NOW(), NOW());
                    SELECT LAST_INSERT_ID();";

                await using var bookingCmd = new MySqlCommand(bookingSql, conn, transaction);
                bookingCmd.Parameters.AddWithValue("@UserId", userId);
                bookingCmd.Parameters.AddWithValue("@ShowtimeId", showtimeId);
                bookingCmd.Parameters.AddWithValue("@Total", total);
                bookingCmd.Parameters.AddWithValue("@Payment", paymentMethod);
                bookingCmd.Parameters.AddWithValue("@Code", code);

                var bookingId = Convert.ToInt32(await bookingCmd.ExecuteScalarAsync());

                // Insert seat rows (Menyesuaikan kolom Laravel)
                foreach (var seat in seats)
                {
                    const string seatSql = "INSERT INTO booking_seats (booking_id, showtime_id, seat_number, created_at, updated_at) VALUES (@BId, @ShowtimeId, @Seat, NOW(), NOW())";
                    await using var seatCmd = new MySqlCommand(seatSql, conn, transaction);
                    seatCmd.Parameters.AddWithValue("@BId", bookingId);
                    seatCmd.Parameters.AddWithValue("@ShowtimeId", showtimeId);
                    seatCmd.Parameters.AddWithValue("@Seat", seat);
                    await seatCmd.ExecuteNonQueryAsync();
                }

                await transaction.CommitAsync();
                return (true, code);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // BOOKING HISTORY
        // ──────────────────────────────────────────────────────────────────────
        public async Task<List<Booking>> GetBookingHistoryAsync(int userId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();

            const string sql = @"
                SELECT b.id, b.showtime_id, b.total_price, b.payment_method, b.status, 
                       b.booking_reference, b.created_at,
                       m.title, m.poster_path, m.genre,
                       s.start_time, s.price,
                       c.name AS cinema_name
                FROM bookings b
                JOIN showtimes s ON s.id = b.showtime_id
                JOIN movies m ON m.id = s.movie_id
                JOIN cinemas c ON c.id = s.cinema_id
                WHERE b.user_id = @UserId
                ORDER BY b.created_at DESC";

            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            await using var reader = await cmd.ExecuteReaderAsync();

            var list = new List<Booking>();
            while (await reader.ReadAsync())
            {
                var startTime = reader.GetDateTime(reader.GetOrdinal("start_time"));
                list.Add(new Booking
                {
                    Id = reader.GetInt32(0),
                    ShowtimeId = reader.GetInt32(1),
                    UserId = userId,
                    TotalAmount = reader.GetDecimal(2),
                    PaymentMethod = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Status = reader.IsDBNull(4) ? "" : reader.GetString(4),
                    BookingCode = reader.IsDBNull(5) ? "" : reader.GetString(5),
                    BookedAt = reader.GetDateTime(6),
                    Movie = new Movie
                    {
                        Title = reader.GetString(7),
                        PosterUrl = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        Genre = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    },
                    Showtime = new Showtime
                    {
                        ShowDate = startTime.Date,
                        StartTime = startTime.TimeOfDay,
                        PricePerSeat = reader.GetDecimal(11),
                        CinemaName = reader.GetString(12),
                    }
                });
            }

            // Load seats for each booking
            await reader.CloseAsync();
            foreach (var booking in list)
            {
                const string seatSql = "SELECT seat_number FROM booking_seats WHERE booking_id = @BId";
                await using var seatCmd = new MySqlCommand(seatSql, conn);
                seatCmd.Parameters.AddWithValue("@BId", booking.Id);
                await using var seatReader = await seatCmd.ExecuteReaderAsync();
                while (await seatReader.ReadAsync())
                {
                    booking.Seats.Add(new BookingSeat { SeatLabel = seatReader.GetString(0), BookingId = booking.Id });
                }
            }

            return list;
        }

        // ──────────────────────────────────────────────────────────────────────
        // ADMIN: MOVIES CRUD
        // ──────────────────────────────────────────────────────────────────────
        public async Task<bool> AddMovieAsync(Movie movie)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            const string sql = @"INSERT INTO movies (title, genre, duration_minutes, poster_path, trailer_url, is_showing, release_date, created_at, updated_at) 
                                 VALUES (@Title, @Genre, @Duration, @Poster, @Trailer, @IsShowing, @ReleaseDate, NOW(), NOW())";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Title", movie.Title);
            cmd.Parameters.AddWithValue("@Genre", movie.Genre);
            cmd.Parameters.AddWithValue("@Duration", movie.DurationMinutes);
            cmd.Parameters.AddWithValue("@Poster", movie.PosterUrl);
            cmd.Parameters.AddWithValue("@Trailer", movie.TrailerUrl);
            cmd.Parameters.AddWithValue("@IsShowing", movie.IsShowing);
            cmd.Parameters.AddWithValue("@ReleaseDate", movie.ReleaseDate);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> UpdateMovieAsync(Movie movie)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            const string sql = @"UPDATE movies SET title=@Title, genre=@Genre, duration_minutes=@Duration, 
                                 poster_path=@Poster, trailer_url=@Trailer, is_showing=@IsShowing, 
                                 release_date=@ReleaseDate, updated_at=NOW() WHERE id=@Id";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", movie.Id);
            cmd.Parameters.AddWithValue("@Title", movie.Title);
            cmd.Parameters.AddWithValue("@Genre", movie.Genre);
            cmd.Parameters.AddWithValue("@Duration", movie.DurationMinutes);
            cmd.Parameters.AddWithValue("@Poster", movie.PosterUrl);
            cmd.Parameters.AddWithValue("@Trailer", movie.TrailerUrl);
            cmd.Parameters.AddWithValue("@IsShowing", movie.IsShowing);
            cmd.Parameters.AddWithValue("@ReleaseDate", movie.ReleaseDate);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteMovieAsync(int movieId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("DELETE FROM movies WHERE id=@Id", conn);
            cmd.Parameters.AddWithValue("@Id", movieId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        // ADMIN: SHOWTIMES
        public async Task<bool> AddShowtimeAsync(int movieId, int cinemaId, DateTime startTime, decimal price)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            const string sql = @"INSERT INTO showtimes (movie_id, cinema_id, start_time, price, created_at, updated_at) 
                                 VALUES (@MovieId, @CinemaId, @StartTime, @Price, NOW(), NOW())";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MovieId", movieId);
            cmd.Parameters.AddWithValue("@CinemaId", cinemaId);
            cmd.Parameters.AddWithValue("@StartTime", startTime);
            cmd.Parameters.AddWithValue("@Price", price);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteShowtimeAsync(int showtimeId)
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("DELETE FROM showtimes WHERE id=@Id", conn);
            cmd.Parameters.AddWithValue("@Id", showtimeId);
            return await cmd.ExecuteNonQueryAsync() > 0;
        }

        public async Task<List<Showtime>> GetAllShowtimesAsync()
        {
            await using var conn = GetConnection();
            await conn.OpenAsync();
            const string sql = @"SELECT s.id, s.movie_id, s.cinema_id, DATE(s.start_time), TIME(s.start_time), s.price, 
                                 c.name, m.title
                                 FROM showtimes s
                                 JOIN cinemas c ON c.id = s.cinema_id
                                 JOIN movies m ON m.id = s.movie_id
                                 ORDER BY s.start_time DESC";
            await using var cmd = new MySqlCommand(sql, conn);
            await using var reader = await cmd.ExecuteReaderAsync();
            var list = new List<Showtime>();
            while (await reader.ReadAsync())
            {
                list.Add(new Showtime
                {
                    Id = reader.GetInt32(0),
                    MovieId = reader.GetInt32(1),
                    CinemaId = reader.GetInt32(2),
                    ShowDate = reader.GetDateTime(3),
                    StartTime = (TimeSpan)reader.GetValue(4),
                    PricePerSeat = reader.GetDecimal(5),
                    CinemaName = reader.GetString(6),
                    MovieTitle = reader.GetString(7),
                });
            }
            return list;
        }

    }
}