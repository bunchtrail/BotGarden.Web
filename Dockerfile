# ��� 1: ���������� SDK ����� ��� ������ � .NET 8.0
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# �������� ���� ������� � ��������� ��������� �������� NuGet �������
COPY ["BotGarden.Web/BotGarden.Web.csproj", "BotGarden.Web/"]
COPY BotGarden.Web/nupkgs/ /src/nupkgs/

# ��������� ��������� �������� NuGet
RUN dotnet nuget add source /src/nupkgs --name LocalNuget

# ��������������� �����������, ������� ��������� ������
RUN dotnet restore "BotGarden.Web/BotGarden.Web.csproj"

# �������� ��������� ��� � �������� ������
COPY BotGarden.Web/ BotGarden.Web/
WORKDIR "/src/BotGarden.Web"
RUN dotnet build "BotGarden.Web.csproj" -c Release -o /app/build

# ��� 2: ��������� ������
RUN dotnet publish "BotGarden.Web.csproj" -c Release -o /app/publish

# ��� 3: ���������� runtime ����� � .NET 8.0
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "BotGarden.Web.dll"]
