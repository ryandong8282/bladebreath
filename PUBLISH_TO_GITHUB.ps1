$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$RemoteUrl = "https://github.com/ryandong8282/bladebreath.git"
Set-Location $Root

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git is required. Install Git for Windows first."
}

if (Get-Command python -ErrorAction SilentlyContinue) {
    python scripts/static_validate.py
} elseif (Get-Command py -ErrorAction SilentlyContinue) {
    py -3 scripts/static_validate.py
} else {
    Write-Warning "Python was not found; static validation was skipped."
}

if (Get-Command node -ErrorAction SilentlyContinue) {
    node scripts/validate_web.js
} else {
    Write-Warning "Node.js was not found; browser-prototype validation was skipped."
}

git branch -M main
$originExists = $false
try {
    git remote get-url origin *> $null
    if ($LASTEXITCODE -eq 0) { $originExists = $true }
} catch {}

if ($originExists) {
    git remote set-url origin $RemoteUrl
} else {
    git remote add origin $RemoteUrl
}

if (Get-Command gh -ErrorAction SilentlyContinue) {
    gh auth status *> $null
    if ($LASTEXITCODE -eq 0) { gh auth setup-git *> $null }
}

git push --set-upstream origin main
if ($LASTEXITCODE -ne 0) { throw "Git push failed. Sign in to GitHub when prompted, then run this file again." }
git push origin --tags
if ($LASTEXITCODE -ne 0) { throw "Tag push failed." }

Write-Host ""
Write-Host "Published successfully: https://github.com/ryandong8282/bladebreath"
Read-Host "Press Enter to close"
