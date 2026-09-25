# ==========================================
# Этап 1: Сборка и компиляция (SDK 9.0)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. Копируем файлы проектов для кэширования слоя restore
COPY ["Users/EventManager.Users.Domain/EventManager.Users.Domain.csproj", "Users/EventManager.Users.Domain/"]
COPY ["Users/EventManager.Users.Application/EventManager.Users.Application.csproj", "Users/EventManager.Users.Application/"]
COPY ["Users/EventManager.Users.Infrastructure/EventManager.Users.Infrastructure.csproj", "Users/EventManager.Users.Infrastructure/"]
COPY ["Users/EventManager.Users.Presentation/EventManager.Users.Presentation.csproj", "Users/EventManager.Users.Presentation/"]

# Восстанавливаем зависимости
RUN dotnet restore "Users/EventManager.Users.Presentation/EventManager.Users.Presentation.csproj"

# 2. Копируем исходный код
COPY Users/ Users/

# 3. Публикуем релизный бинарник в /app/publish
WORKDIR "/src/Users/EventManager.Users.Presentation"
RUN dotnet publish "EventManager.Users.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# Этап 2: Финальный образ (Runtime ASP.NET 9.0)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Порт по умолчанию для ASP.NET Core 8/9
EXPOSE 8080

# Копируем скомпилированные файлы
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "EventManager.Users.Presentation.dll"]