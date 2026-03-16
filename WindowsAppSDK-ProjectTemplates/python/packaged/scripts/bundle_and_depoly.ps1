rm -r build 
rm -r dist 
.\scripts\bundle.ps1
.\scripts\pack.ps1
Add-AppxPackage -Path packaged-winapp.msix
