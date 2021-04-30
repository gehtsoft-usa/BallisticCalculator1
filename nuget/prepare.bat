msbuild nuget.proj -t:Prepare
msbuild nuget.proj -t:NuSpec,NuPack
