using System;
using System.Printing;
using System.Runtime.InteropServices;
using TicketeraApp.Services;

namespace TicketeraApp.Infrastructure
{
    public static class WindowsPrinterHelper
    {
        // P/Invoke declarations for winspool.drv
        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartDocPrinter(IntPtr hPrinter, int Level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Ansi, ExactSpelling = false, SetLastError = true, CallingConvention = CallingConvention.StdCall)]
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

        /// <summary>
        /// Comandos inmediatos TSPL (empiezan con ESC, se ejecutan aunque la impresora esté imprimiendo).
        /// <ESC>!R = reset (vacía el buffer de etiquetas pendientes); <ESC>!C = reinicio (firmware V5.23+).
        /// </summary>
        public const string CmdReset = "\u001b!R";
        public const string CmdRestart = "\u001b!C";

        /// <summary>Resumen del estado de la cola de Windows para el log.</summary>
        public static string DescribeQueue(string printerName)
        {
            try
            {
                using var queue = new LocalPrintServer().GetPrintQueue(printerName);
                queue.Refresh();
                return $"estado={queue.QueueStatus}, trabajos={queue.NumberOfJobs}, puerto={queue.QueuePort?.Name}, driver={queue.QueueDriver?.Name}";
            }
            catch (Exception ex)
            {
                return $"(no se pudo leer la cola: {ex.Message})";
            }
        }

        /// <summary>
        /// Abre la cola con permiso de administración (necesario para Purge/Resume). Si Windows lo
        /// niega, cae al acceso básico, que alcanza para leer estado y cancelar los propios trabajos.
        /// </summary>
        private static PrintQueue OpenQueue(string printerName, bool administrate)
        {
            var server = new LocalPrintServer();
            if (administrate)
            {
                try { return new PrintQueue(server, printerName, PrintSystemDesiredAccess.AdministratePrinter); }
                catch (Exception ex) { LogService.Warn($"Sin permiso de administrar '{printerName}': {ex.Message.Trim()}"); }
            }
            return server.GetPrintQueue(printerName);
        }

        /// <summary>
        /// Una cola pausada u offline en Windows retiene los trabajos y la impresora nunca los recibe.
        /// Devuelve null si está lista, o el motivo si está bloqueada.
        /// </summary>
        public static string? GetBlockReason(string printerName)
        {
            try
            {
                using var queue = OpenQueue(printerName, false);
                queue.Refresh();
                if (queue.IsOffline) return "offline";
                if (queue.IsPaused) return "paused";
            }
            catch (Exception ex)
            {
                LogService.Warn($"No se pudo leer el estado de la cola: {ex.Message.Trim()}");
            }
            return null;
        }

        /// <summary>Intenta reanudar una cola pausada. Devuelve true si quedó funcionando.</summary>
        public static bool TryResumeQueue(string printerName)
        {
            try
            {
                using var queue = OpenQueue(printerName, true);
                queue.Refresh();
                if (!queue.IsPaused) return true;
                queue.Resume();
                queue.Refresh();
                LogService.Info($"Cola '{printerName}' reanudada: IsPaused={queue.IsPaused}");
                return !queue.IsPaused;
            }
            catch (Exception ex)
            {
                LogService.Warn($"No se pudo reanudar la cola '{printerName}': {ex.Message.Trim()}");
                return false;
            }
        }

        /// <summary>Abre la ventana de la cola de impresión de Windows para que el usuario la revise.</summary>
        public static void OpenQueueWindow(string printerName)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                    "rundll32.exe", $"printui.dll,PrintUIEntry /o /n\"{printerName}\"") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                LogService.Warn($"No se pudo abrir la cola de impresión: {ex.Message}");
            }
        }

        /// <summary>Borra todos los trabajos pendientes de la cola de Windows. Devuelve cuántos había.</summary>
        public static int PurgeSpooler(string printerName)
        {
            using var queue = OpenQueue(printerName, true);
            queue.Refresh();
            int pending = queue.NumberOfJobs;
            try
            {
                queue.Purge();
            }
            catch (PrintQueueException ex)
            {
                LogService.Warn($"Purge denegado ({ex.Message.Trim()}). Cancelando trabajos uno por uno.");
                int cancelled = 0;
                foreach (PrintSystemJobInfo job in queue.GetPrintJobInfoCollection())
                {
                    try { job.Cancel(); cancelled++; }
                    catch (Exception jex) { LogService.Warn($"No se pudo cancelar el trabajo {job.JobIdentifier} '{job.Name}': {jex.Message}"); }
                }
                LogService.Info($"Trabajos cancelados individualmente: {cancelled} de {pending}.");
            }
            return pending;
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
            string failedStep = "OpenPrinter";
            int error = 0;

            if (OpenPrinter(szPrinterName.Normalize(), out hPrinter, IntPtr.Zero))
            {
                failedStep = "StartDocPrinter";
                if (StartDocPrinter(hPrinter, 1, di))
                {
                    failedStep = "StartPagePrinter";
                    if (StartPagePrinter(hPrinter))
                    {
                        failedStep = "WritePrinter";
                        bSuccess = WritePrinter(hPrinter, pBytes, dwCount, out int dwWritten);
                        if (!bSuccess) error = Marshal.GetLastWin32Error();
                        else if (dwWritten != dwCount)
                            LogService.Warn($"WritePrinter escribió {dwWritten} de {dwCount} bytes en '{szPrinterName}'");
                        EndPagePrinter(hPrinter);
                    }
                    else error = Marshal.GetLastWin32Error();
                    EndDocPrinter(hPrinter);
                }
                else error = Marshal.GetLastWin32Error();
                ClosePrinter(hPrinter);
            }
            else error = Marshal.GetLastWin32Error();

            if (!bSuccess)
            {
                LogService.Error($"Fallo {failedStep} en '{szPrinterName}' (Win32 error {error})");
                throw new System.ComponentModel.Win32Exception(error, $"{failedStep} falló (código {error})");
            }
            return bSuccess;
        }
    }
}
