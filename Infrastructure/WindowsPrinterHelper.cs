using System;
using System.Runtime.InteropServices;

namespace TicketeraApp.Infrastructure
{
    public static class WindowsPrinterHelper
    {
        // P/Invoke declarations for winspool.drv
        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int Level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public class DOCINFOA
        {
            [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
            [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
            [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
        }

        /// <summary>
        /// Envía una cadena de texto (como comandos ZPL o TSPL) directamente a la impresora (RAW bypass).
        /// </summary>
        public static bool SendStringToPrinter(string printerName, string text)
        {
            if (string.IsNullOrEmpty(printerName))
                throw new ArgumentNullException(nameof(printerName));
            if (string.IsNullOrEmpty(text))
                throw new ArgumentNullException(nameof(text));

            IntPtr pBytes = IntPtr.Zero;
            try
            {
                // Convierte el string a puntero ANSI en memoria no administrada (unmanaged).
                // Muchas impresoras ZPL/TSPL de 8-bits prefieren ANSI o UTF8.
                pBytes = Marshal.StringToCoTaskMemAnsi(text);
                int dwCount = text.Length;

                return SendBytesToPrinter(printerName, pBytes, dwCount);
            }
            finally
            {
                if (pBytes != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(pBytes);
                }
            }
        }

        private static bool SendBytesToPrinter(string szPrinterName, IntPtr pBytes, int dwCount)
        {
            IntPtr hPrinter = IntPtr.Zero;
            var di = new DOCINFOA
            {
                pDocName = "Ticketera Raw Document",
                pDataType = "RAW" // Fundamental para hacer bypass al driver de gráficos
            };
            bool bSuccess = false;

            if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
            {
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    if (StartPagePrinter(hPrinter))
                    {
                        bSuccess = WritePrinter(hPrinter, pBytes, dwCount, out int dwWritten);
                        EndPagePrinter(hPrinter);
                    }
                    EndDocPrinter(hPrinter);
                }
                ClosePrinter(hPrinter);
            }
            // Si bSuccess es false, Marshal.GetLastWin32Error() daría la razón
            if (!bSuccess)
            {
                int error = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(error);
            }
            return bSuccess;
        }
    }
}
