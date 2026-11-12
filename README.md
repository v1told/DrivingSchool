# Автошкола: система формування та управління розкладом занять

Спільний курсовий проєкт з дисципліни «Об'єктно-орієнтоване програмування».
Мова C#, платформа .NET 8, інтерфейс WPF (MVVM), база даних SQLite через Entity Framework Core, тести xUnit.

## Запуск

1. Встановити Visual Studio 2022 (17.8 або новіша) з робочим навантаженням «.NET desktop development».
2. Відкрити `DrivingSchool.sln`.
3. Дочекатися відновлення пакетів NuGet (відбувається автоматично).
4. Зробити `DrivingSchool.App` стартовим проєктом і натиснути F5.

Під час першого запуску поруч із програмою створюється файл `drivingschool.db`
з демонстраційними даними: 4 автомобілі, 4 викладачі, 2 групи, 10 учнів,
розклад за три минулі, поточний і наступний тижні, записи, відвідування й оцінки.
Щоб почати з чистої бази, достатньо видалити цей файл.

Тести: меню Test → Run All Tests (або `dotnet test` у теці рішення).

## Структура рішення

```
DrivingSchool.sln
├── src/DrivingSchool.Core          бібліотека бізнес-логіки (без інтерфейсу)
│   ├── Models/                     класи предметної області (спільні)
│   ├── Data/                       AppDbContext, DbInitializer (спільні)
│   ├── Common/                     IEntity, Repository<T>, IClock, DomainException
│   ├── Scheduling/                 МОДУЛЬ 1: облік ресурсів і формування розкладу
│   └── Learning/                   МОДУЛЬ 2: запис, відвідування, успішність
├── src/DrivingSchool.App           WPF-застосунок (MVVM)
│   ├── Infrastructure/             ViewModelBase, RelayCommand, Db, Lookups
│   ├── ViewModels/, Views/         вкладки обох модулів
└── tests/DrivingSchool.Tests       модульні тести xUnit
```

## Розподіл функціоналу

| Студентка 1: облік ресурсів і розклад | Студентка 2: запис, відвідування, прогрес |
|---|---|
| `Scheduling/ResourceService` | `Learning/BookingService` |
| `Scheduling/ScheduleRules`, `ScheduleConflictChecker` | `Learning/AttendanceService` |
| `Scheduling/ScheduleService`, `ScheduleGenerator` | `Learning/ProgressService`, `ReportService`, `LearningPolicy` |
| `CrudViewModel<T>`, `Students/Instructors/Vehicles/Groups/ScheduleViewModel` | `Booking/Attendance/ProgressViewModel` |
| `StudentsView`, `InstructorsView`, `VehiclesView`, `GroupsView`, `ScheduleView` | `BookingView`, `AttendanceView`, `ProgressView` |
| `SchedulingTests` | `LearningTests` |

Спільна частина (`Models`, `Data`, `Common`, `MainWindow`, `App`) розробляється разом.

## Спільний репозиторій GitHub (історія комітів обох студенток)

1. Перша студентка створює **порожній** репозиторій `DrivingSchool` на GitHub і запускає з кореня цієї папки:
   `powershell -ExecutionPolicy Bypass -File scripts\student1.ps1 https://github.com/<логін>/DrivingSchool.git`.
   Скрипт робить коміти спільної частини і модуля 1 у гілках `main` і `feature/scheduling` від її імені.
2. Вона додає другу студентку як Collaborator (Settings → Collaborators).
3. Друга студентка з кореня своєї копії папки запускає
   `powershell -ExecutionPolicy Bypass -File scripts\student2.ps1 https://github.com/<логін>/DrivingSchool.git`.
   Скрипт робить коміти модуля 2 у гілці `feature/learning` від її імені.
4. Обидві гілки вливаються в `main` через Pull Request. Внесок видно в Insights → Contributors і `git log --author`.

## Підтвердження збірки і тестів

Файл `.github/workflows/build-and-test.yml` запускає на GitHub (Windows, .NET 8) команди
`dotnet restore`, `dotnet build` і `dotnet test` при кожному push. Результат (кількість пройдених тестів)
видно на вкладці **Actions**, звіт `test-results.trx` можна завантажити. Локально те саме:

```
dotnet test DrivingSchool.sln --logger "console;verbosity=normal"
```

У проєкті 30 тестових методів (11 у SchedulingTests, 19 у LearningTests), що дають 33 тестові випадки, бо параметризовані тести виконуються для кожного набору даних.
