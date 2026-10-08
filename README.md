# Goalball Sonoro

Juego local de goalball para Windows 10/11 x64, con sonido espacial, narración
española y rival automático. Lee `LEEME.txt` para controles, reglas implementadas
y limitaciones. No es una simulación completa ni certificada del reglamento IBSA.

## Desarrollo

No hay dependencias en el motor: `npm test` ejecuta sus pruebas con Node 24.
Para probar en navegador, desde este directorio:

```sh
python -m http.server 8765 --bind 127.0.0.1
```

El servidor es para uso local de desarrollo. Los cinco archivos de audio están
incluidos y tienen licencia CC0 (véase `SONIDOS-LICENCIA.txt`).

## Generar Windows desde Linux

Las herramientas se instalan fuera del checkout. No requieren modificar la
aplicación ni descargar de nuevo sus sonidos.

```sh
npm --cache /tmp/goalball-npm-cache install --prefix /workspace/goalball-build @electron/packager@19.0.1 playwright@1.58.2
XDG_CACHE_HOME=/tmp/goalball-cache ELECTRON_CACHE=/tmp/goalball-electron-cache /workspace/goalball-build/node_modules/.bin/electron-packager /workspace/goallball-game GoalballSonoro --platform=win32 --arch=x64 --electron-version=40.10.6 --out=/workspace/goalball-build/dist --overwrite --asar
```

Distribuye la carpeta entera `GoalballSonoro-win32-x64`, no solo el EXE.
Electron verifica los checksums de sus descargas; no desactives esa verificación.
Conserva las licencias incluidas por Electron. El paquete no está firmado.
Pruebas del motor y Chromium realizadas en Linux; falta prueba en Windows real.
