# Архітектура системи Hive Emulator

## Загальна структура

Система складається з **3 основних рівнів** та **2 інфраструктурних сервісів**:

```
┌─────────────────────────────────────────────────────────────┐
│                    Communication Control API                │
│                    (Порт 8080) - Центральний контролер      │
│  • Управління множиною HiveMind інстансів                  │
│  • Телеметрія від усіх роїв                                 │
│  • Проксі-ендпоінти до HiveMind                              │
│  • Публікація в Redis для BMS                               │
└───────────────────────┬─────────────────────────────────────┘
                        │ HTTP
                        │
        ┌───────────────┴───────────────┐
        │                               │
┌───────▼────────┐            ┌────────▼────────┐
│  HiveMind API  │            │    BMS API       │
│  (Порт 5149)   │            │  (Порт 5121)     │
│                │            │                  │
│  "Польовий"    │            │  Battlefield     │
│  сервіс        │            │  Management      │
│  для одного    │            │  System          │
│  рою дронів    │            │                  │
│                │            │  • Зберігання     │
│  • Топологія   │            │    телеметрії    │
│  • Mesh-мережа │            │  • EW зони       │
│  • Команди     │            │  • Статуси роїв  │
│  • Дрони       │            │                  │
└────────────────┘            └────────┬─────────┘
                                       │
                                       │ Redis Pub/Sub
                                       │
                              ┌────────▼────────┐
                              │     Redis       │
                              │  (Порт 6379)    │
                              │                 │
                              │  Message Bus    │
                              └─────────────────┘
                                       │
                                       │
                              ┌────────▼────────┐
                              │   PostgreSQL   │
                              │  (Порт 5432)   │
                              │                 │
                              │  BMS Database   │
                              └─────────────────┘
```

---

## Компоненти системи

### 1. **Communication Control API** (Порт 8080)
**Роль:** Центральний координатор всієї системи

**Відповідальність:**
- ✅ Управління множиною HiveMind інстансів (кожен HiveMind обслуговує один рій)
- ✅ Прийом телеметрії від усіх роїв через HiveMind
- ✅ Проксі-ендпоінти для mesh-операцій (створення hive, дронів, топологій)
- ✅ Публікація телеметрії в Redis для BMS
- ✅ Управління інтерференціями та операційними зонами

**Ключові сервіси:**
- `HiveManagementService` - реєстрація/управління роями
- `TelemetryService` - обробка телеметрії
- `HiveMindMeshIntegrationService` - проксі до HiveMind API
- `InterferenceManagementService` - управління перешкодами

**HTTP Endpoints:**
- `POST /api/v1/hive/connect` - підключення HiveMind до системи
- `POST /api/v1/hive/telemetry` - прийом телеметрії
- `POST /api/v1/hivemind/*` - проксі до HiveMind (створення hive, дронів, топологій)

---

### 2. **HiveMind API** (Порт 5149)
**Роль:** "Польовий" сервіс для управління одним роєм дронів

**Відповідальність:**
- ✅ Управління дронами в межах одного рою (hive)
- ✅ Побудова та управління топологіями (mesh, star, dual-star)
- ✅ Аналіз зв'язності рою (connected components, isolated groups)
- ✅ Маршрутизація mesh-команд через найкоротший шлях
- ✅ Емуляція деградації з'єднань між дронами
- ✅ Відправка телеметрії до Communication Control

**Ключові сервіси:**
- `DroneRelayService` - управління дронами та топологіями
- `DroneCommandService` - маршрутизація команд через mesh
- `HiveMindService` - телеметрія та підключення до Communication Control

**In-Memory State (`HiveInMemoryState`):**
- Словник дронів: `Dictionary<string, Drone>`
- Словник hive: `Dictionary<string, Hive>`
- Маппінг дронів до hive: `Dictionary<string, HashSet<string>>`
- Entry relays для mesh: `Dictionary<string, HashSet<string>>`
- Черги команд для дронів: `Dictionary<string, Queue<DroneCommand>>`

**HTTP Endpoints:**
- `POST /api/v1/hives` - створення hive
- `POST /api/v1/drones/batch` - масове створення дронів
- `POST /api/v1/hives/{hiveId}/drones/batch-join` - додавання дронів до рою
- `POST /api/v1/hives/{hiveId}/topology/rebuild` - перебудова топології
- `POST /api/v1/hives/{hiveId}/topology/connect-hivemind` - реєстрація entry relays
- `GET /api/v1/hives/{hiveId}/topology/connectivity` - аналіз зв'язності
- `POST /api/v1/hives/{hiveId}/drones/{droneId}/commands/mesh` - відправка команди через mesh

---

### 3. **BMS API** (Порт 5121)
**Роль:** Система управління полем бою (Battlefield Management System)

**Відповідальність:**
- ✅ Зберігання телеметрії в PostgreSQL
- ✅ Відстеження статусів роїв
- ✅ Управління EW (Electronic Warfare) зонами
- ✅ Прослуховування Redis для отримання телеметрії

**Ключові сервіси:**
- `TelemetryProcessor` - обробка телеметрії з Redis
- `CurrentStatusService` - поточні статуси роїв
- `EwZoneService` - управління EW зонами

**Background Service:**
- `TelemetryListenerBackgroundService` - слухає Redis канал `HiveChannel` і обробляє телеметрію

---

## Потоки даних

### 1. **Ініціалізація системи**

```
1. HiveMind API стартує
   ↓
2. Викликає ConnectHive() → POST http://communication-control-api:8080/api/v1/hive/connect
   ↓
3. Communication Control реєструє HiveMind
   ↓
4. HiveMind запускає таймер телеметрії (кожні 5 секунд)
```

### 2. **Потік телеметрії**

```
HiveMind API (кожні 5 сек)
   ↓
POST /api/v1/hive/telemetry → Communication Control API
   ↓
Communication Control:
   • Оновлює статус рою
   • Публікує в Redis (канал "HiveChannel")
   ↓
BMS API (TelemetryListenerBackgroundService)
   • Слухає Redis
   • Зберігає в PostgreSQL
   • Оновлює HiveStatuses та TelemetryHistory
```

### 3. **Створення рою та дронів**

```
Користувач → Communication Control API
   POST /api/v1/hivemind/hives
   ↓
Communication Control → HiveMind API (проксі)
   POST /api/v1/hives
   ↓
HiveMind створює Hive в HiveInMemoryState

Користувач → Communication Control API
   POST /api/v1/hivemind/drones/batch
   ↓
Communication Control → HiveMind API (проксі)
   POST /api/v1/drones/batch
   ↓
HiveMind додає дронів в HiveInMemoryState._drones

Користувач → Communication Control API
   POST /api/v1/hivemind/hives/{hiveId}/drones/batch-join
   ↓
Communication Control → HiveMind API (проксі)
   POST /api/v1/hives/{hiveId}/drones/batch-join
   ↓
HiveMind додає дронів до hive в HiveInMemoryState._hiveDrones
```

### 4. **Побудова топології**

```
Користувач → Communication Control API
   POST /api/v1/hivemind/hives/{hiveId}/topology/rebuild
   Body: { "topologyType": "mesh", "defaultWeight": 0.8 }
   ↓
Communication Control → HiveMind API (проксі)
   POST /api/v1/hives/{hiveId}/topology/rebuild
   ↓
HiveMind.DroneRelayService.RebuildTopology():
   • Отримує всіх дронів з hive
   • Будує топологію (mesh/star/dual-star)
   • Оновлює Connections у кожного дрона
   • Зберігає в HiveInMemoryState._drones
```

### 5. **Відправка mesh-команди**

```
Користувач → HiveMind API
   POST /api/v1/hives/{hiveId}/drones/{targetDroneId}/commands/mesh?minWeight=0.5
   ↓
HiveMind.DroneCommandService.SendMeshCommand():
   1. Отримує entry relays для hive (з HiveInMemoryState._hiveEntryRelays)
   2. Запускає BFS від entry relays до targetDroneId
   3. Знаходить найкоротший шлях з мінімальною вагою >= minWeight
   4. Створює DroneCommand для кожного реле в маршруті
   5. Додає команди в черги дронів (HiveInMemoryState._droneCommands)
   6. Повертає інформацію про маршрут (routePath, routeWeights, hopCount)
```

### 6. **Аналіз зв'язності**

```
Користувач → Communication Control API
   GET /api/v1/hivemind/hives/{hiveId}/topology/connectivity
   ↓
Communication Control → HiveMind API (проксі)
   GET /api/v1/hives/{hiveId}/topology/connectivity
   ↓
HiveMind.DroneRelayService.AnalyzeConnection():
   1. Отримує всіх дронів з hive
   2. Будує граф з'єднань (враховуючи ваги > 0)
   3. Запускає BFS для знаходження connected components
   4. Обчислює:
      - isFullyConnected
      - connectedComponents
      - isolatedGroups
      - largestComponentSize
   5. Повертає SwarmConnectivityResponse
```

---

## Чому розділено Communication Control та HiveMind?

### **Communication Control** (Центральний контролер)
- **Масштабування:** Може управляти множиною HiveMind інстансів (кожен обслуговує один рій)
- **Агрегація:** Збирає телеметрію від усіх роїв в одному місці
- **Оркестрація:** Координує взаємодію між роями та зовнішніми системами (BMS)

### **HiveMind** (Польовий сервіс)
- **Фокус:** Управління одним роєм дронів
- **Локальність:** In-memory state для швидкого доступу до топології та команд
- **Автономність:** Може працювати незалежно, навіть якщо Communication Control тимчасово недоступний
- **Спеціалізація:** Оптимізований для mesh-маршрутизації та аналізу топології

**Аналогія:** 
- Communication Control = "Штаб" (координує всі рої)
- HiveMind = "Командир рою" (управляє конкретним роєм)

---

## Технологічний стек

- **.NET 8** - всі API сервіси
- **PostgreSQL** - база даних для BMS
- **Redis** - message bus для телеметрії
- **Docker Compose** - оркестрація контейнерів
- **Entity Framework Core** - ORM для BMS
- **Polly** - retry policies для HTTP клієнтів

---

## Ключові концепції

### **Hive (Рій)**
- Група дронів, які працюють разом
- Кожен HiveMind інстанс обслуговує один Hive
- Hive має унікальний ID

### **Drone (Дрон)**
- Може бути типу: `Scout`, `Striker`, або `Relay`
- Має з'єднання з іншими дронами (з вагою 0.0-1.0)
- Може належати лише одному Hive одночасно

### **Connection Weight (Вага з'єднання)**
- `0.0` = з'єднання не існує (видалено)
- `0.1-0.9` = деградоване з'єднання
- `1.0` = ідеальне з'єднання
- Використовується для вибору найкращого маршруту в mesh

### **Entry Relays**
- Реле-дрони, через які HiveMind починає маршрутизацію команд
- Реєструються через `POST /api/v1/hives/{hiveId}/topology/connect-hivemind`
- Не мають спеціальних прямих з'єднань до всіх дронів

### **Mesh Routing**
- BFS (Breadth-First Search) для знаходження найкоротшого шляху
- Враховує мінімальну вагу з'єднань (`minWeight`)
- Створює relay-команди для проміжних дронів

---

## Приклад повного циклу

1. **Створення рою:**
   ```bash
   POST http://localhost:8080/api/v1/hivemind/hives
   Body: { "id": "hive-001", "name": "Alpha Swarm" }
   ```

2. **Створення дронів:**
   ```bash
   POST http://localhost:8080/api/v1/hivemind/drones/batch
   Body: { "drones": [ { "id": "drone-001", "type": "Relay", ... }, ... ] }
   ```

3. **Додавання дронів до рою:**
   ```bash
   POST http://localhost:8080/api/v1/hivemind/hives/hive-001/drones/batch-join
   Body: { "droneIds": ["drone-001", "drone-002", ...] }
   ```

4. **Побудова топології:**
   ```bash
   POST http://localhost:8080/api/v1/hivemind/hives/hive-001/topology/rebuild
   Body: { "topologyType": "mesh", "defaultWeight": 0.8 }
   ```

5. **Реєстрація entry relays:**
   ```bash
   POST http://localhost:8080/api/v1/hivemind/hives/hive-001/topology/connect-hivemind
   Body: { "entryRelayIds": ["drone-001", "drone-002"] }
   ```

6. **Відправка команди через mesh:**
   ```bash
   POST http://localhost:5149/api/v1/hives/hive-001/drones/drone-010/commands/mesh?minWeight=0.5
   ```

7. **Перевірка зв'язності:**
   ```bash
   GET http://localhost:8080/api/v1/hivemind/hives/hive-001/topology/connectivity
   ```

---

## Важливі деталі реалізації

### **In-Memory State (HiveMind)**
- Всі дані про дронів, hive та команди зберігаються в пам'яті
- Thread-safe через `lock` statements
- Не персистуються між перезапусками

### **Телеметрія**
- HiveMind відправляє телеметрію кожні 5 секунд
- Використовує поточний `HiveId` з `HiveInMemoryState`
- Communication Control публікує в Redis для BMS

### **Mesh Routing Algorithm**
- BFS від entry relays до target drone
- Враховує `minWeight` для фільтрації слабких з'єднань
- Повертає найкоротший шлях з найкращою якістю

### **Connection Degradation**
- `weight = 0` означає видалення з'єднання
- Оновлює обидва напрямки (bidirectional)
- Впливає на mesh routing та connectivity analysis

