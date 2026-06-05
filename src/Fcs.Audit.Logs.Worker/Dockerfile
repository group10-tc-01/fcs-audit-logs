FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/fcs.Audit.Logs.Worker/fcs.Audit.Logs.Worker.csproj", "src/fcs.Audit.Logs.Worker/"]
COPY ["src/fcs.Audit.Logs.Application/fcs.Audit.Logs.Application.csproj", "src/fcs.Audit.Logs.Application/"]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
RUN dotnet restore "src/fcs.Audit.Logs.Worker/fcs.Audit.Logs.Worker.csproj"
COPY . .
WORKDIR "/src/src/fcs.Audit.Logs.Worker"
RUN dotnet build "fcs.Audit.Logs.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "fcs.Audit.Logs.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "fcs.Audit.Logs.Worker.dll"]
