using CineCore.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Globalization;
using System.Linq;

namespace CineCore.Services
{
    // Generate PDF report "Sales & Ticketing Summary" pakai QuestPDF (A4 portrait).
    public static class ReportPdfService
    {
        private static readonly CultureInfo Id = new("id-ID");
        private static bool _licensed;

        private static string Rp(decimal v) => "Rp " + v.ToString("#,##0", Id);

        public static void Generate(SalesReport report, string filePath)
        {
            if (!_licensed)
            {
                QuestPDF.Settings.License = LicenseType.Community;
                _licensed = true;
            }

            var now = DateTime.Now;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(28);
                    page.DefaultTextStyle(t => t.FontSize(10).FontColor("#0f172a").FontFamily("Segoe UI"));

                    page.Header().Element(h => ComposeHeader(h, now));
                    page.Content().PaddingVertical(10).Element(c => ComposeContent(c, report));
                    page.Footer().Element(f => ComposeFooter(f, now));
                });
            }).GeneratePdf(filePath);
        }

        private static void ComposeHeader(IContainer container, DateTime now)
        {
            container.Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Item().Text(t =>
                        {
                            t.Span("Cine").FontSize(22).Bold().FontColor("#1d4ed8");
                            t.Span("Core").FontSize(22).Bold().FontColor("#7c3aed");
                        });
                        left.Item().Text("SALES & TICKETING SUMMARY REPORT")
                            .FontSize(11).Bold().FontColor("#0f172a");
                    });

                    row.ConstantItem(200).AlignRight().Column(right =>
                    {
                        right.Item().AlignRight().Text($"Generated: {now.ToString("MMMM dd, yyyy", CultureInfo.InvariantCulture)}")
                            .FontSize(9).FontColor("#64748b");
                        right.Item().AlignRight().Text("Scope: All Cities & Cinemas")
                            .FontSize(9).FontColor("#64748b");
                    });
                });

                col.Item().PaddingTop(6).LineHorizontal(3).LineColor("#1e293b");
            });
        }

        private static void ComposeContent(IContainer container, SalesReport report)
        {
            container.Column(col =>
            {
                col.Spacing(16);

                // ── 4 STAT BOXES ──
                col.Item().Row(row =>
                {
                    row.Spacing(10);
                    StatBox(row.RelativeItem(), "TOTAL REVENUE", Rp(report.TotalRevenue));
                    StatBox(row.RelativeItem(), "TICKETS SOLD", $"{report.TotalTickets} Pcs");
                    StatBox(row.RelativeItem(), "TOTAL BOOKINGS", report.TotalBookings.ToString());
                    StatBox(row.RelativeItem(), "ACTIVE MOVIES", $"{report.ActiveMovies} Titles");
                });

                // ── TRANSACTIONS TABLE ──
                col.Item().Column(section =>
                {
                    section.Item().PaddingBottom(6).BorderBottom(2).BorderColor("#e2e8f0")
                        .Text("All Paid Transactions").FontSize(13).Bold();

                    section.Item().PaddingTop(8).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(2.2f); // Date
                            c.RelativeColumn(1.8f); // Reference
                            c.RelativeColumn(1.8f); // Customer
                            c.RelativeColumn(2.4f); // Movie
                            c.RelativeColumn(2.2f); // Cinema
                            c.RelativeColumn(1.8f); // Seats
                            c.RelativeColumn(1.6f); // Amount
                            c.RelativeColumn(1.2f); // Status
                        });

                        table.Header(header =>
                        {
                            void H(string text, bool right = false)
                            {
                                var cell = header.Cell().Background("#1e293b").PaddingVertical(5).PaddingHorizontal(4);
                                (right ? cell.AlignRight() : cell.AlignLeft())
                                    .Text(text).FontSize(9).Bold().FontColor("#ffffff");
                            }
                            H("DATE"); H("REFERENCE"); H("CUSTOMER"); H("MOVIE");
                            H("CINEMA"); H("SEATS"); H("AMOUNT", true); H("STATUS");
                        });

                        int i = 0;
                        foreach (var tx in report.RecentTransactions)
                        {
                            var bg = (i++ % 2 == 0) ? "#f8fafc" : "#ffffff";

                            IContainer Cell() => table.Cell().Background(bg).PaddingVertical(4).PaddingHorizontal(4);

                            Cell().Text(tx.Date.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture)).FontSize(9);
                            Cell().Text(tx.Reference).FontSize(9).FontColor("#334155");
                            Cell().Text(tx.Customer).FontSize(9);
                            Cell().Text(tx.Movie).FontSize(9);
                            Cell().Text(tx.Cinema).FontSize(9);
                            Cell().Text(tx.SeatsText).FontSize(9);
                            Cell().AlignRight().Text(Rp(tx.Amount)).FontSize(9).Bold();
                            Cell().AlignCenter().Element(e => e.Background("#dcfce7").PaddingVertical(2).PaddingHorizontal(6)
                                .Text("Paid").FontSize(8).Bold().FontColor("#166534"));
                        }

                        if (report.RecentTransactions.Count == 0)
                        {
                            table.Cell().ColumnSpan(8).Background("#f8fafc").Padding(16).AlignCenter()
                                .Text("No paid transactions yet.").FontSize(10).FontColor("#64748b");
                        }
                    });

                    // ── TOTAL ROW ──
                    section.Item().PaddingTop(10).AlignRight().Text(
                        $"TOTAL: {report.RecentTransactions.Count} transactions  —  {Rp(report.TotalRevenue)}")
                        .FontSize(11).Bold().FontColor("#0f172a");
                });
            });
        }

        private static void StatBox(IContainer container, string label, string value)
        {
            container.Border(1).BorderColor("#e2e8f0").Background("#f8fafc").CornerRadius(8)
                .PaddingVertical(12).PaddingHorizontal(14).Column(col =>
                {
                    col.Item().Text(label).FontSize(8).FontColor("#64748b").LetterSpacing(0.06f);
                    col.Item().PaddingTop(4).Text(value).FontSize(15).Bold().FontColor("#0f172a");
                });
        }

        private static void ComposeFooter(IContainer container, DateTime now)
        {
            container.BorderTop(1).BorderColor("#e2e8f0").PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text("CineCore Management System — Confidential")
                    .FontSize(9).FontColor("#94a3b8");
                row.RelativeItem().AlignRight().Text($"Generated on {now:dd/MM/yyyy HH:mm}")
                    .FontSize(9).FontColor("#94a3b8");
            });
        }
    }
}
