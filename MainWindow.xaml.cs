using System;
using System.Linq;
using System.Printing;
using System.Windows;
using TicketeraApp.Models;
using TicketeraApp.Services;
using TicketeraApp.Infrastructure;

namespace TicketeraApp
{
    public partial class MainWindow : Window
    {
        private readonly LabelService _labelService;
        private readonly SettingsService _settingsService;
        private readonly BarcodeRegistryService _registryService;

        private FieldSettings _nameSettings;
        private FieldSettings _barcodeSettings;
        private FieldSettings _priceSettings;

        public MainWindow()
        {
            InitializeComponent();
            _labelService    = new LabelService();
            _settingsService = new SettingsService();
            _registryService = new BarcodeRegistryService();

            LoadSettings();
            LoadPrinters();
            RefreshCodeEntryState();
        }

        private void LoadSettings()
        {
            var settings = _settingsService.Load();

            _nameSettings    = settings.NameSettings;
            _barcodeSettings = settings.BarcodeSettings;
            _priceSettings   = settings.PriceSettings;

            SpacingTextBox.Text            = settings.SpacingX.ToString();
            GlobalOffsetXTextBox.Text      = settings.GlobalOffsetX.ToString();
            FirstColumnOffsetTextBox.Text  = settings.FirstColumnXOffset.ToString();

            // Restaurar estado de checkboxes
            EnableProductNameCheckBox.IsChecked = settings.IncludeProductName;
            EnablePriceCheckBox.IsChecked       = settings.IncludePrice;
            ProductNameTextBox.IsEnabled        = settings.IncludeProductName;
            PriceTextBox.IsEnabled              = settings.IncludePrice;
        }

        private void SaveSettings()
        {
            if (!int.TryParse(SpacingTextBox.Text, out int spacingX)) spacingX = 280;
            if (!int.TryParse(GlobalOffsetXTextBox.Text, out int globalOffsetX)) globalOffsetX = 0;
            if (!int.TryParse(FirstColumnOffsetTextBox.Text, out int firstColOffset)) firstColOffset = 20;

            _settingsService.Save(new AppSettings
            {
                NameSettings          = _nameSettings,
                BarcodeSettings       = _barcodeSettings,
                PriceSettings         = _priceSettings,
                SpacingX              = spacingX,
                GlobalOffsetX         = globalOffsetX,
                FirstColumnXOffset    = firstColOffset,
                IncludeProductName    = EnableProductNameCheckBox.IsChecked ?? true,
                IncludePrice          = EnablePriceCheckBox.IsChecked ?? true
            });
        }

        private void LoadPrinters()
        {
            try
            {
                var printServer = new LocalPrintServer();
                foreach (var queue in printServer.GetPrintQueues())
                    PrinterComboBox.Items.Add(queue.FullName);

                if (PrinterComboBox.Items.Count > 0)
                {
                    var def = LocalPrintServer.GetDefaultPrintQueue();
                    PrinterComboBox.SelectedItem = PrinterComboBox.Items.Contains(def.FullName)
                        ? def.FullName
                        : PrinterComboBox.Items[0];
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error cargando impresoras: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── Checkbox handlers ────────────────────────────────────────────────
        private void EnableProductNameCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (ProductNameTextBox != null)
            {
                ProductNameTextBox.IsEnabled = EnableProductNameCheckBox.IsChecked ?? false;
                SaveSettings();
            }
        }

        private void EnablePriceCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (PriceTextBox != null)
            {
                PriceTextBox.IsEnabled = EnablePriceCheckBox.IsChecked ?? false;
                SaveSettings();
            }
        }

        // ── Config button handlers ───────────────────────────────────────────
        private void ConfigNameButton_Click(object sender, RoutedEventArgs e)
        {
            if (OpenFieldDialog(ref _nameSettings, isBarcode: false))
                SaveSettings();
        }

        private void ConfigBarcodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (OpenFieldDialog(ref _barcodeSettings, isBarcode: true))
                SaveSettings();
        }

        private void ConfigPriceButton_Click(object sender, RoutedEventArgs e)
        {
            if (OpenFieldDialog(ref _priceSettings, isBarcode: false))
                SaveSettings();
        }

        /// <returns>true si el usuario hizo clic en Aceptar</returns>
        private bool OpenFieldDialog(ref FieldSettings settings, bool isBarcode)
        {
            var dialog = new FieldSettingsWindow(settings, isBarcode) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                settings = dialog.Result;
                return true;
            }
            return false;
        }

        // ── EAN-13 live check digit ───────────────────────────────────────────
        private void SkuTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Guarda: el evento se dispara durante InitializeComponent() antes de que los controles existan
            if (CheckDigitHintBlock == null || EanValidationWarning == null) return;

            string sku = SkuTextBox.Text.Trim();
            EanValidationWarning.Visibility = Visibility.Collapsed;
            CheckDigitHintBlock.Foreground = System.Windows.Media.Brushes.SteelBlue;

            if (sku.Length == 12 && sku.All(char.IsDigit))
            {
                int check = CalculateEan13CheckDigit(sku);
                CheckDigitHintBlock.Text = $"✔ Dígito verificador: {check}  →  Código completo: {sku}{check}";
                CheckDigitHintBlock.Visibility = Visibility.Visible;
            }
            else if (sku.Length == 13 && sku.All(char.IsDigit))
            {
                int expected = CalculateEan13CheckDigit(sku.Substring(0, 12));
                bool valid = (sku[12] - '0') == expected;
                CheckDigitHintBlock.Text = valid
                    ? "✔ Código EAN-13 válido"
                    : $"⚠ Verificador incorrecto (debería ser {expected})";
                CheckDigitHintBlock.Foreground = valid
                    ? System.Windows.Media.Brushes.SeaGreen
                    : System.Windows.Media.Brushes.DarkOrange;
                CheckDigitHintBlock.Visibility = Visibility.Visible;
            }
            else
            {
                CheckDigitHintBlock.Visibility = Visibility.Collapsed;
            }
        }

        private static int CalculateEan13CheckDigit(string twelveDigits)
        {
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int d = twelveDigits[i] - '0';
                sum += (i % 2 == 0) ? d : d * 3;
            }
            return (10 - (sum % 10)) % 10;
        }

        // ── Validation ───────────────────────────────────────────────────────
        private bool IsValidEAN13(string sku)
        {
            if (string.IsNullOrWhiteSpace(sku)) return false;
            if (sku.Length != 12 && sku.Length != 13) return false;
            return sku.All(char.IsDigit);
        }

        // ── Registry / History ───────────────────────────────────────────────
        private void RefreshCodeEntryState()
        {
            var existing = _registryService.Load();
            if (existing.Any())
            {
                // Bloquear input manual y preparar autogenerado (12 dígitos base)
                SkuTextBox.IsReadOnly = true;
                SkuTextBox.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240));
                
                string lastCode = existing.OrderByDescending(r => r.RegisteredAt).First().Code;
                string prefix = lastCode.Length >= 2 ? lastCode.Substring(0, 2) : "20";
                string nextCode13 = _registryService.GetNextCode(prefix);
                SkuTextBox.Text = nextCode13.Substring(0, 12);
            }
            else
            {
                // Desbloquear para el primer registro manual
                SkuTextBox.IsReadOnly = false;
                SkuTextBox.Background = System.Windows.Media.Brushes.White;
                if (SkuTextBox.Text.Length > 12) SkuTextBox.Text = string.Empty;
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new BarcodeHistoryWindow(_registryService) { Owner = this };
            win.ShowDialog();
            RefreshCodeEntryState();
        }

        // ── Print ────────────────────────────────────────────────────────────
        private void PrintButton_Click(object sender, RoutedEventArgs e)
        {
            EanValidationWarning.Visibility = Visibility.Collapsed;

            if (PrinterComboBox.SelectedItem == null)
            {
                MessageBox.Show("Por favor, selecciona una impresora válida.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string printerName = PrinterComboBox.SelectedItem.ToString() ?? "";
            string productName = (EnableProductNameCheckBox.IsChecked ?? false) ? ProductNameTextBox.Text.Trim() : string.Empty;
            string sku = SkuTextBox.Text.Trim();
            string price = (EnablePriceCheckBox.IsChecked ?? false) ? PriceTextBox.Text.Trim() : string.Empty;

            if ((EnableProductNameCheckBox.IsChecked ?? false) && string.IsNullOrEmpty(productName))
            {
                MessageBox.Show("Por favor, ingrese el nombre del producto.", "Campo Obligatorio", MessageBoxButton.OK, MessageBoxImage.Warning);
                ProductNameTextBox.Focus();
                return;
            }

            if (!IsValidEAN13(sku))
            {
                EanValidationWarning.Text = "⚠ El código EAN-13 debe tener exactamente 12 o 13 dígitos numéricos.";
                EanValidationWarning.Visibility = Visibility.Visible;
                return;
            }

            if (!int.TryParse(SpacingTextBox.Text, out int spacingX)) spacingX = 280;
            if (!int.TryParse(GlobalOffsetXTextBox.Text, out int globalOffsetX)) globalOffsetX = 0;
            if (!int.TryParse(FirstColumnOffsetTextBox.Text, out int firstColOffset)) firstColOffset = 20;

            SaveSettings();

            try
            {
                StatusTextBlock.Text = "Generando comando...";
                string command = _labelService.Generate3ColumnEan13Command(
                    productName, sku, price,
                    _nameSettings, _barcodeSettings, _priceSettings,
                    spacingX, globalOffsetX, firstColOffset);

                StatusTextBlock.Text = "Enviando a la impresora...";
                bool ok = WindowsPrinterHelper.SendStringToPrinter(printerName, command);

                if (ok)
                {
                    StatusTextBlock.Text = $"¡Etiquetas enviadas a {printerName}!";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.SeaGreen;

                    // Asegurar código de 13 dígitos para registro
                    string codeToSave = sku;
                    if (sku.Length == 12)
                        codeToSave = sku + CalculateEan13CheckDigit(sku);

                    // Confirmar almacenamiento en el historial
                    var save = MessageBox.Show(
                        $"¿Guardar el código {codeToSave} en el historial?\n\nProducto: {(string.IsNullOrEmpty(productName) ? "—" : productName)}\nPrecio: {(string.IsNullOrEmpty(price) ? "—" : price)}",
                        "Guardar en historial",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (save == MessageBoxResult.Yes)
                    {
                        var existing = _registryService.Load();
                        if (existing.Any(r => r.Code == codeToSave))
                        {
                            MessageBox.Show($"El código {codeToSave} ya está registrado en el historial.",
                                "Duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                        else
                        {
                            // Registrar y generar autoincremento para la próxima etiqueta
                            _registryService.Add(new BarcodeRecord
                            {
                                Code         = codeToSave,
                                ProductName  = productName,
                                Price        = price,
                                RegisteredAt = DateTime.Now
                            });

                            // Restablecer campos de entrada opcionales
                            if (EnableProductNameCheckBox.IsChecked ?? false)
                                ProductNameTextBox.Text = string.Empty;
                            if (EnablePriceCheckBox.IsChecked ?? false)
                                PriceTextBox.Text = string.Empty;

                            ProductNameTextBox.Focus();
                            RefreshCodeEntryState();
                            StatusTextBlock.Text = $"✔ Guardado. Código siguiente: {SkuTextBox.Text}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error: {ex.Message}";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show(ex.Message, "Error de Impresión", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}