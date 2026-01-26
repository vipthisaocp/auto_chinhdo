# ============================================================
# QUICK STATUS CHECK
# ============================================================
# One-time status check of all modules (no loop)
#
# Usage: .\check-status.ps1
# ============================================================

param(
    [string]$ProjectRoot = (Split-Path -Parent $PSScriptRoot),
    [switch]$Json = $false
)

$modulesPath = Join-Path $ProjectRoot "modules"

if (-not (Test-Path $modulesPath)) {
    if ($Json) {
        Write-Output '{"error": "Modules folder not found"}'
    } else {
        Write-Host "Modules folder not found: $modulesPath" -ForegroundColor Red
    }
    exit 1
}

$results = @()
$modules = Get-ChildItem -Path $modulesPath -Directory | Where-Object { $_.Name -ne "_template" }

foreach ($module in $modules) {
    $statusFile = Join-Path $module.FullName "status.md"
    $status = "UNKNOWN"
    $progress = 0
    $lastModified = $null
    
    if (Test-Path $statusFile) {
        $fileInfo = Get-Item $statusFile
        $lastModified = $fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
        $content = Get-Content $statusFile -Raw -ErrorAction SilentlyContinue
        
        if ($content -match "Status[:\s]*\*{0,2}(COMPLETED|DONE|FINISHED)\*{0,2}") {
            $status = "COMPLETED"; $progress = 100
        }
        elseif ($content -match "Status[:\s]*\*{0,2}(IN[_\s]?PROGRESS|WORKING|IMPLEMENTING)\*{0,2}") {
            $status = "IN_PROGRESS"
            if ($content -match "(\d{1,3})%") { $progress = [int]$Matches[1] } else { $progress = 50 }
        }
        elseif ($content -match "Status[:\s]*\*{0,2}(PENDING|TODO|NOT[_\s]?STARTED)\*{0,2}") {
            $status = "PENDING"; $progress = 0
        }
        elseif ($content -match "Status[:\s]*\*{0,2}(BLOCKED|ERROR|FAILED)\*{0,2}") {
            $status = "BLOCKED"; $progress = -1
        }
        elseif ($content -match "Status[:\s]*\*{0,2}(REVIEW|NEEDS[_\s]?REVIEW)\*{0,2}") {
            $status = "NEEDS_REVIEW"; $progress = 100
        }
    }
    
    $results += [PSCustomObject]@{
        Module       = $module.Name
        Status       = $status
        Progress     = $progress
        LastModified = $lastModified
    }
}

if ($Json) {
    $results | ConvertTo-Json
} else {
    Write-Host ""
    Write-Host "  [STATUS CHECK] Module Status" -ForegroundColor Cyan
    Write-Host "  ----------------------------" -ForegroundColor DarkGray
    Write-Host ""
    
    foreach ($r in $results | Sort-Object Module) {
        $icon = switch ($r.Status) {
            "COMPLETED"    { "[OK]" }
            "IN_PROGRESS"  { "[..]" }
            "PENDING"      { "[  ]" }
            "BLOCKED"      { "[!!]" }
            "NEEDS_REVIEW" { "[??]" }
            default        { "[--]" }
        }
        
        $color = switch ($r.Status) {
            "COMPLETED"    { "Green" }
            "IN_PROGRESS"  { "Yellow" }
            "PENDING"      { "Gray" }
            "BLOCKED"      { "Red" }
            "NEEDS_REVIEW" { "Magenta" }
            default        { "White" }
        }
        
        Write-Host "  $icon " -NoNewline -ForegroundColor $color
        Write-Host "$($r.Module.PadRight(20))" -NoNewline -ForegroundColor White
        Write-Host " $($r.Status)" -ForegroundColor $color
    }
    
    Write-Host ""
    
    # Summary
    $completed = ($results | Where-Object { $_.Status -eq "COMPLETED" }).Count
    $needsReview = ($results | Where-Object { $_.Status -eq "NEEDS_REVIEW" }).Count
    $blocked = ($results | Where-Object { $_.Status -eq "BLOCKED" }).Count
    
    if ($needsReview -gt 0) {
        Write-Host "  [??] $needsReview module(s) need review!" -ForegroundColor Magenta
    }
    if ($blocked -gt 0) {
        Write-Host "  [!!] $blocked module(s) blocked!" -ForegroundColor Red
    }
    if ($completed -eq $results.Count -and $results.Count -gt 0) {
        Write-Host "  [OK] All modules completed!" -ForegroundColor Green
    }
    
    Write-Host ""
}
