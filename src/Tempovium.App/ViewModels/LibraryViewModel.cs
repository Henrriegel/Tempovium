using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
    private ObservableCollection<MediaImportCandidate> _importReviewCandidates = [];
    private string _statusMessage = string.Empty;
    private string _importReviewSummary = string.Empty;
    private string _importOverlayTitle = "Importando medios";
    private string _importOverlayMessage = "La biblioteca se actualizará al terminar.";
    private MediaItem? _selectedMedia;
    private bool _isImporting;
    private bool _isImportReviewOpen;

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
        ConfirmImportCommand = new SimpleCommand(ExecuteConfirmImport);
        CancelImportReviewCommand = new SimpleCommand(CancelImportReview);
        SelectAllImportReviewCommand = new SimpleCommand(SelectAllImportReviewCandidates);
        ClearImportReviewSelectionCommand = new SimpleCommand(ClearImportReviewSelection);
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

    public ObservableCollection<MediaImportCandidate> ImportReviewCandidates
    {
        get => _importReviewCandidates;
        set
        {
            var oldCandidates = _importReviewCandidates;
            if (SetProperty(ref _importReviewCandidates, value))
            {
                UnsubscribeImportReviewCandidates(oldCandidates);
                SubscribeImportReviewCandidates(_importReviewCandidates);
                RefreshImportReviewSelection();
            }
        }
    }

    public string ImportReviewSummary
    {
        get => _importReviewSummary;
        set => SetProperty(ref _importReviewSummary, value);
    }

    public bool IsImportReviewOpen
    {
        get => _isImportReviewOpen;
        set
        {
            if (SetProperty(ref _isImportReviewOpen, value))
            {
                OnPropertyChanged(nameof(CanImport));
            }
        }
    }

    public bool HasImportReviewCandidates => ImportReviewCandidates.Count > 0;
    public bool HasNoImportReviewCandidates => !HasImportReviewCandidates;
    public int SelectedImportReviewCount => ImportReviewCandidates.Count(candidate => candidate.IsSelected);
    public bool CanConfirmImport => SelectedImportReviewCount > 0;
    public string ImportReviewSelectionText => $"Seleccionados: {SelectedImportReviewCount}";

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
                _shellViewModel.SetImporting(value, _importOverlayTitle, _importOverlayMessage);
            }
        }
    }

    public bool CanImport => !IsImporting && !IsImportReviewOpen;

    public ICommand ImportFolderCommand { get; }
    public ICommand ImportFileCommand { get; }
    public ICommand ConfirmImportCommand { get; }
    public ICommand CancelImportReviewCommand { get; }
    public ICommand SelectAllImportReviewCommand { get; }
    public ICommand ClearImportReviewSelectionCommand { get; }

    public void RefreshImportReviewSelection()
    {
        OnPropertyChanged(nameof(HasImportReviewCandidates));
        OnPropertyChanged(nameof(HasNoImportReviewCandidates));
        OnPropertyChanged(nameof(SelectedImportReviewCount));
        OnPropertyChanged(nameof(CanConfirmImport));
        OnPropertyChanged(nameof(ImportReviewSelectionText));
    }

    private void SubscribeImportReviewCandidates(IEnumerable<MediaImportCandidate> candidates)
    {
        foreach (var candidate in candidates)
        {
            candidate.PropertyChanged += OnImportReviewCandidateChanged;
        }
    }

    private void UnsubscribeImportReviewCandidates(IEnumerable<MediaImportCandidate> candidates)
    {
        foreach (var candidate in candidates)
        {
            candidate.PropertyChanged -= OnImportReviewCandidateChanged;
        }
    }

    private void OnImportReviewCandidateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MediaImportCandidate.IsSelected))
        {
            RefreshImportReviewSelection();
        }
    }

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
        SetImportOverlay("Analizando carpeta", "Preparando la revisión de archivos.");
        IsImporting = true;
        try
        {
            var preview = await _mediaImportService.ScanFolderAsync(user.Id, folderPath);
            ImportReviewCandidates = new ObservableCollection<MediaImportCandidate>(preview.Candidates);
            ImportReviewSummary = FormatPreviewSummary(preview);
            IsImportReviewOpen = true;
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
        SetImportOverlay("Importando medios", "La biblioteca se actualizará al terminar.");
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

    private async void ExecuteConfirmImport()
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

        var user = _userSessionService.CurrentUser!;
        if (!CanConfirmImport)
        {
            StatusMessage = "Selecciona al menos un archivo para importar.";
            return;
        }

        SetImportOverlay("Importando medios", "La biblioteca se actualizará al terminar.");
        IsImporting = true;
        try
        {
            var selectedCandidates = ImportReviewCandidates
                .Where(candidate => candidate.IsSelected)
                .ToList();
            var importResult = await _mediaImportService.ImportCandidatesAsync(user.Id, selectedCandidates);
            IsImportReviewOpen = false;
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

    private void CancelImportReview()
    {
        IsImportReviewOpen = false;
        ImportReviewCandidates = [];
        ImportReviewSummary = string.Empty;
    }

    private void SelectAllImportReviewCandidates()
    {
        foreach (var candidate in ImportReviewCandidates.Where(candidate => candidate.CanSelect))
        {
            candidate.IsSelected = true;
        }

        RefreshImportReviewSelection();
    }

    private void ClearImportReviewSelection()
    {
        foreach (var candidate in ImportReviewCandidates)
        {
            candidate.IsSelected = false;
        }

        RefreshImportReviewSelection();
    }

    private void SetImportOverlay(string title, string message)
    {
        _importOverlayTitle = title;
        _importOverlayMessage = message;
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

    private static string FormatPreviewSummary(MediaImportPreviewResult result)
    {
        return $"Escaneados: {result.TotalFilesScanned}. " +
               $"Compatibles: {result.SupportedCount}. " +
               $"Duplicados: {result.DuplicateCount}. " +
               $"No compatibles: {result.UnsupportedCount}. " +
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
