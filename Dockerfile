# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Устанавливаем рабочую директорию
WORKDIR /src

# Устанавливаем curl для healthcheck
RUN apt-get update && apt-get install -y curl

# Копируем все .csproj файлы и восстанавливаем зависимости
COPY ["BotGarden.Web/BotGarden.Web.csproj", "BotGarden.Web/"]
COPY ["BotGarden.Web/nupkgs/", "BotGarden.Web/nupkgs/"]

# Добавляем локальный источник NuGet пакетов
RUN dotnet nuget add source /src/BotGarden.Web/nupkgs --name LocalPackages

# Восстанавливаем зависимости
RUN dotnet restore "BotGarden.Web/BotGarden.Web.csproj"

# Копируем все остальные файлы
COPY . .

# Собираем приложение
WORKDIR "/src/BotGarden.Web"
RUN dotnet build "BotGarden.Web.csproj" -c Release -o /app/build

# Публикуем приложение
FROM build AS publish
ARG ASPNETCORE_ENVIRONMENT
ENV ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT}
RUN dotnet publish "BotGarden.Web.csproj" -c Release -o /app/publish

# Final stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Устанавливаем curl для healthcheck
RUN apt-get update && apt-get install -y curl

WORKDIR /app

# Создаем директорию для загрузок и устанавливаем права
RUN mkdir -p /app/Uploads && \
    chown -R $APP_UID:$APP_UID /app/Uploads

# Копируем опубликованное приложение
COPY --from=publish /app/publish .

# Устанавливаем переменные окружения
ENV ASPNETCORE_URLS=http://+:80

# Добавляем healthcheck
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

EXPOSE 80

ENTRYPOINT ["dotnet", "BotGarden.Web.dll"]
