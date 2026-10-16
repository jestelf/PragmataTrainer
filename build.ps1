param([string]$Out = '', [string]$VcVars = 'C:\BuildTools\VC\Auxiliary\Build\vcvars64.bat')
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if (-not $Out) { $Out = Join-Path $root 'dist\PragmataTrainer.exe' }
New-Item -ItemType Directory -Force (Split-Path $Out) | Out-Null
$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$assets = Join-Path $root 'assets'
New-Item -ItemType Directory -Force $assets | Out-Null

$tmp = Join-Path $env:TEMP 'ptb'
New-Item -ItemType Directory -Force $tmp | Out-Null
Copy-Item "$root\src\PragmataBridge.cpp" $tmp -Force
$vcvars = $VcVars
Set-Content "$tmp\b.bat" -Encoding ASCII -Value "@call `"$vcvars`" >nul 2>nul`r`ncd /d `"$tmp`"`r`ncl /nologo /O2 /LD /EHa /std:c++17 /utf-8 /W3 PragmataBridge.cpp /link /OUT:PragmataBridge.dll user32.lib"
cmd /c "$tmp\b.bat"
if ($LASTEXITCODE -ne 0) { throw 'PragmataBridge.dll: compilation failed' }
Copy-Item "$tmp\PragmataBridge.dll" "$assets\PragmataBridge.dll" -Force

$gen = Join-Path $env:TEMP 'PragmataTrainerIconGen.exe'
& $csc /nologo /target:exe /out:$gen /r:System.Drawing.dll "$root\src\IconGen.cs"
if ($LASTEXITCODE -ne 0) { throw 'IconGen: compilation failed' }
& $gen $assets
if ($LASTEXITCODE -ne 0) { throw 'IconGen: generation failed' }
Remove-Item $gen -ErrorAction SilentlyContinue

& $csc /nologo /target:winexe /platform:x64 /optimize+ "/out:$Out" "/win32icon:$assets\pragmata.ico" "/win32manifest:$root\src\app.manifest" `
  "/resource:$assets\pragmata.png,pragmata.png" "/resource:$assets\PragmataBridge.dll,PragmataBridge.dll" `
  /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  "$root\src\PragmataTrainer.cs" "$root\src\PragmataLink.cs"
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
Get-Item $Out | Select-Object Name, Length, LastWriteTime
