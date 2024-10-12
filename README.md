# WebAPI для приложения ботанического сада ВятГУ

В данном приложении присутствует API для работы с Бот.Садом.

## Как запустить

### Docker

Для загрузки контейнера использовать:

1. Выполните команду для входа в Docker:
   ```
   docker login
2. Скачайте контейнер для данного проекта:
   ```
   docker push loleenko/botgardenapp-webapi:latest
3. Чтобы запустить приложение, скачайте остальные файлы проекта и создайте в корне проекта файл `docker-compose.yml`
4. Содержимое файла сделать таким:
   ```
   version: '3.8'

   services:
      frontend:
        image: loleenko/botgardenapp-frontend:latest
        ports:
          - "3000:3000"
        environment:
          - NODE_ENV=development
          - REACT_APP_API_URL=http://localhost:5000
        depends_on:
          - webapi
    
      webapi:
        image: loleenko/botgardenapp-webapi:latest
        ports:
          - "5000:8080"
        environment:
          - ConnectionStrings__DefaultConnection=Server=db;Port=5432;Database=BotGarden;User Id=postgres;Password=ezpass1;
        depends_on:
          - backend
    
      backend:
        image: loleenko/botgardenapp-backend:latest
        environment:
          - ConnectionStrings__DefaultConnection=Server=db;Port=5432;Database=BotGarden;User Id=postgres;Password=ezpass1;
        depends_on:
          - db
    
      db:
        image: postgis/postgis:13-3.1
        environment:
          POSTGRES_DB: BotGarden
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: ezpass1
        ports:
          - "5432:5432"
    
   volumes:
      pgdata:
5. Запустить команду в директории с `docker-compose.yml`
```
  docker-compose up --build

