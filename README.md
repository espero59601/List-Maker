# ListTracker - Universal Checklist App

A .NET MAUI Blazor Hybrid app for creating and managing custom list templates.
Users can create lists for anything — coin collections, paint colors, travel packing, etc.

## Requirements

- **Visual Studio 2022** (17.8+) with the **.NET MAUI workload** installed
- **.NET 8 SDK**
- For Android: Android SDK (installed via Visual Studio)
- For iOS: macOS with Xcode (required for iOS builds)

## Setup

1. Open `ChecklistApp.csproj` in Visual Studio
2. If prompted, install the MAUI workload
3. Select your target (Android Emulator, iOS Simulator, or a connected device)
4. Press F5 to build and run

## Project Structure

```
ChecklistApp/
├── MauiProgram.cs              # App entry point & service registration
├── App.xaml / App.xaml.cs      # MAUI app shell
├── MainPage.xaml               # Hosts the Blazor WebView
├── Models/
│   └── ListTemplate.cs         # Data models (ListTemplate, ListItem)
├── Services/
│   └── ListTemplateService.cs  # JSON file storage (CRUD + import/export)
├── Components/
│   ├── Routes.razor            # Blazor routing
│   ├── _Imports.razor          # Global using statements
│   ├── Layout/
│   │   └── MainLayout.razor    # App header and layout
│   └── Pages/
│       ├── Home.razor          # Home page — lists all templates
│       └── EditTemplate.razor  # Create/edit a template with items
└── wwwroot/
    ├── index.html              # Blazor host page
    └── css/
        └── app.css             # All app styles
```

## Current Features (Phase 1)

- Create new list templates with name, category, and description
- Add items one at a time or bulk-add (one per line)
- Edit item names inline (tap to edit)
- Reorder items with up/down arrows
- Delete items and templates
- Data persisted as JSON files in app local storage
- Clean, mobile-friendly UI

## Planned Features

- **Phase 2**: Check off items on your personal copy of a list
- **Phase 3**: Browse/search shared list templates from other users
- **Phase 4**: User accounts and cloud sync
- **Phase 5**: Export/share lists via JSON file or link

## Data Storage

Templates are stored as individual JSON files in the app's local data directory:
- Android: `/data/data/com.listtracker.app/files/templates/`
- iOS: App sandbox `Documents/templates/`

Each template is a separate `.json` file, making it easy to debug, export, or share.
