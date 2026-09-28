# ==========================================
# Этап 1: Сборка и компиляция (SDK 9.0)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. Копируем файлы проектов для кэширования слоя restore
COPY ["EventManager.Shared.Contracts/EventManager.Shared.Contracts.csproj", "EventManager.Shared.Contracts/"]
COPY ["Events/EventManager.Events.Domain/EventManager.Events.Domain.csproj", "Events/EventManager.Events.Domain/"]
COPY ["Events/EventManager.Events.Application/EventManager.Events.Application.csproj", "Events/EventManager.Events.Application/"]
COPY ["Events/EventManager.Events.Infrastructure/EventManager.Events.Infrastructure.csproj", "Events/EventManager.Events.Infrastructure/"]
COPY ["Events/EventManager.Events.Presentation/EventManager.Events.Presentation.csproj", "Events/EventManager.Events.Presentation/"]

# Восстанавливаем зависимости
RUN dotnet restore "Events/EventManager.Events.Presentation/EventManager.Events.Presentation.csproj"

# 2. Копируем исходный код
COPY EventManager.Shared.Contracts/ EventManager.Shared.Contracts/
COPY Events/ Events/

# 3. Публикуем релизный бинарник в /app/publish
WORKDIR "/src/Events/EventManager.Events.Presentation"
RUN dotnet publish "EventManager.Events.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# Этап 2: Финальный образ (Runtime ASP.NET 9.0)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Порт по умолчанию для ASP.NET Core 8/9
EXPOSE 8080

# Копируем скомпилированные файлы
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EventManager.Events.Presentation.dll"]