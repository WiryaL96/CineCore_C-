using CineCore.Services;
using CineCore.ViewModels;
using Microsoft.Win32;
using System;
using System.Diagnostics;
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

            var dialog = new SaveFileDialog
            {
                FileName = "cinecore_sales_report.pdf",
                Filter = "PDF File|*.pdf"
            };
            if (dialog.ShowDialog() != true) return;

            try
            {
                ReportPdfService.Generate(vm.Report, dialog.FileName);
                if (MessageBox.Show("PDF berhasil dibuat. Buka sekarang?", "Export PDF",
                        MessageBoxButton.YesNo, MessageBoxImage.Information) == MessageBoxResult.Yes)
                    Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal membuat PDF: {ex.Message}", "Export PDF Error");
            }
        }
    }
}
