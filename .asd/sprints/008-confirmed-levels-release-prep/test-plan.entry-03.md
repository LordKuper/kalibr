# Test plan — sprint 008-confirmed-levels-release-prep, запись 3

Ротированные строки записи 3 (`artifact-layout.md` "Test plan"); не редактируется.

## Risk → check decisions

| Change | Material risk | Chosen check | Decision | Reason |
|---|---|---|---|---|
| `ReviewParts.test.tsx`: `takes the top of the scale of the application it was confirmed in` (замечание F5 `wave-1/iter-01`; строка `remove` записи 2) | отдельного риска нет: компонент видит только `level.scalePoints`, «из 4» уже проверяет первый тест, оба падают от одной мутации `at(0)` | компонентный (vitest) | remove | Выполнено, причина и доказательство — в `Removed tests`. Строка `Added tests` записи 1 об этом тесте (`test-plan.entry-01.md`, сегмент не правится) этим удалением снята; её доказательство относится теперь только к `is one line with the score out of the top of its scale, …` |
| `ReviewReader`: убран фильтр `scale.ContainsKey` (замечание «упасть, а не пропустить», `ca5bba6c`) | оценка компетенции вне шкалы своей версии раньше молча пропускалась, теперь `KeyNotFoundException` | интеграционный | none | Ветка недостижима через API: `PUT .../review` проверяет итоговые оценки по версии заявки (`ScoreRules.Problems`), заявка и оценки одной версии. Тест на неё строил бы состояние прямой записью в базу — гипотетический риск (`code-style.md` §17). Цикл выбора подтверждения на реальных данных идёт во всех тестах `PromotionConfirmedLevelTests` (три версии матрицы, профильная компетенция вне шкалы последней заявки), весь backend зелёный |
| `tours.ts`: фраза о подтверждённом уровне на шагах `review-card` (форма члена; карточка общая, `MemberReviewView`) и `review-scores` (форма председателя, `ChairReviewView`) (AC-8, `84b0cdfe`) | фраза пропала или осталась на одной форме | статическая (сборка, lint) | none | Ни один тест не читает текст справки экрана `promotion`: `ScreenTour.test.tsx` обходит только мастер матрицы. Тест текста закреплял бы формулировку, а не контракт (`code-style.md` §17: утверждение формулировки, а не токена, — не контракт); наблюдаемое поведение AC-8 — строка «Подтверждено: …» на обеих формах — уже закреплено `ReviewParts.test.tsx` и `DemoPage.test.tsx` (оба шага демо). Якоря `data-tour="review-card"` и `tour="review-scores"` на месте в `MemberReviewView.tsx` и `ChairReviewView.tsx`, `lint` и `build` чисты. Меняется только строка литерала |
| `deploy/README.md` раздел 5: порядок шагов обновления, строка «Обновление» (AC-14, `4bd3c30b`) | команда изменилась или потеряна при перестановке | сравнение по `git diff` | none | Блоки кода файла у `c1325443` и `HEAD` дают одинаковый отсортированный md5 строк (`bd5b742b…`), а в изменённых строках `kubectl` и `pg_dump` стоят только в прозе шага: блоки кода не тронуты, переставлены и переписаны шаги 1–2 и пункт списка. Разовые проверки B и C записи 1 не повторялись: их входы (команды) не менялись. `DeploymentTests.Ac6` в полном наборе зелёный |
| `TestEnvironment.CreateDatabaseAsync`: `private` (после удаления теста `Ac63`) | общий для всех интеграционных тестов фундамент | предохранитель общей инфраструктуры | keep | Шаг 8 — полный набор. Побочных вызовов метода не осталось: сборка и 954 теста зелёные |
| `PromotionConfirmedLevelTests`: тест `Ac4_…ComesFromTheLatestOneThatHadIt` и хелпер `PublishVersionAsync` (`5cf024fb`) | выбор подтверждения только из последней заявки | интеграционный | keep | Строка `Added tests` и мутационное доказательство записи 2 (`test-plan.entry-02.md`) остаются в силе: код `ReviewReader` в цикле выбора не менялся с тех пор, строка цикла `ReviewReader.cs:254` та же. Хелпер не покрыт отдельным тестом: он только публикует версию и вызывается обоими `Ac4`-тестами |

## Removed tests

| Test | Reason | In change scope |
|---|---|---|
| `web/src/features/promotion/ReviewParts.test.tsx`: `takes the top of the scale of the application it was confirmed in` | дубликат первого теста (`is one line with the score out of the top of its scale, …`): компонент читает только `level.scalePoints.at(-1)`, «из 4» первый тест уже закрепляет, а мутация `at(-1)` → `at(0)` роняет оба; шкалу по версии матрицы проверяет сервер (`Ac4`), компоненту она не видна. Доказательство, что первый тест покрывает риск без второго: мутация `at(-1)` → `at(0)` в `ReviewParts.tsx` на месте (восстановлена `git checkout` в той же команде) и `npm --prefix web test -- src/features/promotion/ReviewParts.test.tsx` → exit 1, `1 failed \| 1 passed (2)`, упал `is one line with the score out of the top of its scale, …`. Коммит `857f4f15` | yes |

## Added tests

| Test | Regression proof |
|---|---|
