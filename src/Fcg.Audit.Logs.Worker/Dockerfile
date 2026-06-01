FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Fcg.Audit.Logs.Worker/Fcg.Audit.Logs.Worker.csproj", "src/Fcg.Audit.Logs.Worker/"]
COPY ["src/Fcg.Audit.Logs.Application/Fcg.Audit.Logs.Application.csproj", "src/Fcg.Audit.Logs.Application/"]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
RUN dotnet restore "src/Fcg.Audit.Logs.Worker/Fcg.Audit.Logs.Worker.csproj"
COPY . .
WORKDIR "/src/src/Fcg.Audit.Logs.Worker"
RUN dotnet build "Fcg.Audit.Logs.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Fcg.Audit.Logs.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fcg.Audit.Logs.Worker.dll"]
