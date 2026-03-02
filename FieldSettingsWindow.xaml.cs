using System;
using System.Windows;
using TicketeraApp.Models;

namespace TicketeraApp
{
    public partial class FieldSettingsWindow : Window
    {
        public FieldSettings Result { get; private set; }
        private readonly bool _isBarcode;

        public FieldSettingsWindow(FieldSettings current, bool isBarcode = false)
        {
            InitializeComponent();
            _isBarcode = isBarcode;

            Result = new FieldSettings
            {
                Label = current.Label,
                X = current.X,
                Y = current.Y,
                Height = current.Height,
                FontScale = current.FontScale
            };

            Title = $"Configurar: {current.Label}";
            TitleBlock.Text = $"⚙ Configuración de: {current.Label}";
            XTextBox.Text = current.X.ToString();
            YTextBox.Text = current.Y.ToString();
            HeightTextBox.Text = current.Height.ToString();

            // Ocultar "Tamaño de Fuente" para el código de barras (no aplica)
            FontScalePanel.Visibility = isBarcode ? Visibility.Collapsed : Visibility.Visible;

            if (!isBarcode)
            {
                foreach (System.Windows.Controls.ComboBoxItem item in FontScaleComboBox.Items)
                {
                    if (item.Tag?.ToString() == current.FontScale.ToString())
                    {
                        FontScaleComboBox.SelectedItem = item;
                        break;
                    }
                }
                if (FontScaleComboBox.SelectedItem == null)
                    FontScaleComboBox.SelectedIndex = 0;
            }

            // Ajustar etiqueta del campo Height para barcode vs texto
            // La etiqueta ya es genérica pero podemos hacerla más clara
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(XTextBox.Text, out int x)) x = 0;
            if (!int.TryParse(YTextBox.Text, out int y)) y = 0;
            if (!int.TryParse(HeightTextBox.Text, out int height)) height = 0;

            int fontScale = 1;
            if (!_isBarcode
                && FontScaleComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem selected
                && int.TryParse(selected.Tag?.ToString(), out int parsed))
            {
                fontScale = parsed;
            }

            Result.X = x;
            Result.Y = y;
            Result.Height = height;
            Result.FontScale = fontScale;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
