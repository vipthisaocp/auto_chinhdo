@echo off
title Publish Production - Auto Chinh Do
echo -------------------------------------------------------
echo [1/3] Dang don dep cac ban build cu...
if exist bin\ReadyToUse rd /s /q bin\ReadyToUse

echo [2/3] Dang Build ban SingleFile toi uu (win-x64)...
dotnet publish .\auto_chinhdo.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o bin\ReadyToUse\

echo [3/3] Dang bo sung tai nguyen (adb, tessdata, templates)...
copy ".\bin\Release\net8.0-windows\adb_path.txt" ".\bin\ReadyToUse\adb_path.txt" /y >nul 2>&1
xcopy ".\tessdata" ".\bin\ReadyToUse\tessdata\" /s /e /i /y >nul 2>&1
xcopy ".\bin\Release\net8.0-windows\templates" ".\bin\ReadyToUse\templates\" /s /e /i /y >nul 2>&1
copy ".\app_icon.ico" ".\bin\ReadyToUse\app_icon.ico" /y >nul 2>&1
copy ".\firebase-admin-key.json" ".\bin\ReadyToUse\firebase-admin-key.json" /y >nul 2>&1

echo -------------------------------------------------------
echo XONG! Ban build san sang tai: bin\ReadyToUse
echo Bay gio ban co the:
echo 1. Nen ZIP cac file trong ReadyToUse de Update.
echo 2. Chay setup_script.iss de tao bo cai dat.
echo -------------------------------------------------------
pause
