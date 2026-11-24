@echo off
set CSC="C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist %CSC% set CSC="C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

echo Building MuelleLauncher...
%CSC% /target:winexe /optimize+ /out:..\MuelleLauncher.exe MuelleLauncher.cs
if %ERRORLEVEL% equ 0 (
    echo Successfully built MuelleLauncher.exe
) else (
    echo Build failed.
)
