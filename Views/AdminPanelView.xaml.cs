using CineCore.Services;
using CineCore.ViewModels;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace CineCore.Views
{
    public partial class AdminPanelView : UserControl
    {
        public AdminPanelView()
        {
            InitializeComponent();
            DataContext = new AdminPanelViewModel();
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not AdminPanelViewModel vm || vm.Report is null)
            {
                MessageBox.Show("Report belum siap. Buka tab Sales & Report dulu.", "Export PDF");
                return;
            }

            try
            {
                // Langsung download ke folder Downloads user (tanpa SaveFileDialog).
                var downloads = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                Directory.CreateDirectory(downloads); // aman kalau sudah ada
                var path = Path.Combine(downloads,
                    $"cinecore_sales_report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

                ReportPdfService.Generate(vm.Report, path);
                vm.IsPreviewVisible = false; // tutup preview overlay
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); // buka file
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal membuat PDF: {ex.Message}", "Export PDF Error");
            }
        }
    }
}
