# TouchScreenPOS

## Release build/run

Build (Release):

```powershell
cd .\TouchScreenPOS

dotnet clean -c Release

dotnet build -c Release
```

Run from build output (no VS Code):

```powershell
.\bin\Release\net8.0-windows\TouchScreenPOS.exe
```

## Publish (MSIX + MSI)

Publish MSIX artifacts:

```powershell
cd .\TouchScreenPOS\Installer

dotnet publish ..\TouchScreenPOS.csproj -c Release -r win-x64
```

Build MSI (WiX):

```powershell
cd .\TouchScreenPOS\Installer

.\build-msi.ps1
```
