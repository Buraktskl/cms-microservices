# Run this script once to generate a dev HTTPS certificate for local development.
# After running, restart containers with: docker compose up --build -d

$certDir = Join-Path $PSScriptRoot "certs"
$certPath = Join-Path $certDir "aspnetapp.pfx"
$certPassword = "CaseStudy123!"

if (-not (Test-Path $certDir)) {
    New-Item -ItemType Directory -Path $certDir | Out-Null
}

if (Test-Path $certPath) {
    Write-Host "Certificate already exists at $certPath. Skipping generation." -ForegroundColor Yellow
} else {
    Write-Host "Generating HTTPS development certificate..." -ForegroundColor Cyan
    dotnet dev-certs https -ep $certPath -p $certPassword
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Certificate generation failed."
        exit 1
    }
    Write-Host "Certificate generated successfully." -ForegroundColor Green
}

Write-Host "Trusting the certificate on this machine..." -ForegroundColor Cyan
dotnet dev-certs https --trust
Write-Host "Done. Run 'docker compose up --build -d' to apply." -ForegroundColor Green
