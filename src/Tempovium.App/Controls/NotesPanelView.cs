using Avalonia.Controls;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Tempovium.ViewModels;

namespace Tempovium.Controls;

public partial class NotesPanelView : UserControl
{
    private NotesPanelViewModel ViewModel =>
        (NotesPanelViewModel)DataContext!;

    public NotesPanelView()
    {
        InitializeComponent();
        DataContext = Program.AppHost.Services.GetRequiredService<NotesPanelViewModel>();
    }

    private async void AddNote_Click(object? sender, RoutedEventArgs e)
    {
        await ViewModel.AddNoteAsync();
    }

    private void JumpToNote_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not NoteItemViewModel note)
            return;

        ViewModel.JumpToNote(note);
    }

    private async void DeleteNote_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.DataContext is not NoteItemViewModel note)
            return;

        await ViewModel.DeleteNoteAsync(note);
    }
}