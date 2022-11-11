using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using Multi_Window.ViewModels;

namespace Multi_Window.Views;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = App.GetService<MainViewModel>();
    public ShellPage ShellPage { get; } = App.GetService<ShellPage>();
    public ShellViewModel ShellViewModel { get; } = App.GetService<ShellViewModel>();

    public MainPage()
    {
        DataContext = ViewModel;
        InitializeComponent();
    }

    private void StatusWindow_Toggled(object sender, RoutedEventArgs e)
    {
        if (StatusWindowToggle.IsOn && ShellPage.SettingsStatusWindow == false)
        {
            StatusWindow window = new() { Title = "Prosper Status" };
            window.Activate();
            ShellPage.SettingsStatusWindow = true;
        }
        else if (StatusWindowToggle.IsOn == false && ShellPage.SettingsStatusWindow == true)
            WeakReferenceMessenger.Default.Send(new CloseWindowMessage(true));
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new ShutDownMessage());                                     // close all windows
        if (App.Current is App app && app.messageGenerator is not null)
        {
            await app.messageGenerator;                                                                 // wait for closure
            app.messageGenerator = null;
        }
        App.MainWindow.Close();
    }
}
