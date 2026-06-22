using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Models;
using Tempovium.Core.Services;
using Tempovium.Infrastructure.Persistence;
using Tempovium.Services;

namespace Tempovium.ViewModels;

public class LibraryViewModel : ViewModelBase
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IMediaImportService _mediaImportService;
    private readonly UserSessionService _userSessionService;
    private readonly SelectedMediaService _selectedMediaService;
    private readonly MainWindowViewModel _shellViewModel;

    private List<MediaItem> _mediaItems = new();
    private string _statusMessage = string.Empty;
    private MediaItem? _selectedMedia;
    private bool _isImporting;

    public LibraryViewModel(
        IMediaRepository mediaRepository,
        IMediaImportService mediaImportService,
        UserSessionService userSessionService,
        SelectedMediaService selectedMediaService,
        MediaPlayerViewModel mediaPlayerViewModel,
        MainWindowViewModel shellViewModel)
    {
        _mediaRepository = mediaRepository;
        _mediaImportService = mediaImportService;
        _userSessionService = userSessionService;
        _selectedMediaService = selectedMediaService;
        MediaPlayerViewModel = mediaPlayerViewModel;
        _shellViewModel = shellViewModel;

        ImportFolderCommand = new SimpleCommand(ExecuteImportFolder);
        ImportFileCommand = new SimpleCommand(ExecuteImportFile);
        _ = LoadLibraryAsync();
    }

    public MediaPlayerViewModel MediaPlayerViewModel { get; }

    public List<MediaItem> MediaItems
    {
        get => _mediaItems;
        set => SetProperty(ref _mediaItems, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public MediaItem? SelectedMedia
    {
        get => _selectedMedia;
        set
        {
            if (SetProperty(ref _selectedMedia, value))
            {
                if (value is not null)
                {
                    _selectedMediaService.SelectedMedia = value;
                }

                OnPropertyChanged(nameof(HasSelectedMedia));
                OnPropertyChanged(nameof(HasNoSelectedMedia));
                OnPropertyChanged(nameof(SelectedMediaTitle));
                OnPropertyChanged(nameof(SelectedMediaLocationDisplay));
            }
        }
    }

    public bool HasSelectedMedia => SelectedMedia is not null;
    public bool HasNoSelectedMedia => SelectedMedia is null;

    public string SelectedMediaTitle => SelectedMedia?.Title ?? string.Empty;

    public string SelectedMediaLocationDisplay => SelectedMedia is null
        ? string.Empty
        : MediaPathDisplayFormatter.Format(SelectedMedia.FilePath, TempoviumDataPaths.GetManagedMediaDirectory());

    public bool IsImporting
    {
        get => _isImporting;
        private set
        {
            if (SetProperty(ref _isImporting, value))
            {
                OnPropertyChanged(nameof(CanImport));
                _shellViewModel.SetImporting(value);
            }
        }
    }

    public bool CanImport => !IsImporting;

    public ICommand ImportFolderCommand { get; }
    public ICommand ImportFileCommand { get; }

    private async Task LoadLibraryAsync()
    {
        if (!_userSessionService.IsLoggedIn)
        {
            return;
        }

        var user = _userSessionService.CurrentUser!;
        MediaItems = await _mediaRepository.GetByUserAsync(user.Id);
    }

    private async void ExecuteImportFolder()
    {
        if (IsImporting)
        {
            return;
        }

        if (!_userSessionService.IsLoggedIn)
        {
            StatusMessage = "No hay un usuario con sesión activa.";
            return;
        }

        var topLevel = GetTopLevel();
        if (topLevel == null)
        {
            StatusMessage = "No se pudo acceder a la ventana principal.";
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Selecciona una carpeta con medios"
            });

        if (folders.Count == 0)
        {
            StatusMessage = "No se seleccionó ninguna carpeta.";
            return;
        }

        var folderPath = folders[0].Path.LocalPath;
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            StatusMessage = "La carpeta seleccionada no tiene una ruta válida.";
            return;
        }

        var user = _userSessionService.CurrentUser!;
        IsImporting = true;
        try
        {
            var importResult = await _mediaImportService.ImportFolderAsync(user.Id, folderPath);
            await LoadLibraryAsync();
            StatusMessage = FormatImportSummary(importResult);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error durante la importación: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
        }
    }

    private async void ExecuteImportFile()
    {
        if (IsImporting)
        {
            return;
        }

        if (!_userSessionService.IsLoggedIn)
        {
            StatusMessage = "No hay un usuario con sesión activa.";
            return;
        }

        var topLevel = GetTopLevel();
        if (topLevel == null)
        {
            StatusMessage = "No se pudo acceder a la ventana principal.";
            return;
        }

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Selecciona un archivo de medio",
                AllowMultiple = false
            });

        if (files.Count == 0)
        {
            StatusMessage = "No se seleccionó ningún archivo.";
            return;
        }

        var filePath = files[0].Path.LocalPath;
        if (string.IsNullOrWhiteSpace(filePath))
        {
            StatusMessage = "El archivo seleccionado no tiene una ruta válida.";
            return;
        }

        var user = _userSessionService.CurrentUser!;
        IsImporting = true;
        try
        {
            var importResult = await _mediaImportService.ImportFileAsync(user.Id, filePath);
            await LoadLibraryAsync();
            StatusMessage = FormatImportSummary(importResult);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error durante la importación: {ex.Message}";
        }
        finally
        {
            IsImporting = false;
        }
    }

    private static TopLevel? GetTopLevel()
    {
        return TopLevel.GetTopLevel(
            Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null);
    }

    private static string FormatImportSummary(MediaImportResult result)
    {
        return $"Importación completada. Importados: {result.ImportedCount}. " +
               $"Duplicados omitidos: {result.DuplicateCount}. " +
               $"No compatibles omitidos: {result.UnsupportedCount}. " +
               $"Errores: {result.ErrorCount}.";
    }

    public class SimpleCommand : ICommand
    {
        private readonly Action _execute;

        public SimpleCommand(Action execute)
        {
            _execute = execute;
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();

        public event EventHandler? CanExecuteChanged;
    }
}
