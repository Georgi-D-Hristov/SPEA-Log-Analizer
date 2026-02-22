---
name: wpf-mvvm
description: "Best practices and guidelines for writing WPF applications using the MVVM pattern in .NET."
---

# WPF MVVM Best Practices

When asked to write, debug, or refactor WPF (Windows Presentation Foundation) code, always strictly adhere to the MVVM (Model-View-ViewModel) architectural pattern:

## 1. Strict Separation of Concerns
- **Model**: Pure data and business logic. Zero dependencies on UI frameworks (do not import `System.Windows`).
- **View**: XAML files. Keep the Code-Behind (`.xaml.cs`) empty of business logic. Only UI-specific logic (e.g., complex animations or UI routing) is allowed.
- **ViewModel**: The glue. It holds the presentation state and exposes data and commands to the View.

## 2. State and Data Binding
- All ViewModels MUST implement `INotifyPropertyChanged` so the UI reacts to data changes.
- If the project uses `CommunityToolkit.Mvvm`, use the `[ObservableProperty]` attribute to eliminate boilerplate.
- Always use `ObservableCollection<T>` instead of `List<T>` for UI-bound collections to ensure the View updates automatically when items are added or removed.

## 3. Actions and Commands (ICommand)
- NEVER use standard UI event handlers (e.g., `Button_Click`) in the code-behind for business operations.
- Expose `ICommand` properties in the ViewModel and bind your UI controls to them.
- Prefer `[RelayCommand]` attributes (from `CommunityToolkit.Mvvm`) or standard `RelayCommand`/`DelegateCommand` implementations.

## 4. Dependency Injection & Services
- Do not instantiate UI Windows or Message Boxes directly inside the ViewModel. 
- Use injected interface services (e.g., `IDialogService`, `INavigationService`) to handle UI popups and navigation, maintaining testability and strict decoupling.