param([Parameter(Mandatory=$true)][string]$OutputDirectory)
$ErrorActionPreference = 'Stop'
New-Item -ItemType Directory -Force $OutputDirectory | Out-Null
$sources = @(
    @{ Name='jingle'; Author='alexyquest42'; Id='630469'; Title='Jingle Ball' },
    @{ Name='rolling'; Author='luminadii'; Id='343446'; Title='Ball rolls and hits net 5.WAV' }
)
$credits = @('Grabaciones descargadas de Freesound, vistas previas públicas de alta calidad.', 'Licencia CC BY 4.0: https://creativecommons.org/licenses/by/4.0/', 'Adaptaciones: conversión a WAV PCM 44,1 kHz; normalización, recorte y fundido para el juego.', '')
foreach ($source in $sources) {
    $pageUrl = "https://freesound.org/people/$($source.Author)/sounds/$($source.Id)/"
    $page = (Invoke-WebRequest -Uri $pageUrl -UserAgent "GoalballSonoro/3.0 (https://github.com/arielfmeza91/goallball-game)").Content
    if ($page -notmatch 'creativecommons\.org/licenses/by/4\.0/' -or $page -notmatch $source.Id) { throw "No se confirmó la licencia CC BY 4.0 de $pageUrl" }
    $previews = @([regex]::Matches($page, 'https://cdn\.freesound\.org/previews/[^"'' <>]+-hq\.mp3') | ForEach-Object { $_.Value } | Select-Object -Unique)
    if ($previews.Count -ne 1) { throw "No se encontró una única vista previa HQ: $pageUrl" }
    $path = Join-Path $OutputDirectory ($source.Name + '.mp3')
    Invoke-WebRequest -Uri $previews[0] -OutFile $path
    $hash = (Get-FileHash $path -Algorithm SHA256).Hash
    $credits += "$($source.Title) — $($source.Author)"
    $credits += "Fuente y licencia: $pageUrl"
    $credits += "Audio público HQ: $($previews[0])"
    $credits += "SHA256 del MP3: $hash"
    $credits += ''
}
$credits | Set-Content -Encoding utf8 (Join-Path $OutputDirectory 'GRABACIONES-CREDITOS.txt')
