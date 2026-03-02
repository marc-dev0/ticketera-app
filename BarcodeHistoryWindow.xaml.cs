using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using TicketeraApp.Models;
using TicketeraApp.Services;

namespace TicketeraApp
{
    public partial class BarcodeHistoryWindow : Window
    {
        private readonly BarcodeRegistryService _svc;

        public BarcodeHistoryWindow(BarcodeRegistryService svc)
        {
            InitializeComponent();
            _svc = svc;
            Refresh();
        }

        private void Refresh()
        {
            var records = _svc.Load();
            HistoryGrid.ItemsSource = records.OrderByDescending(r => r.RegisteredAt).ToList();
            int count = records.Count;
            string last = records.OrderByDescending(r => r.RegisteredAt).FirstOrDefault()?.Code ?? "—";
            SubtitleBlock.Text = $"{count} código(s) registrado(s). Último: {last}";
            StatusBlock.Text = "";
        }

        // ── Delete single row ────────────────────────────────────────────────
        private void DeleteRowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Tag is string code)
            {
                var result = MessageBox.Show(
                    $"¿Eliminar el código {code} del historial?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var list = _svc.Load();
                    var toRemove = list.FirstOrDefault(r => r.Code == code);
                    if (toRemove != null)
                    {
                        list.Remove(toRemove);
                        _svc.Save(list);
                        Refresh();
                        StatusBlock.Text = $"Código {code} eliminado.";
                        StatusBlock.Foreground = System.Windows.Media.Brushes.DarkOrange;
                    }
                }
            }
        }

        // ── Export ───────────────────────────────────────────────────────────
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SaveFileDialog
            {
                Title = "Exportar historial de códigos",
                Filter = "Excel (*.xlsx)|*.xlsx",
                FileName = $"codigos_{DateTime.Now:yyyy-MM-dd_HHmmss}"
            };

            if (dlg.ShowDialog() == true)
            {
                _svc.ExportExcel(dlg.FileName);
                StatusBlock.Text = $"✔ Exportado: {System.IO.Path.GetFileName(dlg.FileName)}";
                StatusBlock.Foreground = System.Windows.Media.Brushes.SeaGreen;
            }
        }

        // ── Clear all ────────────────────────────────────────────────────────
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Estás seguro de que quieres eliminar TODOS los registros?\nEsta acción no se puede deshacer.",
                "Confirmar limpieza",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _svc.Save(new System.Collections.Generic.List<BarcodeRecord>());
                Refresh();
                StatusBlock.Text = "Historial limpiado.";
                StatusBlock.Foreground = System.Windows.Media.Brushes.DarkOrange;
            }
        }

        // ── Close ────────────────────────────────────────────────────────────
        private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
        }
    }
}
