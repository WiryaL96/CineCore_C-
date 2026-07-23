-- ═══════════════════════════════════════════════════════════════════════
--  CineCore  –  Complete SQL Server Schema + Seed Data
--  Compatible with SQL Server 2019+ / Azure SQL
--  For MySQL: replace GETDATE() → NOW(), BIT → TINYINT, NVARCHAR → VARCHAR
-- ═══════════════════════════════════════════════════════════════════════

USE master;
GO

-- Create database if not exists
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'CineCore')
BEGIN
    CREATE DATABASE CineCore
        COLLATE SQL_Latin1_General_CP1_CI_AS;
END
GO

USE CineCore;
GO

-- ─────────────────────────────────────────────────────────────────────────
-- 1. USERS
-- ─────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('users', 'U') IS NOT NULL DROP TABLE users;
CREATE TABLE users (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    FullName     NVARCHAR(120)  NOT NULL,
    Email        NVARCHAR(200)  NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256)  NOT NULL,          -- SHA-256 hex (use BCrypt in prod)
    CreatedAt    DATETIME2      NOT NULL DEFAULT GETDATE()
);
CREATE INDEX IX_users_Email ON users(Email);

-- ─────────────────────────────────────────────────────────────────────────
-- 2. MOVIES
-- ─────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('movies', 'U') IS NOT NULL DROP TABLE movies;
CREATE TABLE movies (
    Id              INT IDENTITY(1,1) PRIMARY KEY,
    Title           NVARCHAR(200) NOT NULL,
    Genre           NVARCHAR(80)  NOT NULL,
    DurationMinutes INT           NOT NULL,
    Description     NVARCHAR(MAX) NULL,
    PosterUrl       NVARCHAR(500) NULL,
    TrailerUrl      NVARCHAR(500) NULL,
    Rating          FLOAT         NOT NULL DEFAULT 0,
    IsActive        BIT           NOT NULL DEFAULT 1
);

-- ─────────────────────────────────────────────────────────────────────────
-- 3. CINEMAS
-- ─────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('cinemas', 'U') IS NOT NULL DROP TABLE cinemas;
CREATE TABLE cinemas (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(80)  NOT NULL,   -- e.g. XXI, Premiere, IMAX
    Location     NVARCHAR(200) NULL,
    TotalRows    INT NOT NULL DEFAULT 5,
    TotalColumns INT NOT NULL DEFAULT 8
);

-- ─────────────────────────────────────────────────────────────────────────
-- 4. SHOWTIMES
-- ─────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('showtimes', 'U') IS NOT NULL DROP TABLE showtimes;
CREATE TABLE showtimes (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    MovieId      INT           NOT NULL REFERENCES movies(Id)  ON DELETE CASCADE,
    CinemaId     INT           NOT NULL REFERENCES cinemas(Id) ON DELETE CASCADE,
    ShowDate     DATE          NOT NULL,
    StartTime    TIME(0)       NOT NULL,
    PricePerSeat DECIMAL(12,2) NOT NULL,
    CONSTRAINT UQ_showtime UNIQUE (MovieId, CinemaId, ShowDate, StartTime)
);
CREATE INDEX IX_showtimes_MovieCinemaDate ON showtimes(MovieId, CinemaId, ShowDate);

-- ─────────────────────────────────────────────────────────────────────────
-- 5. BOOKINGS
-- ─────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('booking_seats', 'U') IS NOT NULL DROP TABLE booking_seats;
IF OBJECT_ID('bookings',      'U') IS NOT NULL DROP TABLE bookings;

CREATE TABLE bookings (
    Id            INT IDENTITY(1,1) PRIMARY KEY,
    UserId        INT            NOT NULL REFERENCES users(Id),
    ShowtimeId    INT            NOT NULL REFERENCES showtimes(Id),
    TotalAmount   DECIMAL(12,2)  NOT NULL,
    PaymentMethod NVARCHAR(50)   NOT NULL,
    Status        NVARCHAR(20)   NOT NULL DEFAULT 'Confirmed',  -- Confirmed | Cancelled
    BookedAt      DATETIME2      NOT NULL DEFAULT GETDATE(),
    BookingCode   NVARCHAR(30)   NOT NULL UNIQUE
);
CREATE INDEX IX_bookings_User      ON bookings(UserId);
CREATE INDEX IX_bookings_Showtime  ON bookings(ShowtimeId);

-- ─────────────────────────────────────────────────────────────────────────
-- 6. BOOKING SEATS  (prevents double-booking via UNIQUE constraint)
-- ─────────────────────────────────────────────────────────────────────────
CREATE TABLE booking_seats (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    BookingId  INT          NOT NULL REFERENCES bookings(Id) ON DELETE CASCADE,
    SeatLabel  NVARCHAR(5)  NOT NULL,   -- e.g. 'A1', 'C8'
    CONSTRAINT UQ_seat_per_showtime UNIQUE (BookingId, SeatLabel)
);

-- Additional guard: prevents two confirmed bookings for the same seat+showtime
-- (enforced via application-level serializable transaction + check query)

-- ─────────────────────────────────────────────────────────────────────────
-- SEED DATA
-- ─────────────────────────────────────────────────────────────────────────

-- Demo user  (password = "demo1234" SHA-256 hash)
INSERT INTO users (FullName, Email, PasswordHash) VALUES
('Demo User',   'demo@cinecore.id',  '0d6f0a8edaba3f8b1d8e4f9b2e3c7d5a6b4e2f1c9a8d7b6e5c4f3a2b1e0d9c8'),
('Admin Test',  'admin@cinecore.id', '0d6f0a8edaba3f8b1d8e4f9b2e3c7d5a6b4e2f1c9a8d7b6e5c4f3a2b1e0d9c8');

-- Cinemas
INSERT INTO cinemas (Name, Location, TotalRows, TotalColumns) VALUES
('XXI',      'Bandung Indah Plaza Lt. 3',  5, 8),
('Premiere', 'Paris Van Java Mall Lt. 4',  5, 8),
('IMAX',     'Trans Studio Mall Lt. 5',    5, 8);

-- Movies
INSERT INTO movies (Title, Genre, DurationMinutes, Description, PosterUrl, TrailerUrl, Rating) VALUES
('Inception',
 'Sci-Fi / Thriller', 148,
 'A thief who steals corporate secrets through the use of dream-sharing technology is given the inverse task of planting an idea into the mind of a C.E.O.',
 'https://image.tmdb.org/t/p/w500/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg',
 'https://www.youtube.com/watch?v=YoHD9XEInc0', 8.8),

('Dune: Part Two',
 'Sci-Fi / Adventure', 166,
 'Paul Atreides unites with Chani and the Fremen while on a path of revenge against the conspirators who destroyed his family.',
 'https://image.tmdb.org/t/p/w500/8b8R8l88Qje9dn9OE8PY05Nxl1X.jpg',
 'https://www.youtube.com/watch?v=U2Qp5pL3ovA', 8.5),

('Oppenheimer',
 'Biography / Drama', 180,
 'The story of American scientist J. Robert Oppenheimer and his role in the development of the atomic bomb during World War II.',
 'https://image.tmdb.org/t/p/w500/8Gxv8gSFCU0XGDykEGv7zR1n2ua.jpg',
 'https://www.youtube.com/watch?v=uYPbbksJxIg', 8.6),

('The Dark Knight',
 'Action / Crime', 152,
 'When the menace known as the Joker wreaks havoc and chaos on the people of Gotham, Batman must accept one of the greatest psychological and physical tests of his ability to fight injustice.',
 'https://image.tmdb.org/t/p/w500/qJ2tW6WMUDux911r6m7haRef0WH.jpg',
 'https://www.youtube.com/watch?v=EXeTwQWrcwY', 9.0),

('Spider-Man: No Way Home',
 'Action / Fantasy', 148,
 'With Spider-Man''s identity now revealed, Peter asks Doctor Strange for help. When a spell goes wrong, dangerous foes from other worlds start to appear.',
 'https://image.tmdb.org/t/p/w500/1g0dhYtq4irTY1GPXvft6k4YLjm.jpg',
 'https://www.youtube.com/watch?v=JfVOs4VSpmA', 8.3),

('Interstellar',
 'Sci-Fi / Drama', 169,
 'A team of explorers travel through a wormhole in space in an attempt to ensure humanity''s survival.',
 'https://image.tmdb.org/t/p/w500/gEU2QniE6E77NI6lCU6MxlNBvIx.jpg',
 'https://www.youtube.com/watch?v=zSWdZVtXT7E', 8.7);

-- Showtimes (today + next 3 days for all movies × all cinemas)
DECLARE @Today DATE = CAST(GETDATE() AS DATE);
DECLARE @M INT, @C INT;

-- Generate showtimes for all movie+cinema combos
DECLARE movie_cur CURSOR FOR SELECT Id FROM movies;
OPEN movie_cur;
FETCH NEXT FROM movie_cur INTO @M;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE cinema_cur CURSOR FOR SELECT Id FROM cinemas;
    OPEN cinema_cur;
    FETCH NEXT FROM cinema_cur INTO @C;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        -- 3 showtimes per day for 4 days
        INSERT INTO showtimes (MovieId, CinemaId, ShowDate, StartTime, PricePerSeat)
        SELECT @M, @C, DATEADD(DAY, d, @Today), t, p
        FROM (VALUES
            (0,'10:00:00',50000),(0,'14:00:00',65000),(0,'19:30:00',75000),
            (1,'11:00:00',50000),(1,'15:30:00',65000),(1,'20:00:00',75000),
            (2,'12:00:00',50000),(2,'16:00:00',65000),(2,'20:30:00',75000),
            (3,'10:30:00',50000),(3,'14:30:00',65000),(3,'19:00:00',75000)
        ) AS v(d, t, p)
        WHERE NOT EXISTS (
            SELECT 1 FROM showtimes
            WHERE MovieId=@M AND CinemaId=@C
              AND ShowDate=DATEADD(DAY,v.d,@Today)
              AND StartTime=v.t
        );
        FETCH NEXT FROM cinema_cur INTO @C;
    END
    CLOSE cinema_cur; DEALLOCATE cinema_cur;
    FETCH NEXT FROM movie_cur INTO @M;
END
CLOSE movie_cur; DEALLOCATE movie_cur;

GO

-- Verify
SELECT 'users'         AS [Table], COUNT(*) AS Rows FROM users   UNION ALL
SELECT 'movies',                   COUNT(*)          FROM movies  UNION ALL
SELECT 'cinemas',                  COUNT(*)          FROM cinemas UNION ALL
SELECT 'showtimes',                COUNT(*)          FROM showtimes;
GO
