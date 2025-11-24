FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj files and restore
COPY ["CoderamaOpsAI.Api/CoderamaOpsAI.Api.csproj", "CoderamaOpsAI.Api/"]
COPY ["CoderamaOpsAI.Dal/CoderamaOpsAI.Dal.csproj", "CoderamaOpsAI.Dal/"]
COPY ["CoderamaOpsAI.Common/CoderamaOpsAI.Common.csproj", "CoderamaOpsAI.Common/"]
RUN dotnet restore "CoderamaOpsAI.Api/CoderamaOpsAI.Api.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/CoderamaOpsAI.Api"
RUN dotnet build "CoderamaOpsAI.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoderamaOpsAI.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoderamaOpsAI.Api.dll"]
