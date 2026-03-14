using System.Collections.Generic;
using Tempovium.Core.Entities;
using Tempovium.Core.Interfaces;
using Tempovium.Core.Services;

namespace Tempovium.ViewModels;

public class LibraryViewModel : ViewModelBase
{
    private readonly IMediaRepository _mediaRepository;
    private readonly UserSessionService _userSessionService;

    private List<MediaItem> _mediaItems = new();

    public LibraryViewModel(
        IMediaRepository mediaRepository,
        UserSessionService userSessionService)
    {
        _mediaRepository = mediaRepository;
        _userSessionService = userSessionService;

        LoadLibrary();
    }

    public List<MediaItem> MediaItems
    {
        get => _mediaItems;
        set => SetProperty(ref _mediaItems, value);
    }

    private async void LoadLibrary()
    {
        if (!_userSessionService.IsLoggedIn)
            return;

        var user = _userSessionService.CurrentUser!;

        var media = await _mediaRepository.GetByUserAsync(user.Id);

        MediaItems = media;
    }
}