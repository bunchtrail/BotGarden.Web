# Шаг 1: Используем SDK образ для сборки с .NET 8.0
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Копируем файл проекта и добавляем локальный источник NuGet пакетов
COPY ["BotGarden.Web/BotGarden.Web.csproj", "BotGarden.Web/"]
COPY BotGarden.Web/nupkgs/ /src/nupkgs/

# Добавляем локальный источник NuGet
RUN dotnet nuget add source /src/nupkgs --name LocalNuget

# Восстанавливаем зависимости, включая локальные пакеты
RUN dotnet restore "BotGarden.Web/BotGarden.Web.csproj"

# Копируем остальной код и собираем проект
COPY BotGarden.Web/ BotGarden.Web/
WORKDIR "/src/BotGarden.Web"
RUN dotnet build "BotGarden.Web.csproj" -c Release -o /app/build

# Шаг 2: Публикуем проект
RUN dotnet publish "BotGarden.Web.csproj" -c Release -o /app/publish

# Шаг 3: Используем runtime образ с .NET 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BotGarden.Web.dll"]
