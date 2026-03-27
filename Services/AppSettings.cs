using TicketeraApp.Models;

namespace TicketeraApp.Services
{
    public class AppSettings
    {
        public FieldSettings NameSettings { get; set; } = new FieldSettings();
        public FieldSettings BarcodeSettings { get; set; } = new FieldSettings();
        public FieldSettings PriceSettings { get; set; } = new FieldSettings();
        public int SpacingX { get; set; } = 280;
        public int GlobalOffsetX { get; set; } = 0;
        public int FirstColumnXOffset { get; set; } = 10;
        public bool IncludeProductName { get; set; } = true;
        public bool IncludePrice { get; set; } = true;

        public static AppSettings CreateDefaults() => new AppSettings
        {
            NameSettings    = new FieldSettings { Label = "Nombre del Producto", X = 50,  Y = 0,   Height = 0,   FontType = "1", FontSize = 1 },
            BarcodeSettings = new FieldSettings { Label = "Código EAN-13",       X = 50,  Y = 25,  Height = 105, FontType = "1", FontSize = 1 },
            PriceSettings   = new FieldSettings { Label = "Precio",              X = 80,  Y = 145, Height = 0,   FontType = "1", FontSize = 1 },
            SpacingX           = 280,
            GlobalOffsetX      = 0,
            FirstColumnXOffset = 10,
            IncludeProductName = true,
            IncludePrice       = true
        };
    }
}
