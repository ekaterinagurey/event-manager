# ==========================================
# Этап 1: Сборка и компиляция (SDK 9.0)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. Копируем файлы проектов для кэширования слоя restore
COPY ["EventManager.Shared.Contracts/EventManager.Shared.Contracts.csproj", "EventManager.Shared.Contracts/"]
COPY ["Bookings/EventManager.Bookings.Domain/EventManager.Bookings.Domain.csproj", "Bookings/EventManager.Bookings.Domain/"]
COPY ["Bookings/EventManager.Bookings.Application/EventManager.Bookings.Application.csproj", "Bookings/EventManager.Bookings.Application/"]
COPY ["Bookings/EventManager.Bookings.Infrastructure/EventManager.Bookings.Infrastructure.csproj", "Bookings/EventManager.Bookings.Infrastructure/"]
COPY ["Bookings/EventManager.Bookings.Presentation/EventManager.Bookings.Presentation.csproj", "Bookings/EventManager.Bookings.Presentation/"]

# Восстанавливаем зависимости
RUN dotnet restore "Bookings/EventManager.Bookings.Presentation/EventManager.Bookings.Presentation.csproj"

# 2. Копируем исходный код
COPY EventManager.Shared.Contracts/ EventManager.Shared.Contracts/
COPY Bookings/ Bookings/

# 3. Публикуем релизный бинарник в /app/publish
WORKDIR "/src/Bookings/EventManager.Bookings.Presentation"
RUN dotnet publish "EventManager.Bookings.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# Этап 2: Финальный образ (Runtime ASP.NET 9.0)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Порт по умолчанию для ASP.NET Core 8/9
EXPOSE 8080

# Копируем скомпилированные файлы
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EventManager.Bookings.Presentation.dll"]