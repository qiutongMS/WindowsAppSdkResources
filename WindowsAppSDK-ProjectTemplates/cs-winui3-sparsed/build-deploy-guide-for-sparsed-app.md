## Build & Deploy with Sparse Identity
1. **Prepare the environment**: Use Windows 11 on ARM64 or x64 with the .NET 8 SDK, Windows App SDK 1.8 runtime, and Windows 10/11 SDK 10.0.26100+. Work from a Developer PowerShell session (`pwsh` 7+).
2. **Detect the active architecture and project info** (run once per session):

	2.1. **Detect system architecture and platform**:
	```pwsh
	# Detect platform and RID based on actual system architecture
	$osArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
	switch ($osArch) {
		'Arm64' { $platform = 'arm64'; $rid = 'win-arm64' }
		'X64'   { $platform = 'x64'; $rid = 'win-x64' }
		'X86'   { $platform = 'x86'; $rid = 'win-x86' }
		default { 
			Write-Warning "Unknown architecture: $osArch. Defaulting to x64."
			$platform = 'x64'; $rid = 'win-x64' 
		}
	}
	Write-Host "Detected architecture: $osArch -> Platform: $platform, RID: $rid"
	```

	2.2. **Locate and identify the main project**:
	```pwsh
	# Auto-detect project name from .csproj file
	$projectFile = Get-ChildItem -Filter "*.csproj" -Recurse | Where-Object { $_.Directory.Name -ne "PackageIdentity" } | Select-Object -First 1
	$projectName = $projectFile.BaseName
	$projectDir = $projectFile.Directory.Name
	Write-Host "Found project: $projectName in directory: $projectDir"
	```

	2.3. **Read TargetFramework from project file** (AI agent task):
	```pwsh
	# AI agent: Read the TargetFramework property from the main project's .csproj file
	# 1. Open and examine the .csproj file located at: $projectFile.FullName
	# 2. Look for <TargetFramework>value</TargetFramework> in the PropertyGroup section
	# 3. Extract the exact value and assign it to $frameworkVersion variable below
	$frameworkVersion = "<AI_AGENT_READ_FROM_CSPROJ>"  # Replace with actual TargetFramework from project file
	Write-Host "Framework version: $frameworkVersion"
	```

	2.4. **Summary of detected configuration**:
	```pwsh
	Write-Host "=== Build Configuration Summary ==="
	Write-Host "Project: $projectName"
	Write-Host "Directory: $projectDir" 
	Write-Host "Platform: $platform"
	Write-Host "RID: $rid"
	Write-Host "Framework: $frameworkVersion"
	Write-Host "=================================="
	```
3. **Publish the unpackaged payload** so the sparse package has binaries to point at:
	```pwsh
	dotnet publish $projectDir/$projectName.csproj -c Debug -r $rid
	```
	Output lands at `BlankApp/bin/Debug/$frameworkVersion/$rid/publish`.
4. **Produce the sparse identity MSIX** for the detected platform:
	```pwsh
	pwsh -NoLogo -NoProfile -File PackageIdentity/BuildSparsePackage.ps1 -Configuration Debug -Platform $platform -PackageName <the package name> -Publisher "CN=<the publisher>"
	```
	The signed MSIX appears under `PackageIdentity/publish/Debug/$platform/` and the development certificate lands in `PackageIdentity/.user/`.
5. **Trust the development certificate** (once per machine) so Windows accepts the package signature. The AI agent **must** explicitly remind the user to watch for the Windows *Security Warning* dialog (similar to the screenshot) and keep it in the foreground so they can approve the certificate install. Skipping this manual confirmation leads to `0x800B0109` when installing the MSIX directly. When running below script, make sure the comment is attached:

	```pwsh
	$cert = Resolve-Path PackageIdentity/.user/<the package name>.Sparse.certificate.sample.cer
	Import-Certificate -FilePath $cert -CertStoreLocation Cert:\CurrentUser\TrustedPeople | Out-Null
	Import-Certificate -FilePath $cert -CertStoreLocation Cert:\CurrentUser\Root | Out-Null   # [Manual operation required] Please watch for a Windows *Security Warning* dialog for your app after this script is triggered, read the dialog carefully, and complete the certificate installation for your app.
	```
6. **Register the sparse package** with the manifest-first flow to avoid transient signature issues:
	```pwsh
	$external = (Resolve-Path "$projectDir/bin/Debug/$frameworkVersion/$rid/publish").Path
	Add-AppxPackage -Register PackageIdentity/AppxManifest.xml -ExternalLocation $external -ForceApplicationShutdown
	```
	After the certificate is trusted you can optionally install the MSIX directly:
	```pwsh
	$msixPath = (Resolve-Path PackageIdentity/publish/Debug/$platform/<the package name>.Sparse.msix).Path
	Add-AppxPackage -Path $msixPath -ExternalLocation $external
	```
7. **Run and verify**: look for and launch the `<project name>.exe` from the publish folder (or the Start menu) and confirm identity if needed with `pwsh PackageIdentity/Check-ProcessIdentity.ps1 -ProcessId (Get-Process BlankApp).Id`.
8. **Iterate on changes**: rerun `dotnet publish` and `BuildSparsePackage.ps1` after updates. Use `-ForceApplicationShutdown` with `Add-AppxPackage -Register` to refresh in place, and pass `-Clean` to the build script when you need to delete artifacts and uninstall the sparse package.
