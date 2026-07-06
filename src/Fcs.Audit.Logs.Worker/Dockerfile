FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Fcs.Audit.Logs.Worker/Fcs.Audit.Logs.Worker.csproj", "src/Fcs.Audit.Logs.Worker/"]
COPY ["src/Fcs.Audit.Logs.Application/Fcs.Audit.Logs.Application.csproj", "src/Fcs.Audit.Logs.Application/"]
COPY ["Directory.Build.props", "."]
COPY ["Directory.Packages.props", "."]
RUN dotnet restore "src/Fcs.Audit.Logs.Worker/Fcs.Audit.Logs.Worker.csproj"
COPY . .
WORKDIR "/src/src/Fcs.Audit.Logs.Worker"
RUN dotnet build "Fcs.Audit.Logs.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Fcs.Audit.Logs.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fcs.Audit.Logs.Worker.dll"]
