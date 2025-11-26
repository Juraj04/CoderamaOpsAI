FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CoderamaOpsAI.Worker/CoderamaOpsAI.Worker.csproj", "CoderamaOpsAI.Worker/"]
COPY ["CoderamaOpsAI.Dal/CoderamaOpsAI.Dal.csproj", "CoderamaOpsAI.Dal/"]
COPY ["CoderamaOpsAI.Common/CoderamaOpsAI.Common.csproj", "CoderamaOpsAI.Common/"]
RUN dotnet restore "CoderamaOpsAI.Worker/CoderamaOpsAI.Worker.csproj"
COPY . .
WORKDIR "/src/CoderamaOpsAI.Worker"
RUN dotnet build "CoderamaOpsAI.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoderamaOpsAI.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoderamaOpsAI.Worker.dll"]
