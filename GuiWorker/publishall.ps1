dotnet publish GuiWorker.csproj --output ..\binaries\win-x64 --configuration Release --runtime win-x64 --self-contained
dotnet publish GuiWorker.csproj --output ..\binaries\linux-x64 --configuration Release --runtime linux-x64 --self-contained
dotnet publish GuiWorker.csproj --output ..\binaries\osx-arm64 --configuration Release --runtime osx-arm64 --self-contained
