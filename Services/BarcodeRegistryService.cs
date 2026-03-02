using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TicketeraApp.Models;

namespace TicketeraApp.Services
{
    public class BarcodeRegistryService
    {
        private static readonly string FilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                         "TicketeraApp", "barcode_registry.json");

        private static readonly JsonSerializerOptions _opts = new JsonSerializerOptions { WriteIndented = true };

        public List<BarcodeRecord> Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<List<BarcodeRecord>>(json, _opts) ?? new List<BarcodeRecord>();
                }
            }
            catch { /* silencioso */ }
            return new List<BarcodeRecord>();
        }

        public void Save(List<BarcodeRecord> records)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(records, _opts));
            }
            catch { }
        }

        public void Add(BarcodeRecord record)
        {
            var list = Load();
            list.Add(record);
            Save(list);
        }

        /// <summary>
        /// Devuelve el siguiente código secuencial dado un prefijo (ej. "20").
        /// Si no hay registros con ese prefijo, devuelve prefix + "0000000000000".pad
        /// </summary>
        public string GetNextCode(string prefix)
        {
            var list = Load();
            long maxNum = 0;
            bool found = false;

            foreach (var r in list)
            {
                // trabajar con los 12 dígitos base (sin verificador) o con los 13
                string code = r.Code.Length == 13 ? r.Code.Substring(0, 12) : r.Code;
                if (code.StartsWith(prefix) && long.TryParse(code, out long num))
                {
                    if (num > maxNum) { maxNum = num; found = true; }
                }
            }

            if (!found)
            {
                // Primer código: rellenar con ceros hasta 12 dígitos
                string start = prefix.PadRight(12, '0');
                return AddCheckDigit(start);
            }

            string next = (maxNum + 1).ToString().PadLeft(12, '0');
            return AddCheckDigit(next);
        }

        private static string AddCheckDigit(string twelve)
        {
            if (twelve.Length != 12) return twelve;
            int sum = 0;
            for (int i = 0; i < 12; i++)
                sum += (twelve[i] - '0') * (i % 2 == 0 ? 1 : 3);
            int check = (10 - (sum % 10)) % 10;
            return twelve + check;
        }

        public void ExportExcel(string filePath)
        {
            var list = Load();
            using var wb = new ClosedXML.Excel.XLWorkbook();
            var ws = wb.Worksheets.Add("Códigos EAN-13");

            // Headers
            ws.Cell(1, 1).Value = "Código EAN-13";
            ws.Cell(1, 2).Value = "Nombre del Producto";
            ws.Cell(1, 3).Value = "Precio";
            ws.Cell(1, 4).Value = "Fecha de Registro";

            // Style headers
            var header = ws.Range(1, 1, 1, 4);
            header.Style.Font.Bold = true;
            header.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#1A5276");
            header.Style.Font.FontColor = ClosedXML.Excel.XLColor.White;

            // Data rows
            for (int i = 0; i < list.Count; i++)
            {
                var r = list[i];
                int row = i + 2;

                // Código: forzar como texto para evitar notación científica
                ws.Cell(row, 1).SetValue(r.Code);
                ws.Cell(row, 1).Style.NumberFormat.Format = "@";

                ws.Cell(row, 2).Value = r.ProductName;
                ws.Cell(row, 3).Value = r.Price;
                ws.Cell(row, 4).Value = r.RegisteredAt.ToString("yyyy-MM-dd HH:mm:ss");
            }

            // Auto-fit todas las columnas al contenido
            ws.Columns().AdjustToContents();

            // Columna código: mínimo 18 caracteres de ancho
            if (ws.Column(1).Width < 18) ws.Column(1).Width = 18;

            wb.SaveAs(filePath);
        }
    }
}
