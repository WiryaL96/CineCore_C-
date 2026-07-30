using CineCore.Services;
using CineCore.ViewModels;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CineCore.Views
{
    public partial class BookingConfirmationView : UserControl
    {
        public BookingConfirmationView(BookingConfirmationParams p)
        {
            InitializeComponent();
            DataContext = new BookingConfirmationViewModel(p);
        }

        private void DownloadPng_Click(object sender, RoutedEventArgs e) => ExportTicket("png");
        private void DownloadJpg_Click(object sender, RoutedEventArgs e) => ExportTicket("jpg");

        // Padanan html2canvas: render visual ticket card ke bitmap 2x resolusi lalu simpan.
        private void ExportTicket(string format)
        {
            var target = TicketCard;
            if (target.ActualWidth < 1 || target.ActualHeight < 1) return;

            try
            {
                const double scale = 2.0; // high-res, mirip scale:2 di html2canvas
                var dpi = 96 * scale;
                var width = (int)Math.Ceiling(target.ActualWidth * scale);
                var height = (int)Math.Ceiling(target.ActualHeight * scale);

                var rtb = new RenderTargetBitmap(width, height, dpi, dpi, PixelFormats.Pbgra32);

                // Gambar background slate-900 dulu (biar sudut membulat gak transparan), lalu visual-nya.
                var visual = new DrawingVisual();
                using (var ctx = visual.RenderOpen())
                {
                    var area = new Rect(0, 0, target.ActualWidth, target.ActualHeight);
                    ctx.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A)), null, area);
                    ctx.DrawRectangle(new VisualBrush(target), null, area);
                }
                rtb.Render(visual);

                var isJpg = format == "jpg";
                BitmapEncoder encoder = isJpg
                    ? new JpegBitmapEncoder { QualityLevel = 95 }
                    : new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rtb));

                var code = (DataContext as BookingConfirmationViewModel)?.BookingCode ?? "ticket";
                var dialog = new SaveFileDialog
                {
                    FileName = $"ticket-{code}.{(isJpg ? "jpg" : "png")}",
                    Filter = isJpg ? "JPEG Image|*.jpg" : "PNG Image|*.png"
                };

                if (dialog.ShowDialog() == true)
                {
                    using var stream = File.Create(dialog.FileName);
                    encoder.Save(stream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan tiket: {ex.Message}", "Download Error");
            }
        }
    }
}
