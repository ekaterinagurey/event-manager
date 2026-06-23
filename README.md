# Event Manager API

REST API для управления мероприятиями

# Структура проекта

Controllers/	- http endpoins
DTOs/			- модель запроса на создание и редактирование
Interfaces/		- интерфейсы сервиса
Mappers/		- преобразование DTO к модели
Models/			- длменные модели
Services/		- бизнес логика

# Запуск проекта

## Собрать проект
dotnet build EventManager\EventManager\EventManager.csproj -c Debug 

## Запустить приложение
dotnet run --project EventManager\EventManager\EventManager.csproj 

## После запуска приложение будет доступно по адресу:
http://localhost:<port>

# Запуск тестов

Для запуска всех тестов выполните:

dotnet test EventManager\EventManager\EventManager.csproj 

# Swagger

Swagger UI доступен по адресу:
https://localhost:<port>/swagger

# API

## Получить список мероприятий
GET /events

### Параметры фильтрации и пагинации

| Параметр   | Тип      | Описание                                                      |
| ---------- | -------- | ------------------------------------------------------------- |
| title    | string   | Поиск по названию (регистронезависимый, частичное совпадение) |
| from     | DateTime | События, начинающиеся не раньше указанной даты        |
| to       | DateTime | События, заканчивающиеся не позже указанной даты      |
| page     | int      | Номер страницы (по умолчанию 1)                               |
| pageSize | int      | Количество элементов на странице (по умолчанию 10)            |


## Получить мероприятие по идентификатору
GET /events/{id}

Создать мероприятие
POST /events
Тело запроса:
{
  "id": 1,
  "title": "event1",
  "description": "string",
  "startAt": "2026-06-14T21:47:09.316Z",
  "endAt": "2026-06-15T21:47:09.316Z"
}

## Обновить мероприятие
PUT /events/{id}

## Удалить мероприятие
DELETE /events/{id}


## Формат ошибок

### 400 Bad Request

Возвращается при ошибках валидации.

{
  "title": "Bad request",
  "status": 400,
  "detail": "Validation failed"
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