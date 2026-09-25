# Event Manager

REST API для управления мероприятиями

# Архитектура системы

Система состоит из трёх независимых сервисов, брокера сообщений и отдельных баз данных PostgreSQL для каждого сервиса.

## Сервис  **Users Service**
Аутентификация, регистрация пользователей и генерация JWT-токенов
PostgreSQL (`users_db`) 

## Сервис  **Events Service**
Управление событиями
PostgreSQL (`events_db`) 

## Сервис  **Bookings Service**
Создание бронирований на события
PostgreSQL (`bookings_db`) 

## Брокер сообщений Kafka
Брокер сообщений для интеграционных событий между сервисом событий и сервисом бронирования


**Аутентификация:** 
Все сервисы используют симметричную валидацию **JWT Bearer**-токенов, выпущенных `Users Service`. 
Общий секретный ключ и параметры валидации передаются через переменные окружения.


# Поток данных: BookingConfirmed

Взаимодействие между сервисами бронирований и событий:

[Клиент]
    1. POST /bookings

[Bookings Service]
    2. Сохраняет бронь (Status = Pending)

[BookingProcessingService (BookingProcessingService)]
    3. Переводит бронь в Confirmed
    4. Публикует событие в брокере сообщений

[Kafka Topic: booking-confirmed]
    5. Читает событие (Consumer Group: events-service-group)
[Events Service (BookingConfirmedConsumer)]
    6. Уменьшает счётчик свободных мест на событии


# Инструкция по запуску

 Предварительные требования

 **Docker Desktop**

## Шаг 1. Переход в рабочую директорию

Перейдите в каталог с файлом `docker-compose.yml`

## Шаг 2. Конфигурация переменных окружения (`.env`)

Создайте в папке `EventManager` файл `.env`:

```env
# Пароли к базам данных
USER_DB_PASSWORD=enter_your_pass
EVENT_DB_PASSWORD=enter_your_pass
BOOKING_DB_PASSWORD=enter_your_pass

# Конфигурация JWT токенов
JWT_SECRET=super_secret_jwt_key_that_is_long_enough_32_bytes
```

## Шаг 3. Сборка и запуск контейнеров

Запустите сборку всех образов и фоновый старт контейнеров:

```powershell
docker compose up -d --build
```

## Шаг 4. Проверка статуса

Убедитесь, что все контейнеры работают и прошли healthcheck:

```powershell
docker compose ps
```


# Доступ к интерфейсам и Swagger UI

После запуска интерфейсы Swagger UI и эндпоинты доступны по следующим адресам:

* **Users API Swagger:** http://localhost:5001/swagger
* **Events API Swagger:** http://localhost:5002/swagger
* **Bookings API Swagger:** http://localhost:5003/swagger

---
# Локальный запуск приложения через IDE
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

Для создания новой миграции выполните  в корне решения (там, где лежит файл .sln) :

```bash
dotnet ef migrations add InitialCreate --project EventManager.Users.Infrastructure --startup-project EventManager.Users.Presentation --output-dir Migrations
dotnet ef migrations add InitialCreate --project EventManager.Events.Infrastructure --startup-project EventManager.Events.Presentation --output-dir Migrations
dotnet ef migrations add InitialCreate --project EventManager.Bookings.Infrastructure --startup-project EventManager.Bookings.Presentation --output-dir Migrations
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


##  Безопасность и аутентификация

В проекте реализована аутентификация на основе **JWT** и ролевая модель доступа к ресурсам API.

---

### 1. Ролевая модель и разграничение прав

В системе предусмотрено две роли: **`User`** (Обычный пользователь) и **`Admin`** (Администратор).

---

### 2. Настройка JWT и безопасность секретного ключа

Конфигурация параметров токена находится в секции `Jwt` файла `appsettings.json`.
Сам секретный ключ хранится с использованием встроенного инструмента **.NET Secret Manager (`dotnet user-secrets`)**.:

```json
{
  "Jwt": {
    "Issuer": "EventManager",
    "Audience": "EventManagerClient",
    "Expires": 15,
    "Secret": "${JWT_SECRET}"
  }
}
```

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