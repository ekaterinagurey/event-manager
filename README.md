# Event Manager API

REST API для управления мероприятиями

# Архитектура проекта

Проект построен с разделением на слои

```text
EventManager/
│
├── EventManager.Domain/
│   ├── Models/
│   └── Exceptions/
│
├── EventManager.Application/
│   ├── DTOs/
│   │   ├── Bookings/
│   │   └── Events/
│   ├── Repositories/
│   │   └── Interfaces/
│   ├── Services/
│   │   └── Interfaces/
│   └── Mappers/
│
├── EventManager.Infrastructure/
│   ├── DataAccess/
│   │   ├── AppDbContext.cs
│   │   └── Configurations/
│   ├── Repositories/
│   ├── Migrations/
│   └── DependencyInjection.cs
│
├── EventManager/
│   ├── Controllers/
│   ├── Middleware/
│   └── Program.cs
│
├── EventManager.IntegrationTests/
└── EventManager.Tests/
```

## Domain

Слой **Domain** содержит доменные сущности и не зависит от внешних фреймворков.

### В этом слое находятся:
- сущности `Event` и `Booking`;
- перечисление `BookingStatus`;
- бизнес-правила сущностей;
- доменные исключения.

---

## Application

Слой **Application** содержит бизнес-сценарии приложения и определяет необходимые для них абстракции.

### В этом слое находятся:
- интерфейсы сервисов;
- реализации сервисов;
- DTO;
- интерфейсы репозиториев;

**Важно:** `Application` не зависит от `Infrastructure`.

---

## Infrastructure

Слой **Infrastructure** содержит реализации, зависящие от внешних технологий.

### В этом слое находятся:
- `AppDbContext`;
- конфигурации сущностей EF Core;
- реализации репозиториев;
- PostgreSQL;
- EF Core migrations;

---

## Presentation

**Presentation** (проект EventManager) отвечает за взаимодействие с клиентом по HTTP.

### В этом слое находятся:
- контроллеры;
- глобальный обработчик исключений;
- `Program.cs`;
- Регистрация зависимостей через DI.

Контроллеры не содержат бизнес-логики и не работают напрямую с `DbContext` или репозиториями. Они вызывают Application-сервисы и возвращают HTTP-ответ.

---

## Регистрация зависимостей

Для сохранения `Program.cs` компактным, слой **Infrastructure:** предоставляет extension-методы для регистрации зависимостей.

```csharp
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

# Запуск проекта

## Настройка базы данных

Для запуска приложения требуется **PostgreSQL**.

### Настройка строки подключения

Перед запуском приложения необходимо указать строку подключения к PostgreSQL в конфигурации приложения.

В `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=${POSTGRES_PASSWORD}"
  }
}
```

### Конфигурация и управление секретами

Приложение запускается локально (через IDE или dotnet run), 
а база данных запускается через Docker Compose. 

#### Запуск базы данных
При запуске базы данных через Docker Compose пароль к PostgreSQL передаётся через переменные окружения файла `.env`.

**Создайте файл `.env`** в корне репозитория (рядом с `docker-compose.yml`) на основе шаблона .env.example.
Пример:
POSTGRES_PASSWORD=your_secure_password

#### Локальный запуск приложения через IDE
При локальном запуске приложения через IDE или через терминал (`dotnet run`) пароль к PostgreSQL хранится с 
использованием встроенного инструмента **.NET Secret Manager (`dotnet user-secrets`)**.

 Настройка **user-secrets** через CLI (Терминал)

1. **Инициализация хранилища секретов** (создает уникальный `<UserSecretsId>` в файле `.csproj`):
   ```bash
   dotnet user-secrets init
    ```

2. Добавление пароля к базе данных:
    ```bash
    dotnet user-secrets set "POSTGRES_PASSWORD" "your_local_password"
    ```  

### Миграции EF Core

Схема базы данных управляется с помощью **миграций Entity Framework Core**.

## Создание миграции

Для создания новой миграции выполните:

```bash
dotnet ef migrations add InitialCreate
```

где `InitialCreate` — имя миграции.

### Применение миграций

Для применения миграций к базе данных:

```bash
dotnet ef database update
```

После выполнения команды EF Core создаст или обновит схему PostgreSQL в соответствии с миграциями.

При запуске приложения схема также может быть автоматически обновлена через вызов:

```csharp
context.Database.Migrate();
```

> Для выполнения команд `dotnet ef` может потребоваться установить инструмент Entity Framework Core CLI:

```bash
dotnet tool install --global dotnet-ef
```
---

### Unit-тесты
Для юнит-тестов используется **Entity Framework Core InMemory Database**. Тесты не требуют подключения к PostgreSQL.

Для каждого теста используется отдельное имя InMemory-базы:

```csharp
var dbName = Guid.NewGuid().ToString();

services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase(dbName));
```

Это позволяет изолировать данные разных тестов и выполнять тесты независимо от состояния реальной базы данных

### Интеграционные тесты

Для проверки работы репозиториев с реальным PostgreSQL используются интеграционные тесты на базе **Testcontainers**.

При запуске интеграционных тестов автоматически создаётся контейнер PostgreSQL, в котором выполняются тесты репозиториев.

Для запуска интеграционных тестов необходимо:

1. Установить **Docker**.
2. Убедиться, что Docker Engine запущен.
3. Выполнить:

```bash
dotnet test EventManager\EventManager.IntegrationTests\EventManager.IntegrationTests.csproj
```

Testcontainers самостоятельно создаёт и запускает PostgreSQL-контейнер на время выполнения тестов.

## Собрать проект
```bash
dotnet build EventManager\EventManager\EventManager.csproj -c Debug 
```

## Запустить приложение
```bash
dotnet run --project EventManager\EventManager\EventManager.csproj 
```

## После запуска приложение будет доступно по адресу:
http://localhost:<port>

# Запуск тестов

Для запуска Unit-тестов выполните:

```bash
dotnet test EventManager\EventManager.Tests\EventManager.Tests.csproj
```

Для запуска интеграционных тестов выполните:

```bash
dotnet test EventManager\EventManager.IntegrationTests\EventManager.IntegrationTests.csproj
```

# Swagger

Swagger UI доступен по адресу:
https://localhost:<port>/swagger

# API

## Events

### Получить список мероприятий
GET /events

#### Параметры фильтрации и пагинации

| Параметр   | Тип      | Описание                                                      |
| ---------- | -------- | ------------------------------------------------------------- |
| title    | string   | Поиск по названию (регистронезависимый, частичное совпадение) |
| from     | DateTime | События, начинающиеся не раньше указанной даты        |
| to       | DateTime | События, заканчивающиеся не позже указанной даты      |
| page     | int      | Номер страницы (по умолчанию 1)                               |
| pageSize | int      | Количество элементов на странице (по умолчанию 10)            |


### Получить мероприятие по идентификатору
GET /events/{id}

Создать мероприятие
POST /events
Тело запроса:
{
  "title": "event1",
  "description": "string",
  "startAt": "2026-06-14T21:47:09.316Z",
  "endAt": "2026-06-15T21:47:09.316Z"
}

### Обновить мероприятие
PUT /events/{id}

### Удалить мероприятие
DELETE /events/{id}

## Bookings

### Создать бронирование

POST /events/{id}/book

Создает бронь для указанного события.

**Ответы:**

| Код | Описание |
|------|----------|
| 202 Accepted | Бронь успешно создана |
| 404 Not Found | Событие не найдено |
| 409 Conflict | На событии отсутствуют свободные места |

### Получить информацию о бронировании

GET /bookings/{id}


## Модель Booking

| Поле | Тип | Описание |
|------|-----|----------|
| Id | Guid | Уникальный идентификатор брони |
| EventId | Guid | Идентификатор мероприятия |
| Status | BookingStatus | Текущий статус брони |
| CreatedAt | DateTime | Дата и время создания |
| ProcessedAt | DateTime? | Дата и время обработки |
| TotalSeats | int | Общее количество мест на событии |
| AvailableSeats | int | Текущее количество свободных мест |

При создании события значение `AvailableSeats` автоматически устанавливается равным `TotalSeats`.

---

## Статусы бронирования

| Статус | Описание |
|---------|----------|
| Pending | Бронь создана и ожидает обработки |
| Confirmed | Бронь успешно подтверждена |
| Rejected | Бронь отклонена |


## Фоновая обработка бронирований

После создания бронирования ему автоматически присваивается статус Pending.

Фоновый сервис (`BackgroundService`):

1. Периодически проверяет наличие бронирований со статусом Pending.
2. Выполняет искусственную задержку (`Task.Delay`) продолжительностью 2 секунды, имитируя обращение к внешней системе.
3. Изменяет статус бронирования на Confirmed.
4. Заполняет поле `ProcessedAt`.
5. Сохраняет изменения в хранилище.

## Формат ошибок

### 400 Bad Request

Возвращается при ошибках валидации.

{
  "status": 400,
  "detail": "EndAt должна быть позже StartAt."
}
### 404 Not Found

Возвращается, если событие не найдено.

{
  "status": 404,
  "detail": "Событие с id = 1 не найдено."
}
### 500 Internal Server Error

Возвращается при непредвиденных ошибках.

{
  "status": 500,
  "detail": "An unexpected error occurred."
}

## Используемые примитивы синхронизации

### SemaphoreSlim

В `BookingService` используется

```
private readonly SemaphoreSlim _processingSemaphore = new(1, 1);
```

`SemaphoreSlim` применяется для предотвращения состояния гонки (overbooking) при одновременных запросах на бронирование мест.

---

## Пример предотвращения овербукинга

Пусть событие имеет:

```
TotalSeats = 5
AvailableSeats = 5
```

Одновременно поступает **20 запросов** на бронирование.

Результат работы сервиса:

- успешно создаются **5 бронирований**;
- **15 запросов** получают ответ **409 Conflict** с сообщением:

```text
No available seats for this event
```

Количество созданных бронирований  не превышает количество доступных мест.