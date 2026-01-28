Param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$projectDir = Resolve-Path ".."
$installerDir = Resolve-Path "."
$publishDir = Join-Path $installerDir "publish"

dotnet publish (Join-Path $projectDir "TouchScreenPOS.csproj") -c $Configuration -r $Runtime --self-contained false -o $publishDir

$wxs = Join-Path $installerDir "TouchScreenPOS.wxs"
$wixobj = Join-Path $installerDir "TouchScreenPOS.wixobj"
$msi = Join-Path $installerDir "TouchScreenPOS.msi"

if (Get-Command wix -ErrorAction SilentlyContinue) {
    $publishDirArg = "PublishDir=$publishDir\\"
    $projectDirArg = "ProjectDir=$projectDir\\"
    & wix build $wxs -d $publishDirArg -d $projectDirArg -o $msi
    exit 0
}

$candle = Get-Command candle -ErrorAction SilentlyContinue
$light = Get-Command light -ErrorAction SilentlyContinue
if ($candle -and $light) {
    & $candle.Source -dPublishDir="$publishDir\" -dProjectDir="$projectDir\" -out $wixobj $wxs
    & $light.Source -out $msi $wixobj
    exit 0
}

Write-Error "WiX Toolset not found. Install WiX (wix.exe) or candle/light, then re-run this script."
exit 1
