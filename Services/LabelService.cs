using System;
using System.Text;
using TicketeraApp.Models;

namespace TicketeraApp.Services
{
    public class LabelService
    {
        public string Generate3ColumnEan13Command(
            string productName, string sku, string price,
            FieldSettings nameSettings,
            FieldSettings barcodeSettings,
            FieldSettings priceSettings,
            int spacingX,
            int globalOffsetX = 0,
            int firstColumnXOffset = 0,
            int quantity = 1)
        {
            var sb = new StringBuilder();

            sb.AppendLine("SIZE 105 mm, 22 mm");
            sb.AppendLine("GAP 3 mm, 0 mm");
            sb.AppendLine("DIRECTION 1");
            sb.AppendLine("CLS");

            bool hasName = !string.IsNullOrEmpty(productName);
            bool hasPrice = !string.IsNullOrEmpty(price);

            int eanStart = barcodeSettings.Y;
            int eanHeight;

            if (barcodeSettings.Height > 0)
            {
                eanHeight = barcodeSettings.Height;
            }
            else if (hasPrice)
            {
                eanHeight = Math.Max(20, priceSettings.Y - eanStart - 10);
            }
            else
            {
                eanHeight = Math.Max(20, 170 - eanStart);
            }

            for (int col = 0; col < 3; col++)
            {
                int col1Extra = (col == 0) ? firstColumnXOffset : 0;
                int baseX = globalOffsetX + col1Extra + (col * spacingX);

                if (hasName)
                {
                    sb.AppendLine($"TEXT {baseX + nameSettings.X}, {nameSettings.Y}, " +
                                  $"\"{nameSettings.FontType}\", 0, {nameSettings.FontSize}, {nameSettings.FontSize}, \"{productName}\"");
                }

                sb.AppendLine($"BARCODE {baseX + barcodeSettings.X}, {eanStart}, " +
                              $"\"EAN13\", {eanHeight}, 1, 0, 2, 2, \"{sku}\"");

                if (hasPrice)
                {
                    sb.AppendLine($"TEXT {baseX + priceSettings.X}, {priceSettings.Y}, " +
                                  $"\"{priceSettings.FontType}\", 0, {priceSettings.FontSize}, {priceSettings.FontSize}, \"{price}\"");
                }
            }

            sb.AppendLine($"PRINT {quantity},1");
            return sb.ToString();
        }

        public string GenerateCombinedRowCommand(
            string[] productNames, string[] skus, string[] prices,
            FieldSettings nameSettings,
            FieldSettings barcodeSettings,
            FieldSettings priceSettings,
            int spacingX,
            int globalOffsetX = 0,
            int firstColumnXOffset = 0)
        {
            var sb = new StringBuilder();

            sb.AppendLine("SIZE 105 mm, 22 mm");
            sb.AppendLine("GAP 3 mm, 0 mm");
            sb.AppendLine("DIRECTION 1");
            sb.AppendLine("CLS");

            for (int col = 0; col < 3; col++)
            {
                if (col >= skus.Length || string.IsNullOrWhiteSpace(skus[col]))
                    continue;

                string productName = productNames.Length > col ? productNames[col] : "";
                string sku = skus[col];
                string price = prices.Length > col ? prices[col] : "";

                bool hasName = !string.IsNullOrEmpty(productName);
                bool hasPrice = !string.IsNullOrEmpty(price);

                int eanStart = barcodeSettings.Y;
                int eanHeight;

                if (barcodeSettings.Height > 0)
                {
                    eanHeight = barcodeSettings.Height;
                }
                else if (hasPrice)
                {
                    eanHeight = Math.Max(20, priceSettings.Y - eanStart - 10);
                }
                else
                {
                    eanHeight = Math.Max(20, 170 - eanStart);
                }

                int col1Extra = (col == 0) ? firstColumnXOffset : 0;
                int baseX = globalOffsetX + col1Extra + (col * spacingX);

                if (hasName)
                {
                    sb.AppendLine($"TEXT {baseX + nameSettings.X}, {nameSettings.Y}, " +
                                  $"\"{nameSettings.FontType}\", 0, {nameSettings.FontSize}, {nameSettings.FontSize}, \"{productName}\"");
                }

                sb.AppendLine($"BARCODE {baseX + barcodeSettings.X}, {eanStart}, " +
                              $"\"EAN13\", {eanHeight}, 1, 0, 2, 2, \"{sku}\"");

                if (hasPrice)
                {
                    sb.AppendLine($"TEXT {baseX + priceSettings.X}, {priceSettings.Y}, " +
                                  $"\"{priceSettings.FontType}\", 0, {priceSettings.FontSize}, {priceSettings.FontSize}, \"{price}\"");
                }
            }

            sb.AppendLine("PRINT 1,1");
            return sb.ToString();
        }
    }
}
