using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using HeightMapExtractorGUI.ViewModels;

namespace HeightMapExtractorGUI.Views;

public partial class AesKeyDemandWindow : Window
{
    public AesKeyDemandWindow(bool testing = false)
    {
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        InitializeComponent();

        if (Design.IsDesignMode || testing)
        {
            DataContext = new AesKeyDemandViewModel(AesKeyDemandViewModel.GetDummyKeys());
            return;
        }
        
        DataContext = new AesKeyDemandViewModel();
    }

    public AesKeyDemandWindow()  : this(false) { }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        (DataContext as ViewModels.AesKeyDemandViewModel)?.Ok();
        Close((((AesKeyDemandViewModel)DataContext!)!).WasOkClicked);
    }

    private async void FNButton_OnClick(object? sender, RoutedEventArgs e)
    {
        //use content dialog to get text input
        var dialog = new ContentDialog();
        dialog.Title = "Fortnite Version";
        // dialog.Content = "Enter the Fortnite version you want to fetch keys for";
        
        var textBox = new TextBox();
        textBox.Watermark = "Enter version here (eg. 27.10)/Leave empty for latest version";
        var stackPanel = new StackPanel();
        stackPanel.Children.Add(new TextBlock { Text = "Enter the Fortnite version you want to fetch keys for", Padding = new Thickness(0, 0, 0, 10)});
        stackPanel.Children.Add(textBox);
        dialog.Content = stackPanel;
        
        dialog.PrimaryButtonText = "Ok";
        dialog.DefaultButton = ContentDialogButton.Primary;
        dialog.CloseButtonText = "Cancel";
        dialog.PrimaryButtonClick += (o, args) =>
        {
            var version = textBox.Text;
            // if (string.IsNullOrEmpty(version)) return;
            ((AesKeyDemandViewModel)DataContext!).FetchFNButton(version);
        };
        // dialog.SecondaryButtonClick += (o, args) => {  };
        dialog.CloseButtonClick += (o, args) => { };
        
        var result = await dialog.ShowAsync();
        return;
    }
}