---
responsibility:
  owns: brownfield findings for sprint scope (existing docs, code, gaps incl. dependencies/migration, risks)
  excludes: requirements, decisions, plan, code
  delegates_to: prd.html (requirements), adr.html (decisions), plan.md (tasks)
---

# Audit

## Scope reference
[sprint.md](./sprint.md)

## Touched areas
Затронутых файлов больше порога пакетного чтения (394 при пороге 200): области сначала отобраны поиском, прочитаны совпавшие участки.
- `src/Kalibr.Promotion/ReviewReader.cs`, `ReviewCompetencyResponse.cs`: новое поле карточки кандидата (AC-1, AC-2, AC-4, AC-5).
- `web/src/features/promotion/ChairReviewView.tsx`, `MemberReviewView.tsx`, `ReviewParts.tsx`, `ScaleChoice.tsx`, `tours.ts`, `demo/scenario.tsx`: показ на формах и в демо (AC-3, AC-8).
- `web/src/api/schema.d.ts`, `openapi/openapi.json`: контракт (AC-2, AC-11).
- `src/Kalibr.Platform/Migrations/`: 15 миграций с Designer и снимок модели (AC-9).
- `tests/Kalibr.Tests/IdentityRolesMigrationTests.cs`, `DeploymentTests.cs`, `Infrastructure/TestEnvironment.cs`, `PlatformTests.cs`, новые тесты подтверждений (AC-1…AC-7, AC-9, AC-10, AC-16).
- `deploy/k8s/configmap.yaml`, `deploy/k8s/db.yaml`, `deploy/secret.template.yaml`, `deploy/dev/docker-compose.yml`, `src/Kalibr.Api/Properties/launchSettings.json`: роль БД (AC-15).
- `deploy/README.md`, `deploy/dev/README.md`: обновление, резервная копия, учётные записи, версия (AC-10, AC-14, AC-17, AC-18).
- `Directory.Build.props`, `web/package.json`, `web/package-lock.json`, новый `CHANGELOG.md`: версия и история выпусков (AC-18, AC-19).
- `.asd/project/config.yaml`, `.asd/project/custom-common-rules.md`: режим и правило (AC-12, AC-13).

## Existing docs found
- [Постановка](../../../docs/Постановка.md): §20.2 — комиссия видит required_score, aggregate_score и другое и ставит final_score; подтверждённого уровня между сессиями в постановке нет.
- [Требования promotion](../../../docs/product/requirements/promotion.html): AC-29 (итоговый балл председателя), AC-32 (менять можно до конца этапа), AC-53 (откат сохраняет введённое), AC-68 (публикация результатов). Новые требования дополняют, не отменяют.
- [promotion.md](../../../docs/architecture/promotion.md): рейтинг считает только `ICareerModel.Score`, авторов оценок API не отдаёт; подтверждение рейтинга не касается.
- [career-framework.md](../../../docs/architecture/career-framework.md), [audit.md](../../../docs/architecture/audit.md), [org-structure.md](../../../docs/architecture/org-structure.md), [user-management.md](../../../docs/architecture/user-management.md): ссылаются на общую линию миграций и «Контракт из кода»; audit.md описывает функцию и триггеры журнала вне модели.
- [stack.html](../../../docs/architecture/stack.html): «Контракт из кода» — правило до релиза 1.0 и отклонённый `backward_compat: migration`; открытые «Учётная запись БД приложения», Q5, Q11, CSP, браузеры.
- [efcore-10.0.12.md](../../../docs/architecture/tech-reference/efcore-10.0.12.md), [dotnet-ef-10.0.12.md](../../../docs/architecture/tech-reference/dotnet-ef-10.0.12.md), [efcore design](../../../docs/architecture/tech-reference/microsoft-entityframeworkcore-design-10.0.12.md): правила миграций и `MigrateAsync` при старте.
- [postgres-image-18.6-trixie.md](../../../docs/architecture/tech-reference/postgres-image-18.6-trixie.md): скрипты `/docker-entrypoint-initdb.d` выполняются только при пустом каталоге данных; `POSTGRES_USER` — суперпользователь.
- [Требования desk](../../../docs/product/requirements/desk.html): AC-121 — версия контракта 1.0.0 до релиза не повышается. Требования career-framework и org-structure упоминают «до релиза 1.0» только в отозванных пунктах.
- [DESIGN.md](../../../docs/ux/DESIGN.md), [design-system.html](../../../docs/ux/design-system.html), [accessibility.html](../../../docs/ux/accessibility.html): новые токены не нужны, используются существующие элементы.
- [deploy/README.md](../../../deploy/README.md), [deploy/dev/README.md](../../../deploy/dev/README.md): строка 92 описывает обновление до релиза и будущее схлопывание; резервное копирование — открытый вопрос.

## Contradictions
- `docs/architecture/stack.html` «отклонено: `backward_compat: migration`» vs принятый скоуп AC-12, AC-13: прежнее отклонение касалось deprecated в API; новое решение пользователя оставляет API без совместимости и включает режим для данных; winner=принятый скоуп, документ меняется по AC-13.
- Комментарий `migration` в `config.yaml` («with a documented migration path and deprecation notice») и `asd-reviewer-quality` (`breaking change without migration when backward_compat != none`) vs AC-13 (API без совместимости): winner=правило проекта `custom-common-rules.md`, его читает каждый агент; AC-13 обязан назвать API исключением явно.

## Existing implementation found
- `src/Kalibr.Promotion/PromotionFinalScore.cs`: итоговые баллы уже хранятся по заявке и компетенции, внешний ключ `Restrict`; заявка с баллами не удаляется.
- `src/Kalibr.Promotion/ReviewReader.cs` `PreviousAsync`: уже выбирает прошлые решённые заявки сотрудника с опубликованными результатами (`Status == Decided`, `StageOf >= Feedback`), без рассматриваемой заявки. AC-1 — то же условие, дополнительно читаются итоговые баллы.
- `src/Kalibr.Promotion/ReviewEndpoints.cs`: `GET` и `PUT` отдают карточку через один `DescribeAsync`, поле появляется в обоих. `DecidedAt` пишется только при смене решения; дата подтверждения — дата решения, не последней правки баллов.
- `src/Kalibr.Promotion/PromotionApplication.cs` `ReturnToStage`: откат переводит решённую заявку в `Submitted` или `Admitted`, баллы остаются; условие по статусу покрывает AC-5.
- `ChangeOf` в `ReviewReader`: компетенции сравниваются между версиями по `CompetencyId`, это подтверждает AC-4.
- `src/Kalibr.Platform/Migrations/`: `HasData` модели (роли, настройки карьерных сессий), расширение и сопоставления генерируются заново.
- `src/Kalibr.Api/Program.cs`: `MigrateDatabaseAsync` при старте, `ContractVersion = "1.0.0"`; остаётся по выбору Д.
- `deploy/k8s/app.yaml`: одна реплика, `strategy: Recreate`.

## Gaps
- Вне модели и только в миграциях (AC-9): функция `audit_events_reject_change`, триггеры `audit_events_reject_update_delete` и `audit_events_reject_truncate`, их `ENABLE ALWAYS`, индексы `IX_positions_name_lower` и `IX_legal_entities_name_lower`, вставка шкалы по умолчанию в `competency_scales`. Переносов данных (`UPDATE`, `DELETE`) в базовой миграции быть не должно.
- Подтверждение нигде не вычисляется, поля в контракте и SPA нет.
- Шкала компетенции может меняться между версиями каталога (`competencyVersions.ScaleId`): подтверждение обязано нести описание балла из версии своей заявки, а не текущей.
- `Directory.Build.props` без `<Version>`, `web/package.json` и корень `web/package-lock.json` — 0.0.0, `CHANGELOG.md` нет.
- Роль без `SUPERUSER`: `POSTGRES_USER=kalibr` в `configmap.yaml` и compose; нужен отдельный суперпользователь и скрипт инициализации базы.
- `tests/Kalibr.Tests/Infrastructure/TestEnvironment.cs`: база создаётся и используется суперпользователем контейнера.
- `.github/workflows` нет: `has-pending-model-changes` и тесты запускаются только локально.
- Тег релиза: `pr` merge mode ставит тег только при `self_hosting`; для AC-20 это отдельное действие оркестратора после подтверждения мержа.
- External dependency gaps: PostgreSQL trusted extensions: `pg_trgm` создаётся не суперпользователем с правом CREATE на базу (research.md); живая проверка — прогон тестов под ролью (AC-16).
- Migration gaps: `IdentityRolesMigrationTests.cs` проверяет удаляемую миграцию `20261021152612`: удаляется вместе с ней; `DeploymentTests.cs:38` проверяет текст `deploy/README.md`: меняется вместе с ним. Все базы: линия из 15 миграций → новая `InitialCreate` с новым идентификатором, старая база падает на старте явно, а не расходится молча.

## Risks
- Потеря объекта вне модели при сжатии: impact=чистая база без защиты журнала аудита или без шкалы по умолчанию, mitigation=сравнить схему и начальные данные чистых баз старой и новой линии; `PlatformTests` и `AuditTests` проверяют триггеры.
- Docker Desktop не запущен (проверено при audit): impact=Testcontainers-тесты и сравнение схем не запускаются, mitigation=проверить до impl-test, при необходимости попросить пользователя запустить.
- Тесты, зависящие от суперпользователя: impact=красные тесты после смены роли, mitigation=найден один случай `PlatformTests.cs:217` (`session_replication_role`); ему нужен отдельный административный доступ, полный прогон покажет остальные.
- Скрипт инициализации БД срабатывает только на пустом томе: impact=dev-стек со старым томом останется с суперпользователем и упадёт на новой миграции, mitigation=AC-10, понятная инструкция; пересоздаёт пользователь.
- Плотная строка компетенции на формах: impact=таблица и так широкая, mitigation=использовать существующую колонку или подпись, без новых токенов.

## Subsystems map
- `promotion`: карточка кандидата, формы, демо; новых подсистем и границ нет.
- `career-framework`: только чтение версий через `ICareerModel`.
- `audit`: функция и триггеры переходят в новую базовую миграцию без изменений.
- Платформенное ядро (не подсистема): миграции, хост, версия.
