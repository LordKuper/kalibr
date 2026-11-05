# Сборка и развёртывание

Образы собираются вне контура, на машине с интернетом, и переносятся в контур готовыми: запущенный сервис ничего не скачивает. Манифесты — plain YAML (без Kustomize и Helm). Ingress и TLS манифесты не задают.

| Файл | Назначение |
|---|---|
| `Dockerfile`, `.dockerignore` (корень репозитория) | образ `app`: SPA (с шрифтами PT) и API, непривилегированный пользователь `app` (UID 1654), порт 8080 |
| `deploy/k8s/configmap.yaml` | несекретная конфигурация `app` и `db`, скрипт `db-init`, создающий роль БД приложения |
| `deploy/k8s/db.yaml` | Service и StatefulSet `db` на официальном образе `postgres:18.6-trixie` (не пересобирается) |
| `deploy/k8s/app.yaml` | Service, PVC ключей DataProtection и Deployment `app` |
| `deploy/secret.template.yaml` | шаблон Secret с плейсхолдерами; лежит выше `deploy/k8s`, чтобы `kubectl apply -f deploy/k8s/` не создал Secret с плейсхолдерами |

## 1. Сборка образов (вне контура)

Нужен Docker с BuildKit. Образ платформы `linux/amd64` получается на любом хосте.

1. Определить digest базовых образов (строка `Digest:` в выводе):

   ```
   docker buildx imagetools inspect node:24.21.0-trixie-slim
   docker buildx imagetools inspect mcr.microsoft.com/dotnet/sdk:10.0.401
   docker buildx imagetools inspect mcr.microsoft.com/dotnet/aspnet:10.0.12-noble
   docker buildx imagetools inspect postgres:18.6-trixie
   ```

2. Собрать `app` из корня репозитория, подставив digest вместо `<DIGEST>`:

   ```
   docker build \
     --build-arg NODE_IMAGE=node:24.21.0-trixie-slim@sha256:<DIGEST> \
     --build-arg SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0.401@sha256:<DIGEST> \
     --build-arg RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:10.0.12-noble@sha256:<DIGEST> \
     -t kalibr-app:<VERSION> .
   ```

   `<VERSION>` — версия выпуска, она же `Version` в `Directory.Build.props`; первый выпуск — `1.0.0`, образ `kalibr-app:1.0.0`.

   Без `--build-arg` берутся теги — только для локальных проверок. Версии SDK и runtime меняются парой (`10.0.401` ↔ `10.0.12`), цикл патчей .NET — ежемесячно.

3. Просканировать `kalibr-app` и `postgres` на уязвимости, сформировать SBOM (инструменты стеком не заданы) и приложить результаты к поставке.

## 2. Перенос в контур

- Есть внутренний registry: `docker push`, узлы забирают образы сами.
- Нет registry: `docker save -o kalibr-images.tar kalibr-app:<VERSION> postgres:18.6-trixie`, перенос архива, импорт на каждом узле, где может запуститься под (для containerd — `ctr -n k8s.io images import kalibr-images.tar`). `imagePullPolicy: IfNotPresent` — pull не требуется.

Digest в манифестах: `postgres:18.6-trixie@sha256:<DIGEST>` в `db.yaml` и `kalibr-app:<VERSION>@sha256:<DIGEST>` в `app.yaml`. Digest у собранного локально образа появляется после `docker push`; при tar-доставке соответствие digest после импорта не проверено — сверить на узле (`crictl images --digests`), а если digest недоступен, оставить ссылку по тегу без `@sha256:<DIGEST>`.

## 3. Применение

Реальный Secret создаёт оператор; значения в репозиторий не попадают.

```
kubectl create namespace kalibr
cp deploy/secret.template.yaml <путь вне репозитория>/kalibr-secret.yaml
# заполнить плейсхолдеры, затем:
kubectl -n kalibr apply -f <путь вне репозитория>/kalibr-secret.yaml
# заполнить плейсхолдеры в deploy/k8s/configmap.yaml (почта и публичный адрес), затем:
kubectl -n kalibr apply -f deploy/k8s/
kubectl -n kalibr rollout status statefulset/db
kubectl -n kalibr rollout status deployment/app
```

Поля Secret `kalibr-secret`:

- Две учётные записи БД. `POSTGRES_PASSWORD` — пароль суперпользователя кластера `postgres`, только для обслуживания; `app` его не получает. `KALIBR_DB_PASSWORD` — пароль роли `kalibr` без прав суперпользователя (`LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE`), владельца базы `kalibr`; роль создаёт скрипт `db-init` (`10-app-role.sh`) при первой инициализации тома. Пароли разные, без `;` и кавычек.
- `ConnectionStrings__Default` — строка подключения `app` от имени роли `kalibr` с паролем `KALIBR_DB_PASSWORD`; `Database` совпадает с `POSTGRES_DB` из `db-config`; `GSS Encryption Mode=Disable` из шаблона оставить (причина — в комментарии шаблона). Пароли применяются при первой инициализации тома: позже Secret их не меняет; на работающей БД пароль меняют вручную (`ALTER ROLE` от имени `postgres`) и в Secret.
- `Bootstrap__AdminEmail`, `Bootstrap__AdminPassword` — первый глобальный администратор: его e-mail и пароль задаются при развёртывании так же, как пароль БД, и в репозиторий не попадают. E-mail одновременно служит именем для входа (отдельного имени пользователя нет), должен быть корректным адресом не длиннее 254 символов. Пароль подчиняется политике Identity: не короче 10 символов, цифра, строчная и заглавная буква, неалфавитно-цифровой символ.

- `Smtp__UserName`, `Smtp__Password` — учётная запись SMTP relay. Оба ключа задаются вместе; если relay принимает почту без аутентификации, оба удаляются из Secret (`optional: true`).

Почта и публичный адрес в `app-config` (`deploy/k8s/configmap.yaml`):

- `App__PublicBaseUrl` — адрес, по которому пользователи открывают приложение (например, `https://kalibr.corp.example`); с него начинаются все ссылки в письмах, заголовок `Host` запроса на них не влияет.
- `Smtp__Host`, `Smtp__Port` — корпоративный SMTP relay; `Smtp__From` — адрес отправителя, можно с именем: `Имя <адрес>`.
- `Smtp__SecureSocketOptions` — защита соединения: `StartTls` (порт 587, значение по умолчанию), `SslOnConnect` (порт 465), `StartTlsWhenAvailable`, `Auto`, `None`. `None` и `StartTlsWhenAvailable` допускают отправку без шифрования — только для перехватчика почты в разработке.
- `Smtp__Timeout` — предел одной отправки (по умолчанию `00:00:15`): письма по действиям администратора уходят сразу после сохранения, и его запрос ждёт отправку не дольше этого времени.
- Необязательные ключи (значения по умолчанию заданы в образе): `AccountLinks__InvitationLifetime` — срок ссылки-приглашения (`7.00:00:00`); `AccountLinks__PasswordResetLifetime` — срок ссылки сброса пароля (`01:00:00`); `AccountLinks__PasswordResetInterval` — не чаще какого интервала запрос «Не помню пароль» выдаёт учётной записи новую ссылку (`00:05:00`, более частые запросы молча пропускаются); `RateLimiting__PasswordReset__PermitLimit` и `RateLimiting__PasswordReset__WindowSeconds` — лимит запросов с одного адреса (`10` за `60` с), общий для «Не помню пароль», сброса пароля по ссылке и принятия приглашения; `Smtp__CheckCertificateRevocation` — проверка отзыва сертификата relay (`true`), см. «Доверие внутреннему CA».

Доверие внутреннему CA (шаг развёртывания): отправка по `StartTls`/`SslOnConnect` завершается ошибкой TLS (`SslHandshakeException` в журнале) в двух случаях.

- CA не доверен: сертификат relay выпущен внутренним CA, которому образ не доверяет. Смонтировать в под файл PEM, в котором системные корневые сертификаты дополнены сертификатом внутреннего CA (например, из ConfigMap), и указать путь к нему в переменной `SSL_CERT_FILE` контейнера `app`.
- CA доверен, но точка распространения CRL/OCSP внутреннего CA недоступна из пода: проверка отзыва сертификата не проходит. Сначала обеспечить поду доступ к CRL/OCSP. Если это невозможно, оператор явно принимает решение и задаёт в `app-config` `Smtp__CheckCertificateRevocation: "false"`: отключается только проверка отзыва, сертификат и цепочка по-прежнему проверяются, но отозванный сертификат relay будет принят.

Отключать проверку сертификата нельзя. После развёртывания проверить отправку: например, отправить приглашение на свой адрес.

Доступ без Ingress, для проверки: `kubectl -n kalibr port-forward svc/app 8080:8080` и `curl http://localhost:8080/healthz/ready`.

## 4. Поведение при старте

- `app` применяет схему сам (`MigrateAsync`) ролью `kalibr` из `ConnectionStrings__Default`: она не суперпользователь, но владеет базой, поэтому DDL-права у неё есть; расширение `pg_trgm` (trusted) создаёт миграция под этой ролью. Порт открывается после миграций, поэтому `startupProbe` ждёт их; liveness — `/healthz/live`, readiness — `/healthz/ready` (проверяет БД).
- До миграций `app` проверяет настройки почты и публичного адреса: не задан или неверен `Smtp__Host`, `Smtp__Port`, `Smtp__SecureSocketOptions` (не одно из перечисленных выше значений), `Smtp__From`, `Smtp__Timeout` (не положительный или больше `49.17:02:47.294`), `App__PublicBaseUrl` (не абсолютный http(s) URL) или задан только один из `Smtp__UserName`/`Smtp__Password` — старт завершается ошибкой, в которой названы неверные ключи (логин и пароль SMTP в журнал не попадают). Доступность relay при старте не проверяется: отказ отправки виден только при отправке письма. Существующее развёртывание при обновлении до этой версии не стартует, пока эти ключи не заполнены в `app-config`.
- Kubernetes не гарантирует порядок запуска: пока `db` не готова, процесс `app` завершается и перезапускается — на первом развёртывании возможны рестарты.
- Если активного глобального администратора нет, `app` создаёт его из `Bootstrap__AdminEmail` и `Bootstrap__AdminPassword`; существующего не перезаписывает. Нет администратора и нет одного из ключей — старт завершается ошибкой, в которой назван недостающий ключ (пароль в журнал не попадает). После первого входа ключи `Bootstrap__*` можно убрать из Secret (`optional: true`).
- Первая установка — версия 1.0.0 на пустой том: `db-init` создаёт роль `kalibr`, `MigrateAsync` применяет базовую миграцию `InitialCreate`, администратор создаётся из `Bootstrap__*`. Контракт API остаётся версии 1.0.0 (`info.version`, путь `/api/v1`). Дальше схема меняется только вперёд, инкрементальными миграциями с переносом данных поверх `InitialCreate`; она не пересоздаётся. Порядок обновления — в разделе 5.
- Единственный переход старых баз: базы, развёрнутые до 1.0.0, пересоздаются, данные не переносятся. В `__EFMigrationsHistory` такой базы лежат миграции прежней линии (`20261021142744_InitialCreate`, миграции раньше `20261021091246_InitialCreate` и последующие), которых в 1.0.0 нет: `MigrateAsync` пытается заново создать существующие таблицы, и старт `app` завершается ошибкой. Новая база заодно получает роль `kalibr` от `db-init`, поэтому Secret должен быть заполнен по новому шаблону (`KALIBR_DB_PASSWORD`, `ConnectionStrings__Default`) до пересоздания. Пересоздание: `kubectl -n kalibr scale deployment/app --replicas=0`, `kubectl -n kalibr apply -f <путь вне репозитория>/kalibr-secret.yaml`, `kubectl -n kalibr delete statefulset db`, `kubectl -n kalibr delete pvc data-db-0`, затем снова `kubectl -n kalibr apply -f deploy/k8s/`. Все данные, включая журнал `audit`, удаляются; администратор создаётся заново из `Bootstrap__*`, поэтому эти ключи должны быть в Secret. Все пользователи выходят из сессии, ссылки сброса пароля и приглашения недействительны.
- Установка со старыми именами (`competency`) не переносится: Kalibr ставится заново.
- Ключи DataProtection лежат на PVC `app-keys` (путь `DataProtection__KeysPath`); пока том жив, cookie-сессии переживают рестарт. `strategy: Recreate`, одна реплика: том `ReadWriteOnce`, а миграции не должны идти из двух подов.
- Данные PostgreSQL — на PVC `data-db-0`, смонтированном строго в `/var/lib/postgresql`; каталог данных не переопределяется.

## 5. Обновление и откат

Версия выпуска — `<VERSION>` из раздела 1; каждый следующий выпуск приносит `app` с новыми миграциями, которые применяются при его старте. Миграции идут только вперёд, обратных нет: откат — это восстановление резервной копии БД и прежний образ. Автоматического резервного копирования нет (Q5, см. «Открытые точки»), копию делает оператор.

Порядок обновления:

1. Резервная копия `pg_dump` до любых изменений в кластере; копию хранить вне кластера и вне PVC:

   ```
   kubectl -n kalibr exec statefulset/db -- sh -c 'pg_dump --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --format=custom' > <путь вне репозитория>/kalibr-<ДАТА>.dump
   ```

2. Собрать, просканировать и перенести новые образы (разделы 1 и 2).
3. Подставить в `app.yaml` новые версию и digest `kalibr-app:<VERSION>`, затем `kubectl -n kalibr apply -f deploy/k8s/` и `kubectl -n kalibr rollout status deployment/app`. Из-за `strategy: Recreate` `app` недоступен, пока меняется под и идут миграции.
4. Проверить `curl http://localhost:8080/healthz/ready` через `port-forward` (раздел 3) и вход в приложение.

Откат, если `app` не стартует или обновление принято неудачным:

1. Остановить `app`: `kubectl -n kalibr scale deployment/app --replicas=0`.
2. Пересоздать базу из копии (данные, записанные после копии, теряются):

   ```
   kubectl -n kalibr exec statefulset/db -- sh -c 'dropdb --username "$POSTGRES_USER" --force "$POSTGRES_DB" && createdb --username "$POSTGRES_USER" --owner kalibr "$POSTGRES_DB"'
   kubectl -n kalibr exec -i statefulset/db -- sh -c 'pg_restore --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" --exit-on-error' < <путь вне репозитория>/kalibr-<ДАТА>.dump
   ```

3. Вернуть в `app.yaml` прежние версию и digest образа и выполнить `kubectl -n kalibr apply -f deploy/k8s/`; прежний образ стартует на восстановленной схеме.

## 6. Проверка манифестов (Manual verification)

`kubectl apply --dry-run=client -f deploy/k8s/` и `kubectl apply --dry-run=client -f deploy/secret.template.yaml` — команде нужен доступ к API-серверу (схема и discovery), без кластера она не выполняется. Результат: все объекты `created (dry run)`, ошибок нет.

## Открытые точки

TLS, резервное копирование и доставка образов — открытые вопросы, их ведёт `docs/architecture/stack.html`.

- TLS: Ingress не задан. `Authentication__Cookie__SecurePolicy` в `app-config` равен `SameAsRequest`; когда TLS заканчивается перед приложением, поставить `Always`. Заголовки `X-Forwarded-*` приложение не разбирает: за прокси лимиты входа и сброса пароля (`RateLimiting:Login`, `RateLimiting:PasswordReset`) считаются по адресу прокси, а не клиента.
- Q5 (RPO/RTO, способ и расписание резервного копирования) открыт, его ведёт `docs/architecture/stack.html`. Автоматического копирования и манифестов бэкапа нет: есть только ручная копия `pg_dump` перед обновлением (раздел 5); без неё PVC `data-db-0` и `app-keys` — единственные копии данных.
- Доставка образов (registry или tar) не выбрана.
- Учётные записи БД: `app` подключается ролью `kalibr` без прав суперпользователя, владельцем базы `kalibr`; суперпользователь `postgres` (`POSTGRES_USER`) нужен только для обслуживания и приложению не передаётся. Роль не может переключить `session_replication_role`, а триггеры неизменяемости журнала включены как `ENABLE ALWAYS`. Как владелец, она выполняет DDL, в том числе `DROP TRIGGER` и `DISABLE TRIGGER`, поэтому от DDL журнал не защищён — остаточный риск остаётся открытым. Расширение `pg_trgm` помечено как trusted, и роль-владелец БД без прав суперпользователя создаёт его сама (проверено).
- `requests`/`limits`, StorageClass и NetworkPolicy не заданы — их определяет кластер; ёмкости PVC (10Gi у `db`, 1Gi у `app-keys`) — стартовые значения.
- Остановка `db`: образ задаёт `STOPSIGNAL SIGINT`; проверить, что runtime кластера его учитывает. Иначе PostgreSQL получит SIGTERM, будет ждать клиентов, и по истечении `terminationGracePeriodSeconds: 120` под получит SIGKILL с последующим crash recovery.
- Обновление: резервная копия, пересборка, сканирование, перенос, `kubectl apply` с новыми digest (раздел 5). Из-за `Recreate` `app` недоступен на время смены пода.
