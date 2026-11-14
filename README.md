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

Anular una impresion
--------------------
El boton "Anular impresion" (atajo F8) esta siempre disponible y no cierra la aplicacion. Sirve cuando se manda una cantidad por error, por ejemplo 500 copias.

Como funciona: la aplicacion envia un solo trabajo con PRINT N,1. Windows lo termina en un segundo y las N copias quedan guardadas dentro de la impresora, asi que no basta con cancelar la cola de Windows. Al pulsar el boton la aplicacion:
1. Vacia la cola de impresion de Windows.
2. Envia a la impresora el comando inmediato TSPL ESC !R (reset), que vacia las etiquetas pendientes.
3. Espera unos segundos y envia ESC !C (reinicio) como respaldo.
4. Vacia otra vez la cola de Windows.

Importante: la aplicacion solo sabe que envio los comandos, no puede confirmar que la impresora los obedecio. Si la impresora sigue imprimiendo, apagarla y revisar el log.

Registro de eventos (logs)
--------------------------
La aplicacion guarda un archivo por dia con las impresiones, los pasos de la anulacion, los errores de Windows y los errores no controlados. Se conservan 14 dias.
- Carpeta: %AppData%\TicketeraApp\logs (archivo ticketera-AAAAMMDD.log).
- Se abre desde el enlace "Logs" al pie de la ventana.
- Si algo falla, pedir al usuario ese archivo.

Solucion de problemas
---------------------
La aplicacion dice "enviado" pero no sale nada:
- Revisar la linea "Imprimir: ... cola: estado=..." del log. Si dice Paused u Offline, Windows retiene los trabajos y la impresora nunca los recibe.
- Desde la version 1.4.9 la aplicacion detecta este caso antes de imprimir, intenta reanudar la cola y, si no puede, avisa y ofrece abrir la cola de Windows.
- Manual: Configuracion, Impresoras y escaneres, abrir la cola de la impresora, menu Impresora, quitar "Pausar impresion" y "Usar impresora sin conexion".
- Apagar y encender la impresora y revisar el cable USB.

Impresora de la que se tiene evidencia: HPRT HT300 por USB (se vende tambien como CBX). Los comandos de anulacion salen del manual TSPL/TSPL2 de TSC y no estan confirmados en este modelo.

Publicar una nueva version
--------------------------
1. Subir el numero de version en TicketeraApp.csproj (Version, AssemblyVersion y FileVersion), ticketera_setup.iss (AppVersion) y update.xml (version y url del instalador). Agregar la seccion nueva al inicio de changelog.html.
2. Publicar la aplicacion autocontenida en un solo archivo, desde la carpeta del proyecto:
   dotnet publish TicketeraApp.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o bin\installer_source
   (borrar antes la carpeta bin\installer_source)
3. Compilar el instalador con Inno Setup: ISCC.exe ticketera_setup.iss. Genera bin\installer_output\ProfitCode_Setup_vX.exe.
4. Commit en la rama master.
5. En la rama gh-pages agregar el instalador, update.xml y changelog.html, y hacer commit.
6. Subir ambas ramas (git push origin master y git push origin gh-pages). La rama gh-pages es la que hace que las PCs instaladas reciban la actualizacion: al abrirse, la aplicacion lee https://marc-dev0.github.io/ticketera-app/update.xml. GitHub Pages tarda entre 1 y 3 minutos en publicar.

Regla para el codigo: los comentarios llevan solo letras, sin iconos ni simbolos decorativos.
