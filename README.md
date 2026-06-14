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

Собрать проект
dotnet build EventManager\EventManager\EventManager.csproj -c Debug 

Запустить приложение
dotnet run --project EventManager\EventManager\EventManager.csproj 

После запуска приложение будет доступно по адресу:
http://localhost:<port>

# Swagger

Swagger UI доступен по адресу:
https://localhost:<port>/swagger

# API

Получить список мероприятий
GET /events

Получить мероприятие по идентификатору
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

Обновить мероприятие
PUT /events

Удалить мероприятие
DELETE /events/{id}