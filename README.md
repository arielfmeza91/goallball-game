# Goalball Sonoro — Windows nativo + NVDA

Aplicación C# WinForms (.NET 10). Menú nativo y pista con GDI+; no hay HTML,
WebView ni Electron. Audio estéreo con NAudio y WAV locales. Anuncios de
partido y braille mediante NVDA Controller Client oficial, sin otro TTS.
Consulta `LEEME.txt` para controles y alcance de las reglas.

## Pruebas y compilación

SDK fijado en `global.json` a 10.0.100. Usa el checkout existente; no hace falta
crear otro worktree. Desde la raíz:

```sh
dotnet run --project native/Goalball.Tests/Goalball.Tests.csproj -c Release
dotnet restore native/Goalball.Windows/Goalball.Windows.csproj -r win-x64 --locked-mode
dotnet publish native/Goalball.Windows/Goalball.Windows.csproj -c Release -r win-x64 --self-contained true --no-restore -o dist/GoalballSonoro-Nativo
```

La compilación cruzada funciona en Linux. La interfaz WinForms y NVDA solo
se ejecutan en Windows. Los paquetes NuGet se fijan con packages.lock.json.
Para Windows, conserva toda la carpeta publicada y copia LEEME.txt, LICENSE
y SONIDOS-LICENCIA.txt. Incluye el cliente oficial usando
`scripts/Install-NvdaClient.ps1` con OutputDirectory apuntando al publicado
(y RUNNER_TEMP a una carpeta temporal). Ese script usa exclusivamente la
distribución NVDA 2026.2 de NV Access, conserva LGPL y registra origen y hash.
`GoalballSonoro.exe --verificar` carga la DLL, comprueba sus exportaciones,
valida los WAV y escribe verificacion.txt. No exige NVDA abierto en CI.

## Distribución

Las etiquetas v2* activan la compilación, las pruebas en Windows, el diagnóstico
del paquete y la publicación de Goalball-Nativo-Windows.zip en GitHub Releases.
El cliente oficial se incorpora durante esa compilación, nunca desde mirrors.
La fuente correspondiente de NVDA se enlaza en NVDA-CREDITOS.txt.
No cambies ni desactives verificación de TLS durante la descarga.

El juego se inicia con GoalballSonoro.exe y NVDA abierto. F2 comprueba la
conexión. La partida se controla desde el teclado; Escape abre el menú nativo.
Las pruebas de CI no sustituyen una prueba interactiva con NVDA real.
