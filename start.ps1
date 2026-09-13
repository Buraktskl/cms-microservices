#Requires -Version 5.1
<#
.SYNOPSIS
    CMS Mikroservisler projesini sıfırdan başlatır.
    İlk kez çalıştırıyorsanız bu script tüm ön gereksinimleri otomatik karşılar.

.DESCRIPTION
    1. .env dosyası yoksa .env.example'dan oluşturur
    2. HTTPS sertifikası (certs/aspnetapp.pfx) yoksa oluşturur ve güvenilir yapar
    3. Docker Compose ile tüm servisleri build edip başlatır

.EXAMPLE
    .\start.ps1
    .\start.ps1 -Detach       # Arka planda çalıştır
    .\start.ps1 -Build        # Her seferinde image'ları rebuild et
    .\start.ps1 -Stop         # Tüm servisleri durdur
    .\start.ps1 -Clean        # Servisleri durdur + volume'ları sil
#>

param(
    [switch]$Detach,
    [switch]$Build,
    [switch]$Stop,
    [switch]$Clean
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ROOT = $PSScriptRoot
$ENV_FILE = Join-Path $ROOT ".env"
$ENV_EXAMPLE = Join-Path $ROOT ".env.example"
$CERT_DIR = Join-Path $ROOT "certs"
$CERT_PATH = Join-Path $CERT_DIR "aspnetapp.pfx"
$CERT_PASSWORD = "CaseStudy123!"

function Write-Step([string]$msg) {
    Write-Host "`n>>> $msg" -ForegroundColor Cyan
}

function Write-OK([string]$msg) {
    Write-Host "    [OK] $msg" -ForegroundColor Green
}

function Write-Warn([string]$msg) {
    Write-Host "    [!!] $msg" -ForegroundColor Yellow
}

# ── Dur / Temizle ─────────────────────────────────────────────────────────────

if ($Stop) {
    Write-Step "Servisler durduruluyor..."
    docker compose -f (Join-Path $ROOT "docker-compose.yml") down
    Write-OK "Tüm servisler durduruldu."
    exit 0
}

if ($Clean) {
    Write-Step "Servisler durduruluyor ve volume'lar siliniyor..."
    docker compose -f (Join-Path $ROOT "docker-compose.yml") down -v --remove-orphans
    Write-OK "Temizleme tamamlandı (veritabanı verileri silindi)."
    exit 0
}

# ── Docker kontrolü ───────────────────────────────────────────────────────────

Write-Step "Docker durumu kontrol ediliyor..."
try {
    $null = docker info 2>&1
    Write-OK "Docker çalışıyor."
} catch {
    Write-Host "`n[HATA] Docker Desktop çalışmıyor veya erişilemiyor." -ForegroundColor Red
    Write-Host "       Docker Desktop'ı başlatın ve tekrar deneyin." -ForegroundColor Red
    exit 1
}

# ── .env dosyası ──────────────────────────────────────────────────────────────

Write-Step ".env dosyası kontrol ediliyor..."
if (-not (Test-Path $ENV_FILE)) {
    if (Test-Path $ENV_EXAMPLE) {
        Copy-Item $ENV_EXAMPLE $ENV_FILE
        Write-OK ".env dosyası .env.example'dan oluşturuldu."
        Write-Warn "Üretim ortamı için .env içindeki şifreleri değiştirin."
    } else {
        Write-Host "`n[HATA] Ne .env ne de .env.example bulunamadı!" -ForegroundColor Red
        exit 1
    }
} else {
    Write-OK ".env dosyası mevcut."
}

# ── HTTPS Sertifikası ─────────────────────────────────────────────────────────

Write-Step "HTTPS sertifikası kontrol ediliyor..."
if (-not (Test-Path $CERT_PATH)) {
    Write-Warn "Sertifika bulunamadı. Oluşturuluyor..."

    if (-not (Test-Path $CERT_DIR)) {
        New-Item -ItemType Directory -Path $CERT_DIR | Out-Null
    }

    # .NET SDK kontrolü
    try {
        $null = dotnet --version 2>&1
    } catch {
        Write-Host "`n[HATA] .NET SDK bulunamadı. https://dot.net adresinden yükleyin." -ForegroundColor Red
        exit 1
    }

    dotnet dev-certs https -ep $CERT_PATH -p $CERT_PASSWORD
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n[HATA] Sertifika oluşturulamadı." -ForegroundColor Red
        exit 1
    }

    Write-OK "Sertifika oluşturuldu: $CERT_PATH"
    Write-Step "Sertifika güvenilir yapılıyor (sistem izni gerekebilir)..."
    dotnet dev-certs https --trust
    Write-OK "Sertifika güvenilir olarak işaretlendi."
} else {
    Write-OK "Sertifika mevcut: $CERT_PATH"
}

# ── Docker Compose ────────────────────────────────────────────────────────────

Write-Step "Servisler başlatılıyor..."

$composeArgs = @(
    "compose",
    "-f", (Join-Path $ROOT "docker-compose.yml"),
    "up"
)

if ($Build) { $composeArgs += "--build" }
if ($Detach) { $composeArgs += "-d" }

& docker @composeArgs

if ($Detach -and $LASTEXITCODE -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  Tüm servisler arka planda çalışıyor  " -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "  User Service Swagger  : https://localhost:5003/swagger" -ForegroundColor White
    Write-Host "  Content Service Swagger: https://localhost:5004/swagger" -ForegroundColor White
    Write-Host "  Kong Gateway          : http://localhost:8000" -ForegroundColor White
    Write-Host "    GET  http://localhost:8000/users" -ForegroundColor DarkGray
    Write-Host "    GET  http://localhost:8000/contents" -ForegroundColor DarkGray
    Write-Host "  RabbitMQ Yönetim      : http://localhost:15673  (guest/guest)" -ForegroundColor White
    Write-Host "  Jaeger İzleme         : http://localhost:16686" -ForegroundColor White
    Write-Host "  Kibana                : http://localhost:5601" -ForegroundColor White
    Write-Host "  Portainer             : http://localhost:9000" -ForegroundColor White
    Write-Host ""
    Write-Host "  Durdurmak için: .\start.ps1 -Stop" -ForegroundColor DarkGray
    Write-Host "  Temizlemek için: .\start.ps1 -Clean" -ForegroundColor DarkGray
    Write-Host ""
}
