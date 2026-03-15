using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Services;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia;

namespace Tempovium.ViewModels;

public class LibraryViewModel : ViewModelBase
{
    private readonly IMediaRepository _mediaRepository;
    private readonly IMediaImportService _mediaImportService;
    private readonly UserSessionService _userSessionService;

    private List<MediaItem> _mediaItems = new();
    private string _statusMessage = string.Empty;

    public LibraryViewModel(
        IMediaRepository mediaRepository,
        IMediaImportService mediaImportService,
        UserSessionService userSessionService)
    {
        _mediaRepository = mediaRepository;
        _mediaImportService = mediaImportService;
        _userSessionService = userSessionService;

        ImportFolderCommand = new SimpleCommand(ExecuteImportFolder);

        _ = LoadLibraryAsync();
    }

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

    public ICommand ImportFolderCommand { get; }

    private async Task LoadLibraryAsync()
    {
        if (!_userSessionService.IsLoggedIn)
            return;

        var user = _userSessionService.CurrentUser!;

        var media = await _mediaRepository.GetByUserAsync(user.Id);

        MediaItems = media;
    }

    private async void ExecuteImportFolder()
    {
        if (!_userSessionService.IsLoggedIn)
        {
            StatusMessage = "No hay un usuario con sesión activa.";
            return;
        }

        var topLevel = TopLevel.GetTopLevel(Application.Current?.ApplicationLifetime
            is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null);

        if (topLevel == null)
        {
            StatusMessage = "No se pudo acceder a la ventana principal.";
            return;
        }

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
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

        var importedItems = await _mediaImportService.ImportFolderAsync(user.Id, folderPath);

        await LoadLibraryAsync();

        StatusMessage = $"Importación completada. Se agregaron {importedItems.Count} archivo(s).";
    }

    private class SimpleCommand : ICommand
    {
        private readonly Action _execute;

        public SimpleCommand(Action execute)
        {
            _execute = execute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => _execute();
    }
}