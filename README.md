# CyberSecurityBot3

my C# console application

## Features
- Chat mode: conversational bot with simple responses.
- Quiz mode: interactive quiz view.
- Tasks mode: create, complete, delete tasks (persisted to disk).
- Activity Log: records significant bot actions (GUI window).
- Tasks JSON Viewer: in-app viewer to inspect persisted `tasks.json`.


Prerequisites
- Windows
- .NET 10 SDK
- Visual Studio 2026 (recommended) or VS Code + C# extensions

Open and run
- Open `ChatbotPart2.slnx` in Visual Studio and press F5.
- Or from a terminal:
  - dotnet build
  - dotnet run --project ChatbotPart2.csproj

UI notes
- The header contains mode buttons (`Chat`, `Quiz`, `Tasks`) plus `Activity Log` and `Tasks JSON`.
- Use `Tasks` to add tasks. They are saved automatically to disk.
- Click `Tasks JSON` to view the underlying JSON file in-app.
- Click `Activity Log` to inspect recent actions (clear/copy supported).


- `MainWindow.xaml(.cs)` — main UI and wiring to services.
- `TaskView.xaml(.cs)` — task manager UI and converters.
- `TaskManager.cs` — ViewModel for task UI (uses `TaskStorageHelper`).
- `TaskStorageHelper.cs` — persistent JSON task storage (thread-safe).
- `Services/ActivityLogger.cs` — in-memory logger service.
- `ActivityLogWindow.xaml(.cs)` — activity logger GUI.
- `TaskJsonViewer.xaml(.cs)` — in-app viewer for `tasks.json`.

## Screenshot of passed workflow
<img width="988" height="325" alt="image" src="https://github.com/user-attachments/assets/76598047-6165-4c2f-b40b-c8ff0c5e38f3" />

stay tuned for more updates

## Features
