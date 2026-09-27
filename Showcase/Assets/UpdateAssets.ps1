[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$headers = @{ 'User-Agent' = 'Streamline.NET-Showcase'; 'Accept' = 'application/vnd.github+json' }
$stage = Join-Path ([IO.Path]::GetTempPath()) ('StreamlineShowcase-' + [Guid]::NewGuid())
New-Item -ItemType Directory -Path $stage | Out-Null
try {
    $sceneStage = Join-Path $stage 'Scenes'
    $sdkStage = Join-Path $stage 'Streamline'
    New-Item -ItemType Directory -Path $sceneStage, $sdkStage | Out-Null
    $commit = (Invoke-RestMethod 'https://api.github.com/repos/KhronosGroup/glTF-Sample-Assets/commits/main' -Headers $headers).sha
    $tree = Invoke-RestMethod "https://api.github.com/repos/KhronosGroup/glTF-Sample-Assets/git/trees/${commit}?recursive=1" -Headers $headers
    if ($tree.truncated) { throw 'The asset file list is incomplete.' }
    $prefix = 'Models/Sponza/glTF/'
    $files = @($tree.tree | Where-Object { $_.type -eq 'blob' -and ($_.path.StartsWith($prefix) -or $_.path -eq 'Models/Sponza/README.md' -or $_.path -eq 'Models/Sponza/LICENSE.md') })
    foreach ($file in $files) {
        $relative = if ($file.path.StartsWith($prefix)) { $file.path.Substring($prefix.Length) } else { 'Attribution/' + $file.path }
        $target = Join-Path $sceneStage $relative
        New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($target)) | Out-Null
        Write-Host "Downloading Sponza/$relative"
        Invoke-WebRequest "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/$commit/$($file.path)" -OutFile $target
    }
    $attribution = Get-Content (Join-Path $sceneStage 'Attribution/Models/Sponza/README.md') -Raw
    $licenses = [regex]::Matches($attribution, '\]\(\.\./\.\./(LICENSES/[^)]+)\)') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
    foreach ($licenseSource in $licenses) {
        if (!($tree.tree | Where-Object { $_.type -eq 'blob' -and $_.path -eq $licenseSource })) { throw "Missing upstream license: $licenseSource" }
        $licenseTarget = Join-Path $sceneStage ('Attribution/' + $licenseSource)
        New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($licenseTarget)) | Out-Null
        Invoke-WebRequest "https://raw.githubusercontent.com/KhronosGroup/glTF-Sample-Assets/$commit/$licenseSource" -OutFile $licenseTarget
    }
    if (!(Test-Path (Join-Path $sceneStage 'Sponza.gltf'))) { throw 'Sponza.gltf was not downloaded.' }
    $model = Get-Content (Join-Path $sceneStage 'Sponza.gltf') -Raw | ConvertFrom-Json
    foreach ($entry in @($model.buffers) + @($model.images)) {
        if ($entry.PSObject.Properties.Name -contains 'uri' -and !$entry.uri.StartsWith('data:')) {
            if (!(Test-Path (Join-Path $sceneStage ([Uri]::UnescapeDataString($entry.uri))))) { throw "Missing scene reference: $($entry.uri)" }
        }
    }
    $release = Invoke-RestMethod 'https://api.github.com/repos/NVIDIA-RTX/Streamline/releases/latest' -Headers $headers
    $asset = @($release.assets | Where-Object { $_.name -eq "streamline-sdk-$($release.tag_name).zip" })
    if ($asset.Count -ne 1) { throw 'Could not identify the official Windows x64 SDK archive.' }
    $zip = Join-Path $stage 'sdk.zip'
    Write-Host "Downloading Streamline $($release.tag_name), Windows x64"
    Invoke-WebRequest $asset[0].browser_download_url -OutFile $zip
    $archiveHash = (Get-FileHash $zip -Algorithm SHA256).Hash
    if ($asset[0].PSObject.Properties.Name -contains 'digest' -and $asset[0].digest -and $asset[0].digest -ne "sha256:$archiveHash") { throw 'SDK archive digest does not match the official release.' }
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [IO.Compression.ZipFile]::OpenRead($zip)
    try {
        # The production runtime occupies bin/x64 directly; development/debug are separate subdirectories.
        $runtimeDlls = @('sl.interposer.dll', 'sl.common.dll', 'sl.dlss.dll', 'sl.dlss_d.dll', 'sl.dlss_g.dll', 'sl.reflex.dll', 'sl.pcl.dll', 'nvngx_dlss.dll', 'nvngx_dlssd.dll', 'nvngx_dlssg.dll', 'NvLowLatencyVk.dll')
        $runtime = @($archive.Entries | Where-Object {
            $_.FullName -match '(^|/)bin/x64/[^/]+$' -and
            ($runtimeDlls -contains $_.Name -or $_.Name -in @('nvngx_dlss.license.txt', 'reflex.license.txt'))
        })
        foreach ($entry in $runtime) {
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, (Join-Path $sdkStage $entry.Name), $true)
        }
        foreach ($entry in $archive.Entries | Where-Object { $_.FullName -match '(^|/)(license|notice)[^/]*\.(txt|md)$' }) {
            $license = Join-Path $sdkStage ('Licenses/' + $entry.FullName)
            $licenseRoot = [IO.Path]::GetFullPath((Join-Path $sdkStage 'Licenses')) + [IO.Path]::DirectorySeparatorChar
            if (![IO.Path]::GetFullPath($license).StartsWith($licenseRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid license path in SDK archive.' }
            New-Item -ItemType Directory -Force -Path ([IO.Path]::GetDirectoryName($license)) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $license, $true)
        }
    } finally { $archive.Dispose() }
    foreach ($name in $runtimeDlls) {
        if (!(Test-Path (Join-Path $sdkStage $name))) { throw "Production runtime is incomplete: $name" }
    }
    @{ version = $release.tag_name; source = $asset[0].browser_download_url; sha256 = $archiveHash; architecture = 'x64' } | ConvertTo-Json | Set-Content (Join-Path $sdkStage 'streamline-runtime.json') -Encoding utf8
    @{ repository = 'KhronosGroup/glTF-Sample-Assets'; commit = $commit; model = 'Sponza' } | ConvertTo-Json | Set-Content (Join-Path $sceneStage 'asset-source.json') -Encoding utf8
    foreach ($name in 'Scenes', 'Streamline') {
        $destination = Join-Path $PSScriptRoot $name
        # Replace only after both downloads and scene-reference checks have succeeded.
        if (Test-Path $destination) { Remove-Item $destination -Recurse -Force }
        Move-Item (Join-Path $stage $name) $destination
    }
    Write-Host 'Assets are ready. Run: dotnet run --project Showcase -c Release'
} finally {
    Remove-Item $stage -Recurse -Force
}
