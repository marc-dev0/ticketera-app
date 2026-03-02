Ticketera EAN-13
================

Aplicacion de escritorio construida para facilitar la impresion masiva de etiquetas de codigos de barras en formato EAN-13, pensada especialmente para trabajar con impresoras termicas como la CBX T30 mediante comandos directo al spooler de Windows.

Desarrollador: Miguel Rojas
Ano: 2026

Contexto del Proyecto
--------------------
Esta herramienta nacio de la necesidad de tener un control estricto sobre la secuencia de codigos de barras internos. Genera codigos de 13 digitos asegurando la integridad del digito verificador de manera automatica. 

Lo mas importante de la aplicacion es su capacidad de bloquear el ingreso manual despues de usar el primer codigo, forzando a que los consecuentes numeros se autogeneren, previniendo errores humanos de duplicidad o saltos en la secuencia. Todo el historial se guarda y puede ser exportado facilmente a Excel para compartir con otras areas.

Detalles Tecnicos
-----------------
Construido usando:
- .NET 10
- Interfaz WPF
- Libreria ClosedXML para los reportes de Excel

Instalacion
-----------
La aplicacion fue disenada para funcionar de manera autocontenida (portable). Esto significa que el ejecutable generado pesa un poco mas porque incluye todo el runtime de .NET 10 en su interior, lo cual permite que corra en cualquier PC con Windows 10/11 sin pedirle al usuario final descargar dependencias extra de internet.
