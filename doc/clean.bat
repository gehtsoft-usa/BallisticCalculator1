@echo off
dotnet build project.proj /t:CleanDoc
del obj\*.* /s /q >nul
rmdir obj /s /q >nul
del bin\*.* /s /q >nul
rmdir bin /s /q >nul
