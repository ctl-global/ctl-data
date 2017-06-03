@rem NuGetBuildTasksPackTargets is a workaround for https://github.com/NuGet/Home/issues/4853
dotnet pack --configuration=release /p:NuGetBuildTasksPackTargets="000"