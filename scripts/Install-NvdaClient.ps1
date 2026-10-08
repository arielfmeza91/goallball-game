param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
# Solo distribución oficial de NV Access. No se usa una DLL de terceros.
$base = 'https://download.nvaccess.org/releases/2026.2/'
$uri = [Uri]($base + 'nvda_2026.2_controllerClient.zip')
if ($uri.Scheme -ne 'https' -or $uri.Host -ne 'download.nvaccess.org') { throw 'Destino de descarga inesperado.' }
$work = Join-Path $env:RUNNER_TEMP 'goalball-nvda-client'
New-Item -ItemType Directory -Force $work | Out-Null
$archive = Join-Path $work 'nvda-controller-original.zip'
Invoke-WebRequest -Uri $uri.AbsoluteUri -OutFile $archive
Expand-Archive -LiteralPath $archive -DestinationPath (Join-Path $work 'extracted') -Force
$candidates = @(Get-ChildItem (Join-Path $work 'extracted') -Recurse -File | Where-Object { $_.Name -match '^nvdaControllerClient(64)?\.dll$' -and ($_.FullName -match '[\\/]x64[\\/]' -or $_.Name -match '64') })
if ($candidates.Count -ne 1) { throw 'No se encontró una única DLL x64 oficial de NVDA.' }
Copy-Item $candidates[0].FullName (Join-Path $OutputDirectory 'nvdaControllerClient.dll')
Copy-Item native/NVDA-LGPL-2.1.txt (Join-Path $OutputDirectory 'NVDA-LGPL-2.1.txt')
Copy-Item native/NVDA-CREDITOS.txt (Join-Path $OutputDirectory 'NVDA-CREDITOS.txt')
$hash = (Get-FileHash $candidates[0].FullName -Algorithm SHA256).Hash
"NVDA Controller Client 2026.2, binario sin modificar; nombre adaptado para P/Invoke.`nOrigen: $($uri.AbsoluteUri)`nSHA256: $hash" | Set-Content (Join-Path $OutputDirectory 'NVDA-ORIGEN.txt')
