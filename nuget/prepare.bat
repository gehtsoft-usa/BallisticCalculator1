dotnet build ../BallisticCalculator.sln /p:Configuration=Release
dotnet build nuget.proj -t:Prepare
dotnet build nuget.proj -t:NuSpec,NuPack
