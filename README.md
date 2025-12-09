# Hive Emulator

## Швидкий старт

```bash
docker compose up --build
```

## Доступні сервіси

- **Communication Control API** - http://localhost:8080/swagger
- **HiveMind API** - http://localhost:5149/swagger
- **Map Client** - http://localhost:3000

## Основні можливості

- Управління дронами (створення, оновлення, видалення)
- Управління роями (Hive) - групи дронів
- Топології: mesh, star, dual-star
- Mesh-маршрутизація команд через relay дрони
- Аналіз зв'язності рою
- Емуляція деградації зв'язків

Детальна документація API доступна в Swagger UI.
