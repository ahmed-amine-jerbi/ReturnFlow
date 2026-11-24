# start-all.ps1
# Starts ReturnFlow without Docker for the app itself: database container, AI service, API and web app.
# Put this file in the folder that contains returnflow-api, returnflow-ai and returnflow-web, then run:
#   .\start-all.ps1            -> normal development mode  (http://localhost:5173)
#   .\start-all.ps1 -Share     -> production build + Cloudflare tunnel (installable on your phone)
#
# If PowerShell blocks the script:
#   powershell -ExecutionPolicy Bypass -File .\start-all.ps1

param([switch]$Share)

$root = $PSScriptRoot

function Open-Window([string]$folder, [string]$command) {
    Start-Process powershell -ArgumentList '-NoExit', '-Command', "cd '$root\$folder'; $command"
}

Write-Host "Starting the database (Docker Desktop must be running)..."
docker compose -f "$root\returnflow-api\docker-compose.yml" up -d db
if ($LASTEXITCODE -ne 0) {
    Write-Host "Could not start the database. Open Docker Desktop, wait until it says it is running, then try again." -ForegroundColor Red
    exit 1
}

Open-Window 'returnflow-ai'  '.\.venv\Scripts\python.exe -m uvicorn app.main:app --port 8000'
Open-Window 'returnflow-api' 'dotnet run --project src/ReturnFlow.Api'

if ($Share) {
    Open-Window 'returnflow-web' 'npm run build; npm run preview'
    Start-Sleep -Seconds 12   # give the web server time to start before the tunnel connects to it
    Open-Window 'returnflow-web' 'cloudflared tunnel --url http://localhost:4173'
    Write-Host "Look in the tunnel window for the https://...trycloudflare.com address and open it on your phone."
} else {
    Open-Window 'returnflow-web' 'npm run dev'
    Write-Host "Open http://localhost:5173 once the API window says it is listening."
}
