@echo off
cd /d "%~dp0"
echo Preparando Goalball Sonoro. Puede tardar un minuto...
powershell -NoProfile -Command "& { $ErrorActionPreference='Stop'; $target=Join-Path (Get-Location) 'Goalball-Windows.zip'; $parts=@(Get-ChildItem 'Goalball.zip.part-*' | Sort-Object Name); if($parts.Count -ne 6){throw 'Faltan partes. Extrae todo el ZIP descargado.'}; $output=[IO.File]::Create($target); try { foreach($part in $parts){$input=[IO.File]::OpenRead($part.FullName); try {$input.CopyTo($output)} finally {$input.Dispose()} } } finally {$output.Dispose()}; if((Get-FileHash $target -Algorithm SHA256).Hash -ne '9823D27F6B7EE27823AA2F3B0DB746E5CF55D22C49551CA6B91F06ACCF5D6AA2'){throw 'El archivo no coincide con su checksum. Descarga de nuevo.'}; Expand-Archive -LiteralPath $target -DestinationPath . -Force; Start-Process (Join-Path (Get-Location) 'GoalballSonoro-win32-x64\GoalballSonoro.exe') }"
if errorlevel 1 pause
