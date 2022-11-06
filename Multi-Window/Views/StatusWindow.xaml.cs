﻿using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Multi_Window.Views;
/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class StatusWindow : Window
{
    private ObservableCollection<string> _traceMessages { get; } = new();

    public StatusWindow()
    {
        InitializeComponent();
        var sw = new WindowPrimitives(this);
        sw.AppWindow.SetIcon("Assets/wip.ico");

        WeakReferenceMessenger.Default.Register<CloseWindowMessage>(this, (r, m) => Close());
        WeakReferenceMessenger.Default.Register<ShutDownMessage>(this, (r, m) => Close());
    }

    private void StatusWindow_Closed(object sender, WindowEventArgs args)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);                                             // stop getting messages and avoid memory leaks
        WeakReferenceMessenger.Default.Send(new WindowClosedMessage(true));                             // acknowledge closure
    }

    private void StatusMessages_Loaded(object sender, RoutedEventArgs e)
    {
        // get current Trace messages
        var messages = WeakReferenceMessenger.Default.Send<TraceMessagesRequest>();
        if (messages != null && messages.Responses.Count > 0)
            foreach (var response in messages.Responses)
                foreach (var trace in response)
                    _traceMessages.Add(trace);

        // register for Trace messages and, when they arrive, add them to list
        WeakReferenceMessenger.Default.Register<TraceMessage>(this, (r, m) => _traceMessages.Add(m.Value));
    }
}