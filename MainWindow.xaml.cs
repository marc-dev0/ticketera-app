using System;
using System.Linq;
using System.Printing;
using System.Windows;
using TicketeraApp.Models;
using TicketeraApp.Services;
using TicketeraApp.Infrastructure;
using AutoUpdaterDotNET;

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
        private bool _isReprinting = false;  // evita pedir guardar si el código ya existe

        public MainWindow()
        {
            InitializeComponent();
            _labelService    = new LabelService();
            _settingsService = new SettingsService();
            _registryService = new BarcodeRegistryService();

            LoadSettings();
            LoadPrinters();

            // Configurar y buscar actualizaciones en segundo plano usando GitHub Pages
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.ShowRemindLaterButton = true;
            AutoUpdater.Start("https://marc-dev0.github.io/ticketera-app/update.xml");
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

        // ── Combined Row CheckBox ────────────────────────────────────────────
        private void CombinedRowCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (CombinedRowPanel != null)
            {
                bool isChecked = CombinedRowCheckBox.IsChecked == true;
                CombinedRowPanel.Visibility = isChecked ? Visibility.Visible : Visibility.Collapsed;

                if (!isChecked)
                {
                    Col2SkuTextBox.Text = string.Empty;
                    Col2NameTextBox.Text = string.Empty;
                    Col2PriceTextBox.Text = string.Empty;

                    Col3SkuTextBox.Text = string.Empty;
                    Col3NameTextBox.Text = string.Empty;
                    Col3PriceTextBox.Text = string.Empty;
                }
            }
        }

        private void LoadProductDataFromHistory(string sku, System.Windows.Controls.TextBox nameBox, System.Windows.Controls.TextBox priceBox)
        {
            if (string.IsNullOrWhiteSpace(sku) || sku.Length < 12) return;
            
            var records = _registryService.Load();
            var match = records.FirstOrDefault(r => r.Code == sku) 
                     ?? records.FirstOrDefault(r => r.Code.StartsWith(sku));
            
            if (match != null)
            {
                nameBox.Text = match.ProductName ?? "";
                priceBox.Text = match.Price ?? "";
            }
        }

        private void Col2SkuTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LoadProductDataFromHistory(Col2SkuTextBox.Text.Trim(), Col2NameTextBox, Col2PriceTextBox);
        }

        private void Col3SkuTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            LoadProductDataFromHistory(Col3SkuTextBox.Text.Trim(), Col3NameTextBox, Col3PriceTextBox);
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
                if (string.IsNullOrEmpty(SkuTextBox.Text) || SkuTextBox.Text.Length > 12) 
                    SkuTextBox.Text = "200000005000";
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var win = new BarcodeHistoryWindow(_registryService) { Owner = this };
            win.ShowDialog();

            if (win.SelectedForReprint != null)
            {
                var record = win.SelectedForReprint;

                // Cargar el código sin desbloquear el campo (solo escritura programática)
                SkuTextBox.Text = record.Code.Length == 13 ? record.Code.Substring(0, 12) : record.Code;

                if (!string.IsNullOrEmpty(record.ProductName))
                {
                    EnableProductNameCheckBox.IsChecked = true;
                    ProductNameTextBox.IsEnabled = true;
                    ProductNameTextBox.Text = record.ProductName;
                }

                if (!string.IsNullOrEmpty(record.Price))
                {
                    EnablePriceCheckBox.IsChecked = true;
                    PriceTextBox.IsEnabled = true;
                    PriceTextBox.Text = record.Price;
                }

                _isReprinting = true;
                NewCodeButton.Visibility = Visibility.Visible;
                StatusTextBlock.Text = $"Código {record.Code} cargado. Ajusta la cantidad y presiona Imprimir.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.SteelBlue;
                // Diferir el foco hasta que la ventana del historial haya cerrado por completo
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PrintQuantityTextBox.Focus();
                    PrintQuantityTextBox.SelectAll();
                }), System.Windows.Threading.DispatcherPriority.Input);
            }
            else
            {
                NewCodeButton.Visibility = Visibility.Collapsed;
                RefreshCodeEntryState();
            }
        }

        private void NewCodeButton_Click(object sender, RoutedEventArgs e)
        {
            // Salir del modo reimpresión y volver al flujo normal
            _isReprinting = false;
            NewCodeButton.Visibility = Visibility.Collapsed;
            ProductNameTextBox.Text = string.Empty;
            PriceTextBox.Text = string.Empty;
            PrintQuantityTextBox.Text = "1";
            StatusTextBlock.Text = string.Empty;
            RefreshCodeEntryState();
            ProductNameTextBox.Focus();
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

            int quantity = 1;
            if (!int.TryParse(PrintQuantityTextBox.Text, out quantity) || quantity < 1)
            {
                quantity = 1;
                PrintQuantityTextBox.Text = "1";
            }

            SaveSettings();

            try
            {
                StatusTextBlock.Text = "Generando comando...";
                string command = "";
                bool isCombined = CombinedRowCheckBox.IsChecked == true;

                if (isCombined)
                {
                    // Validate secondary columns
                    string c2Sku = Col2SkuTextBox.Text.Trim();
                    string c3Sku = Col3SkuTextBox.Text.Trim();
                    
                    if (!string.IsNullOrEmpty(c2Sku) && !IsValidEAN13(c2Sku))
                    {
                        MessageBox.Show("El código de la columna 2 es inválido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    if (!string.IsNullOrEmpty(c3Sku) && !IsValidEAN13(c3Sku))
                    {
                        MessageBox.Show("El código de la columna 3 es inválido.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    if (quantity > 1)
                    {
                        command += _labelService.Generate3ColumnEan13Command(
                            productName, sku, price,
                            _nameSettings, _barcodeSettings, _priceSettings,
                            spacingX, globalOffsetX, firstColOffset, quantity - 1);
                    }

                    string[] names = new[] { productName, Col2NameTextBox.Text.Trim(), Col3NameTextBox.Text.Trim() };
                    string[] skus = new[] { sku, c2Sku, c3Sku };
                    string[] prices = new[] { price, Col2PriceTextBox.Text.Trim(), Col3PriceTextBox.Text.Trim() };

                    command += _labelService.GenerateCombinedRowCommand(
                        names, skus, prices,
                        _nameSettings, _barcodeSettings, _priceSettings,
                        spacingX, globalOffsetX, firstColOffset);
                }
                else
                {
                    command = _labelService.Generate3ColumnEan13Command(
                        productName, sku, price,
                        _nameSettings, _barcodeSettings, _priceSettings,
                        spacingX, globalOffsetX, firstColOffset, quantity);
                }

                StatusTextBlock.Text = "Enviando a la impresora...";
                bool ok = WindowsPrinterHelper.SendStringToPrinter(printerName, command);

                if (ok)
                {
                    StatusTextBlock.Text = $"¡Etiquetas enviadas a {printerName}!";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.SeaGreen;

                    if (_isReprinting)
                    {
                        // Reimpresión completada: mantener el código cargado para reimprimir de nuevo si se desea.
                        // El usuario puede presionar Imprimir otra vez, o abrir el Historial y cerrar para volver al modo normal.
                        StatusTextBlock.Text = "✔ Reimpresión enviada. Imprime de nuevo o abre el Historial para crear un código nuevo.";
                        StatusTextBlock.Foreground = System.Windows.Media.Brushes.SeaGreen;
                        PrintQuantityTextBox.Focus();
                        PrintQuantityTextBox.SelectAll();
                    }
                    else
                    {
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