# Приложение для регистрации посетителей поликлиники

Учебное приложение на C# и Windows Forms. Проект создан в Visual Studio Community 2026 (18.10.2) и рассчитан на .NET 10 для Windows. Данные хранятся локально в SQLite через пакет `Microsoft.Data.Sqlite` 10.0.12.

## Готовая сборка для Windows x64

Скачайте архив [PolyclinicRegistration-win-x64.zip](https://github.com/s4pf1re/PolyclinicRegistration/releases/download/v1.0.0/PolyclinicRegistration-win-x64.zip), распакуйте его целиком и запустите `PolyclinicRegistration.exe`. Установка .NET для этой сборки не требуется. Пароль для демонстрации журнала сотрудников — `111`.

## Запуск из исходников

На Windows 10/11 с установленной **.NET Desktop Runtime 10** запустите `PolyclinicRegistration\bin\Release\net10.0-windows\PolyclinicRegistration.exe`. После первого запуска база создаётся автоматически.

Для открытия и сборки исходников откройте `PolyclinicRegistration.sln` в Visual Studio 2026. Также можно выполнить в папке `Application`:

```powershell
dotnet build PolyclinicRegistration.sln -c Release
dotnet run --project PolyclinicRegistration\PolyclinicRegistration.csproj -c Release
```

Для проверки правил записи:

```powershell
dotnet run --project PolyclinicRegistration.Checks\PolyclinicRegistration.Checks.csproj -c Release
```

## Возможности

- Пациент вводит фамилию, имя и отчество, дату рождения, выбирает специальность, врача, дату и свободное время.
- Приём доступен с понедельника по пятницу с 09:00 до 16:30 включительно, интервалы по 30 минут. Прошедшее время и занятые интервалы недоступны.
- После записи показывается талон с номером, данными пациента, врачом, кабинетом, датой и временем. Талон можно сохранить в текстовый файл.
- Кнопка «Вход для сотрудников» открывает журнал после ввода учебного пароля `111`. В журнале можно искать и просматривать записи и пациентов, показать талон, перенести, отменить или удалить запись.
- Отмена и перенос освобождают старое время. Одинаковое время одного врача не может быть занято двумя активными записями; правило обеспечено также уникальным индексом базы данных.

База по умолчанию: `%LOCALAPPDATA%\PolyclinicRegistration\clinic.db`. Для отдельной демонстрационной базы можно задать переменную окружения `POLYCLINIC_DB_PATH` перед запуском. В исходной поставке реальные данные пациентов отсутствуют. Пароль `111` предназначен для учебной демонстрации и не является защитой для промышленного применения.

## Состав решения

- `PolyclinicRegistration` — приложение Windows Forms: модели `Doctor`, `Patient`, `Appointment`; каталог врачей; `AppointmentService` с проверками; `AppointmentRepository` для SQLite; формы записи, талона, входа и журнала.
- `PolyclinicRegistration.Checks` — 18 проверок ключевых сценариев на временной базе SQLite.

Таблица `Patients` содержит идентификатор, ФИО и дату рождения. Таблица `Appointments` содержит идентификатор, ссылку на пациента, идентификатор врача, дату, время и статус `Active`/`Cancelled`. Номер талона формируется из идентификатора записи в виде `Т-000001`.
