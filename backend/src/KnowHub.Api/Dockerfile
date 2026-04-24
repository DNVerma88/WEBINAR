FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y --no-install-recommends libgssapi-krb5-2 && rm -rf /var/lib/apt/lists/*
USER $APP_UID
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/KnowHub.Api/KnowHub.Api.csproj", "src/KnowHub.Api/"]
COPY ["src/KnowHub.Application/KnowHub.Application.csproj", "src/KnowHub.Application/"]
COPY ["src/KnowHub.Domain/KnowHub.Domain.csproj", "src/KnowHub.Domain/"]
COPY ["src/KnowHub.Infrastructure/KnowHub.Infrastructure.csproj", "src/KnowHub.Infrastructure/"]
RUN dotnet restore "src/KnowHub.Api/KnowHub.Api.csproj"
COPY . .
WORKDIR "/src/src/KnowHub.Api"
RUN dotnet build "KnowHub.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KnowHub.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KnowHub.Api.dll"]
