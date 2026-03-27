namespace TicketeraApp.Models
{
    /// <summary>
    /// Almacena la configuración de posición y estilo de un campo de la etiqueta.
    /// </summary>
    public class FieldSettings
    {
        public string Label { get; set; } = string.Empty;

        /// <summary>Margen horizontal (Eje X) relativo al inicio de la columna.</summary>
        public int X { get; set; }

        /// <summary>Margen vertical (Eje Y) desde el borde superior de la etiqueta.</summary>
        public int Y { get; set; }

        /// <summary>Altura del elemento en dots. En 0, se calcula automáticamente.</summary>
        public int Height { get; set; } = 0;

        /// <summary>Tipo de fuente TSPL: "1", "2", "3", "4", "5", "8", "ROMAN.TTF", etc.</summary>
        public string FontType { get; set; } = "1";

        /// <summary>Multiplicador de tamaño (escala) de la fuente (1 a 10).</summary>
        public int FontSize { get; set; } = 1;
    }
}
