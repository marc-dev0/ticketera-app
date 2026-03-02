using System;

namespace TicketeraApp.Models
{
    public class BarcodeRecord
    {
        public string Code { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
    }
}
