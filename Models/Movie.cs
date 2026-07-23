using System;

namespace CineCore.Models
{
    public class Movie
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Genre { get; set; } = string.Empty;
        public int DurationMinutes { get; set; }
        public string Description { get; set; } = string.Empty;
        public string PosterUrl { get; set; } = string.Empty;
        public string TrailerUrl { get; set; } = string.Empty;
        public double Rating { get; set; }

        // ── BARU DITAMBAHKAN BIAR SINKRON SAMA DATABASE LARAVEL ──
        public bool IsShowing { get; set; }
        public DateTime ReleaseDate { get; set; }
    }
}