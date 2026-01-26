# ============================================================
# CREATE NEW MODULE
# ============================================================
# Creates a new module folder from template with work order
#
# Usage: .\create-module.ps1 -Name "auth" -Title "Authentication Module"
# ============================================================

param(
    [Parameter(Mandatory=$true)]
    [string]$Name,
    
    [string]$Title = "",
    [string]$Description = "",
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot)
)

$modulesPath = Join-Path $ProjectRoot "modules"
$templatePath = Join-Path $modulesPath "_template"
$newModulePath = Join-Path $modulesPath $Name

# Validation
if (Test-Path $newModulePath) {
    Write-Host "[ERROR] Module already exists: $Name" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $templatePath)) {
    Write-Host "[ERROR] Template not found: $templatePath" -ForegroundColor Red
    exit 1
}

# Create module
Write-Host ""
Write-Host "[CREATE] Creating module: $Name" -ForegroundColor Cyan

Copy-Item -Path $templatePath -Destination $newModulePath -Recurse

# Update readme.md if title/description provided
if ($Title -or $Description) {
    $readmePath = Join-Path $newModulePath "readme.md"
    
    if (Test-Path $readmePath) {
        $content = Get-Content $readmePath -Raw -Encoding UTF8
        
        if ($Title) {
            $content = $content -replace "# Module: \[Module Name\]", "# Module: $Title"
            $content = $content -replace "\[Module Name\]", $Title
        }
        
        if ($Description) {
            $content = $content -replace "\[Mo ta ngan gon ve module\]", $Description
        }
        
        Set-Content -Path $readmePath -Value $content -Encoding UTF8
    }
}

# Update status.md with current timestamp
$statusPath = Join-Path $newModulePath "status.md"
if (Test-Path $statusPath) {
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
    $content = Get-Content $statusPath -Raw -Encoding UTF8
    $content = $content -replace "\[timestamp\]", $timestamp
    $content = $content -replace "\[Module Name\]", $(if ($Title) { $Title } else { $Name })
    Set-Content -Path $statusPath -Value $content -Encoding UTF8
}

Write-Host "[OK] Module created: $newModulePath" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Edit readme.md with work order details" -ForegroundColor White
Write-Host "  2. Open module folder in separate IDE for Worker agent" -ForegroundColor White
Write-Host "  3. Tell Worker: 'Doc readme.md va bat dau'" -ForegroundColor White
Write-Host ""
