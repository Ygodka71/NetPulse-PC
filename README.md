# NetPulse — Smart Speed & Network Test for Windows

**NetPulse for Windows** is a fast, powerful, and modern desktop application (.NET 10 / WPF) that delivers comprehensive real-time network telemetry, multi-stream internet speed testing, and connection diagnostics wrapped in a sleek dark cyber-neon interface.

---

## What the App Can Do

### Internet Speed Test
- **Multi-Stream Download & Upload**: Measures real-world data download and upload throughput via high-capacity CDN nodes.
- **Animated Speedometer**: Smooth 240-degree neon gauge animation displaying current throughput and peak bandwidth in real time.
- **Automatic Test Phases**: Clear phase transitions (*Ready*, *Ping Check*, *Downloading*, *Uploading*, *Completed*).

### Real-Time Network Pulse & Diagnostics
- **Live Waveform Graph**: Continuous real-time latency visualization with an interactive canvas, grid lines, and live pulse monitoring.
- **Microsecond Ping Measurement**: Direct low-overhead socket response tracking with Stopwatch precision and HTTP fallback.
- **Jitter (Latency Variation)**: Real-time calculation of packet latency variation — vital for competitive gaming, Discord, and Zoom calls.
- **Packet Loss Tracker**: Tracks network packet loss percentage during interference, complete with a clean package icon and an instant reset button.
- **Pause & Resume**: Easily pause real-time pulse monitoring when not needed and resume with a single click.

### Gaming & Call Quality Rating (0–100)
- **Smart Connection Score**: Evaluates overall line stability on a 100-point scale with instant badges (*Excellent*, *Good*, *Moderate*, *Poor*).
- **Practical Verdicts**: Immediately tells you if your connection is optimal for 4K Ultra HD streaming, competitive FPS gaming, or video conferences.
- **Detailed Diagnostic Modal**: Click the score badge to view full breakdown tables (min/max/peak stats, hardware interface, server node) and copy a ready-to-share diagnostic report to your clipboard.

### Public IP, Geolocation & Hardware Adapter Info
- **Complete IP View**: Tap the info icon ("?") to inspect your full outward-facing IP address — long IPv6 addresses are shown in full without truncation.
- **One-Click Copy**: Instant button to copy your IP or network details to the clipboard.
- **Country & ISP**: Automatically resolves geolocation with country name and ISP / organization name.
- **Local Network Hardware**: Displays physical connection type (Ethernet, Wi-Fi), internal gateway IP, and hardware link speed (e.g. 1000 Mbps).

### Modular Screen Customization (Edit Mode)
- **Pencil Edit Mode**: Click the pencil icon to customize your dashboard layout.
- **Hide & Reorder Cards**: Hide unnecessary metrics with "✕" and reorder indicators using "←" and "→" navigation buttons.
- **Bottom Dock Tray**: Restore hidden metrics at any time from the bottom chips bar.
- **Auto-Persistence**: Screen arrangements and preferences are automatically saved to `%LocalAppData%\NetPulse\settings.json`.

### Adaptive Layout & UI Scaling
- **Responsive Multi-Column Grid**: Indicators seamlessly re-arrange between 1, 2, 3, or 4 columns depending on window width.
- **Interface Scale Control**: Choose your preferred UI scale in Settings from **80% to 150%** — includes a custom neon slider, "−"/"＋" step buttons (5%), and 6 instant preset buttons (80%, 90%, 100%, 115%, 130%, 150%).
- **High-DPI & 4K Ready**: Vector graphics and typography scale crisply across all display resolutions without blur.
- **Frameless Window Controls**: Custom title bar with drag support, maximize/restore with multi-monitor taskbar boundaries, and smooth resize handles.

### Windows System Tray & Background Telemetry
- **Runs in System Tray**: NetPulse minimizes to the Windows taskbar notification area next to the clock.
- **Live Tooltip Telemetry**: Hover over the tray icon to view live ping, jitter, loss, speed, and IP address.
- **Tray Context Menu**: Quick access to open the app, start a speed test, toggle pulse, or exit.
- **Customizable Telemetry**: Toggle individual telemetry metrics in Settings to tailor what appears in tray tooltips.

### Custom Servers, History & Themes
- **Preset & Custom Ping Targets**: Switch between Cloudflare Edge, Google DNS, Quad9, or add your own custom servers (host + port) for private or gaming servers.
- **Test History Log**: Retains past measurement records with timestamps, speeds, ping, and quality grades; individually deletable or clearable in bulk.
- **Theme Modes**: Supports Dark (Cyber-Telemetry), Light, and System follow modes.
- **Bilingual Interface**: Instant switching between Russian and English languages.

---
---

# NetPulse — Умный тест скорости и сети для Windows

**NetPulse для Windows** — это быстрое, мощное и красивое приложение для Windows (.NET 10 / WPF), предоставляющее полную телеметрию сетевого соединения в реальном времени, многопоточный тест скорости интернета и комплексную диагностику стабильности сети в фирменном кибернеоновом интерфейсе.

---

## Что умеет приложение

### Замер скорости интернета
- **Многопоточное скачивание и отдача (Download & Upload)**: Точное измерение реальной пропускной способности канала через высокоскоростные узлы CDN.
- **Анимированный спидометр**: Плавная 240-градусная неоновая шкала с живой анимацией стрелки, текущей и пиковой скорости.
- **Стадии тестирования**: Автоматическое и наглядное переключение этапов (*«Готов к тесту»*, *«Проверка пинга»*, *«Скачивание»*, *«Отдача»*, *«Завершено»*).

### Живой пульс сети и диагностика
- **График задержки (Network Pulse)**: Интерактивный холст с живой градиентной волной отклика, координатной сеткой и пульсирующим маркером онлайна.
- **Высокоточный пинг**: Прямое измерение отклика сокетов с субмиллисекундной точностью таймера `Stopwatch` и HTTP-фоллбэком.
- **Джиттер (стабильность задержки)**: Расчет колебания времени отклика между последовательными пакетами — ключевой показатель для игр, Discord и видеосвязи.
- **Учёт потерь пакетов**: Подсчёт процента потерянных пакетов при сбоях и помехах с нейтральным значком пакета и кнопкой мгновенного сброса.
- **Пауза и возобновление**: Возможность приостановить фоновый мониторинг пинга в один клик и возобновить его при необходимости.

### Оценка качества сети для игр и звонков (0–100)
- **Умный рейтинг соединения**: Выставляет общую оценку качеству линии по 100-балльной шкале со значками (*«Превосходно»*, *«Отлично»*, *«Хорошо»*, *«Базовое»*, *«Нестабильно»*).
- **Практические рекомендации**: Подсказывает, подходит ли текущее качество связи для 4K Ultra HD стриминга, онлайн-игр без микрофризов или рабочих видеоконференций.
- **Детальный отчёт**: Нажатие на баннер оценки открывает карточку с подробными показателями (минимум/пик/среднее, сетевой адаптер, сервер) и кнопкой быстрого копирования отчёта для отправки в техподдержку.

### Внешний IP-адрес, геолокация и сетевой адаптер
- **Полный просмотр IP**: Нажатие на значок «?» открывает всплывающее окно, где отображается полный IP-адрес — даже длинные адреса IPv6 видны целиком без обрезки.
- **Копирование в буфер**: Кнопка для копирования IP-адреса и сетевых данных в один клик.
- **Страна и провайдер**: Автоматическое определение страны подключения и имени интернет-провайдера (ISP).
- **Локальный адаптер**: Отображение типа подключения (Ethernet, Wi-Fi), локального IP-адреса и аппаратной скорости линка (например, 1000 Мбит/с).

### Модульные карточки и настройка экрана (Режим редактирования)
- **Режим настройки по кнопке карандаша**: Удобное переключение в режим персонализации рабочего пространства.
- **Скрытие и сортировка**: Скрывайте ненужные показатели кнопкой «✕» и меняйте их порядок стрелками «←» и «→».
- **Нижний док добавления**: Скрытые карточки отображаются в нижней панели, откуда их можно вернуть на экран в любое время.
- **Автосохранение**: Все персональные настройки экрана сохраняются в `%LocalAppData%\NetPulse\settings.json`.

### Адаптивный интерфейс и масштабирование (UI Scale)
- **Адаптивная сетка карточек**: Показатели автоматически перестраиваются в 1, 2, 3 или 4 колонки в зависимости от ширины окна.
- **Регулировка масштаба интерфейса**: Возможность изменить масштаб UI от **80% до 150%** прямо в настройках — с помощью неонового ползунка, кнопок точной подстройки «−»/«+» (шаг 5%) и 6 готовых пресетов (80%, 90%, 100%, 115%, 130%, 150%).
- **Поддержка High-DPI и 4K**: Векторная графика и шрифты масштабируются без размытия на мониторах любого разрешения.
- **Безрамочное окно**: Кастомный заголовок с поддержкой перетаскивания, разворачивание с учётом панели задач на нескольких мониторах и рамки изменения размера.

### Системный трей Windows и фоновая телеметрия
- **Работа в трее**: Приложение сворачивается в область уведомлений Windows рядом с часами.
- **Живая подсказка в трее**: При наведении курсора на значок в трее отображается всплывающая подсказка с текущим пингом, джиттером, потерями, скоростью и IP.
- **Контекстное меню трея**: Быстрый запуск теста скорости, открытие окна, пауза мониторинга или выход.
- **Гибкая настройка**: В настройках можно включать и отключать показ отдельных параметров в трее.

### Свои серверы, история и темы
- **Выбор и добавление серверов**: Предустановленные быстрые серверы (Cloudflare, Google, Quad9) плюс возможность добавить свой хост и порт для проверки пинга до личных или игровых серверов.
- **История замеров**: Сохранение результатов всех тестов с датой, временем, скоростью, задержкой и оценкой; доступно удаление отдельных записей или полная очистка.
- **Темы оформления**: Тёмная (Cyber-Telemetry), Светлая и Системная.
- **Два языка**: Мгновенное переключение между русским и английским языками интерфейса.
