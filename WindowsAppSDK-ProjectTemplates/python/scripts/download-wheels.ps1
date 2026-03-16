param(
    [Parameter(Mandatory=$false)]
    [string]$Repo = "Hong-Xiang/PyWinAppSDK",
    
    [Parameter(Mandatory=$false)]
    [string]$Tag = "latest",
    
    [Parameter(Mandatory=$false)]
    [string]$OutDir = "wheels"
)

$ErrorActionPreference = "Stop"

# Ensure output directory exists
if (-not (Test-Path $OutDir)) {
    Write-Host "Creating directory: $OutDir" -ForegroundColor Cyan
    New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
}

# Determine API URL
$apiUrl = if ($Tag -eq "latest") {
    "https://api.github.com/repos/$Repo/releases/latest"
} else {
    "https://api.github.com/repos/$Repo/releases/tags/$Tag"
}

Write-Host "Fetching release info from: $apiUrl" -ForegroundColor Cyan

try {
    $headers = @{
        "Accept" = "application/vnd.github.v3+json"
    }
    
    # Use GITHUB_TOKEN if available to avoid rate limits
    if ($env:GITHUB_TOKEN) {
        $headers["Authorization"] = "token $env:GITHUB_TOKEN"
    }

    $release = $null
    try {
        $release = Invoke-RestMethod -Uri $apiUrl -Headers $headers
    }
    catch {
        # If tag not found and doesn't start with 'v', try adding 'v' prefix
        if ($Tag -ne "latest" -and $Tag -notlike "v*") {
            $vTag = "v$Tag"
            $vApiUrl = "https://api.github.com/repos/$Repo/releases/tags/$vTag"
            Write-Host "Tag '$Tag' not found, trying '$vTag'..." -ForegroundColor Gray
            try {
                $release = Invoke-RestMethod -Uri $vApiUrl -Headers $headers
            }
            catch {
                # Throw the original error if 'v' prefix also fails
                throw
            }
        }
        else {
            throw
        }
    }

    $zipAsset = $release.assets | Where-Object { $_.name -eq "pywinappsdk-wheels.zip" }

    if ($null -eq $zipAsset) {
        Write-Warning "pywinappsdk-wheels.zip not found in release '$($release.tag_name)'. Falling back to individual .whl files."
        $assets = $release.assets | Where-Object { $_.name -like "*.whl" }
        
        if ($null -eq $assets -or $assets.Count -eq 0) {
            Write-Warning "No wheel files found in release '$($release.tag_name)'"
            return
        }

        Write-Host "Found $($assets.Count) wheels in release $($release.tag_name)" -ForegroundColor Green

        foreach ($asset in $assets) {
            $destFile = Join-Path $OutDir $asset.name
            Write-Host "Downloading $($asset.name)..." -ForegroundColor Yellow
            Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $destFile
        }
    } else {
        Write-Host "Found pywinappsdk-wheels.zip in release $($release.tag_name)" -ForegroundColor Green
        $zipPath = Join-Path $OutDir "pywinappsdk-wheels.zip"
        
        Write-Host "Downloading pywinappsdk-wheels.zip..." -ForegroundColor Yellow
        Invoke-WebRequest -Uri $zipAsset.browser_download_url -OutFile $zipPath
        
        Write-Host "Extracting wheels..." -ForegroundColor Yellow
        Expand-Archive -Path $zipPath -DestinationPath $OutDir -Force
        
        Write-Host "Cleaning up zip file..." -ForegroundColor Gray
        Remove-Item $zipPath
    }

    Write-Host "`nSuccessfully downloaded wheels to $OutDir" -ForegroundColor Green
}
catch {
    Write-Error "Failed to download wheels: $($_.Exception.Message)"
    exit 1
}
