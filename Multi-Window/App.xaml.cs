using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

using Multi_Window.Activation;
using Multi_Window.Contracts.Services;
using Multi_Window.Core.Contracts.Services;
using Multi_Window.Core.Services;
using Multi_Window.Services;
using Multi_Window.ViewModels;
using Multi_Window.Views;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Diagnostics;
using Microsoft.UI.Dispatching;

namespace Multi_Window;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    // The .NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    public IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((App.Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();
    public static ShellPage? ShellPage  { get; set; }
    private static readonly List<string> _traceMessages = new();

    // Provide easy access to the UI thread
    public static DispatcherQueue UIDispatcherQueue = DispatcherQueue.GetForCurrentThread();


    public Task? messageGenerator;

    public App()
    {
        InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddTransient<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();

            // Core Services
            services.AddSingleton<IFileService, FileService>();

            // Views and ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<MainPage>();

            // ** NOTE ** changed to Singleton so we can refer to THE ShellPage/ShellViewModel
            services.AddSingleton<ShellPage>();
            services.AddSingleton<ShellViewModel>();

            // Configuration
        }).
        Build();

        UnhandledException += App_UnhandledException;
        System.AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Microsoft.UI.Xaml.Application.Current.UnhandledException += Current_UnhandledException;
    }

    private async Task RandomMessageGenerator()
    {
        var shutdown = false;
        WeakReferenceMessenger.Default.Register<ShutDownMessage>(this, (r, m) => shutdown = true);
        Debug.WriteLine($"RandomMessageGenerator started on thread {Environment.CurrentManagedThreadId}");

        Random rnd = new();
        while (shutdown == false)
        {
            await Task.Delay(rnd.Next(2000));
            try
            {
                WeakReferenceMessenger.Default.Send(new TraceMessage($"{DateTime.Now:hh:mm:ss.ffff} Timer event. (Th: {Environment.CurrentManagedThreadId})"));
            }
            catch (Exception e) 
            {
                Debug.WriteLine(e.Message);
                break;
            }
        }
        Debug.WriteLine($"RandomMessageGenerator closed at {DateTime.Now:hh:mm:ss.ffff} (Th: {Environment.CurrentManagedThreadId})");
    }

    private void Current_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e) => throw new NotImplementedException();
    private void CurrentDomain_UnhandledException(object sender, System.UnhandledExceptionEventArgs e) => throw new NotImplementedException();

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        // TODO: Log and handle exceptions as appropriate.
        // https://docs.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.application.unhandledexception.
        throw new NotImplementedException();
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

        await App.GetService<IActivationService>().ActivateAsync(args);

        MainWindow.AppWindow.Closing += OnAppWindowClosing;

        WeakReferenceMessenger.Default.Register<TraceMessage>(this, (r, m) =>
        {
            _traceMessages.Add(m.Value);
            Debug.WriteLine(m.Value);
        });
        WeakReferenceMessenger.Default.Register<WindowClosedMessage>(this, (r, m) => OnStatusWindowClosed());           // StatusWindow closed events
        WeakReferenceMessenger.Default.Register<App, TraceMessagesRequest>(this, (r, m) => m.Reply(_traceMessages));    // StatusWindow requests previous messages

        messageGenerator = Task.Run(RandomMessageGenerator);
    }

    private void OnStatusWindowClosed()
    {
        if (ShellPage is not null && ShellPage.SettingsStatusWindow)
        {
            ShellPage.SettingsStatusWindow = false;                                                     // turn off toggle
            if (ShellPage.NavigationFrame.Content is MainPage settingsPage) settingsPage.StatusWindowToggle.IsOn = false;
        }
    }

    private async void OnAppWindowClosing(object sender, AppWindowClosingEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(new ShutDownMessage());                                     // close all windows
        if (messageGenerator is not null)
        {
            e.Cancel = true;
            await messageGenerator;                                                                     // wait for closure
            messageGenerator = null;
            MainWindow.Close();
        }
        else
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);                                         // stop messages and avoid memory leaks
            MainWindow.AppWindow.Closing -= OnAppWindowClosing;
        }
    }
}

// Allows Win32 access to a Window through WinAPI
public class WindowPrimitives
{
    public IntPtr HWnd { get; }
    private WindowId WindowId { get; }
    public AppWindow AppWindow { get; }
    public Window Window { get; }

    public WindowPrimitives(Window window)
    {
        HWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WindowId = Win32Interop.GetWindowIdFromWindow(HWnd);
        AppWindow = AppWindow.GetFromWindowId(WindowId);
        Window = window;
    }
}

// Message Definitions

public class CloseWindowMessage : ValueChangedMessage<bool>
{
    public CloseWindowMessage(bool value) : base(value) { }
}

public class WindowClosedMessage : ValueChangedMessage<bool>
{
    public WindowClosedMessage(bool value) : base(value) { }
}

public class ShutDownMessage
{
}

public class TraceMessage : ValueChangedMessage<string>
{
    public TraceMessage(string value) : base(value) { }
}

public class TraceMessagesRequest : CollectionRequestMessage<List<string>>
{
}
