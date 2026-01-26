# ============================================================
# LIVE DASHBOARD VIEWER
# ============================================================
# Displays a live-updating dashboard of all module statuses
#
# Usage: .\dashboard-live.ps1
# Stop:  Ctrl+C
# ============================================================

param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [int]$RefreshSeconds = 2
)

function Get-StatusIcon {
    param([string]$Status)
    switch ($Status) {
        "COMPLETED"    { return "[OK]" }
        "IN_PROGRESS"  { return "[..]" }
        "PENDING"      { return "[  ]" }
        "BLOCKED"      { return "[!!]" }
        "NEEDS_REVIEW" { return "[??]" }
        default        { return "[--]" }
    }
}

function Show-Dashboard {
    $modulesPath = Join-Path $ProjectRoot "modules"
    
    if (-not (Test-Path $modulesPath)) {
        Write-Host "Modules folder not found!" -ForegroundColor Red
        return
    }
    
    Clear-Host
    $timestamp = Get-Date -Format "HH:mm:ss"
    
    Write-Host ""
    Write-Host "  ================================================================" -ForegroundColor Cyan
    Write-Host "                MULTI-AGENT STATUS DASHBOARD                      " -ForegroundColor Cyan
    Write-Host "  ================================================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  Last Update: $timestamp                   Refresh: ${RefreshSeconds}s" -ForegroundColor White
    Write-Host ""
    
    $modules = Get-ChildItem -Path $modulesPath -Directory | Where-Object { $_.Name -ne "_template" }
    
    if ($modules.Count -eq 0) {
        Write-Host "  No modules found. Create modules in: $modulesPath" -ForegroundColor Yellow
        return
    }
    
    # Table header
    Write-Host "  +------------------------+-----------------+------------------------+" -ForegroundColor DarkGray
    Write-Host "  | Module                 | Status          | Progress               |" -ForegroundColor DarkGray
    Write-Host "  +------------------------+-----------------+------------------------+" -ForegroundColor DarkGray
    
    $stats = @{ Completed = 0; InProgress = 0; Pending = 0; Blocked = 0; Review = 0 }
    
    foreach ($module in $modules | Sort-Object Name) {
        $statusFile = Join-Path $module.FullName "status.md"
        $status = "UNKNOWN"
        $progress = 0
        
        if (Test-Path $statusFile) {
            $content = Get-Content $statusFile -Raw -ErrorAction SilentlyContinue
            
            if ($content -match "Status[:\s]*\*{0,2}(COMPLETED|DONE|FINISHED)\*{0,2}") {
                $status = "COMPLETED"; $progress = 100; $stats.Completed++
            }
            elseif ($content -match "Status[:\s]*\*{0,2}(IN[_\s]?PROGRESS|WORKING|IMPLEMENTING)\*{0,2}") {
                $status = "IN_PROGRESS"
                if ($content -match "(\d{1,3})%") { $progress = [int]$Matches[1] } else { $progress = 50 }
                $stats.InProgress++
            }
            elseif ($content -match "Status[:\s]*\*{0,2}(PENDING|TODO|NOT[_\s]?STARTED)\*{0,2}") {
                $status = "PENDING"; $progress = 0; $stats.Pending++
            }
            elseif ($content -match "Status[:\s]*\*{0,2}(BLOCKED|ERROR|FAILED)\*{0,2}") {
                $status = "BLOCKED"; $progress = -1; $stats.Blocked++
            }
            elseif ($content -match "Status[:\s]*\*{0,2}(REVIEW|NEEDS[_\s]?REVIEW)\*{0,2}") {
                $status = "NEEDS_REVIEW"; $progress = 100; $stats.Review++
            }
        }
        
        $icon = Get-StatusIcon $status
        $moduleName = $module.Name.PadRight(20).Substring(0, 20)
        $statusText = "$icon $status".PadRight(15).Substring(0, 15)
        
        $progressBar = if ($progress -ge 0) {
            $filled = [math]::Floor($progress / 10)
            $empty = 10 - $filled
            "[" + ("#" * $filled) + ("-" * $empty) + "] " + "$progress%".PadLeft(4)
        } else {
            "      N/A           "
        }
        
        $statusColor = switch ($status) {
            "COMPLETED"    { "Green" }
            "IN_PROGRESS"  { "Yellow" }
            "PENDING"      { "Gray" }
            "BLOCKED"      { "Red" }
            "NEEDS_REVIEW" { "Magenta" }
            default        { "White" }
        }
        
        Write-Host "  | " -NoNewline -ForegroundColor DarkGray
        Write-Host "$moduleName" -NoNewline -ForegroundColor White
        Write-Host " | " -NoNewline -ForegroundColor DarkGray
        Write-Host "$statusText" -NoNewline -ForegroundColor $statusColor
        Write-Host " | " -NoNewline -ForegroundColor DarkGray
        Write-Host "$progressBar" -NoNewline -ForegroundColor $statusColor
        Write-Host " |" -ForegroundColor DarkGray
    }
    
    Write-Host "  +------------------------+-----------------+------------------------+" -ForegroundColor DarkGray
    Write-Host ""
    
    # Summary
    $total = $modules.Count
    Write-Host "  Summary: " -NoNewline -ForegroundColor White
    Write-Host "[OK] $($stats.Completed) " -NoNewline -ForegroundColor Green
    Write-Host "[..] $($stats.InProgress) " -NoNewline -ForegroundColor Yellow
    Write-Host "[  ] $($stats.Pending) " -NoNewline -ForegroundColor Gray
    Write-Host "[??] $($stats.Review) " -NoNewline -ForegroundColor Magenta
    Write-Host "[!!] $($stats.Blocked) " -NoNewline -ForegroundColor Red
    Write-Host "/ $total total" -ForegroundColor White
    Write-Host ""
    Write-Host "  Press Ctrl+C to exit" -ForegroundColor DarkGray
}

# Main loop
try {
    while ($true) {
        Show-Dashboard
        Start-Sleep -Seconds $RefreshSeconds
    }
}
finally {
    Write-Host ""
    Write-Host "  Dashboard closed." -ForegroundColor Yellow
}
