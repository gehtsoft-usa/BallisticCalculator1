@echo off
dotnet build ../BallisticCalculator.sln /p:Configuration=Release
dotnet build project.proj /t:Scan,Raw