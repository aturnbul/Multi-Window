# Multi-Window
Multi-Window, multi-threaded messenger test


The following simple multi-threaded program was meant to try out the CommunityToolkit Messenger package for which the documentation says

"Both WeakReferenceMessenger and StrongReferenceMessenger also expose a Default property that offers a thread-safe implementation built-in into the package."

I had hoped this would mean I could send messages on one thread and receive them on other threads but a problem arose with what seems to be the IMessenger Interface. Details follow below.
This project starts with a vanilla TemplateStudio WinUI 3 (v1.1.5) desktop template that uses the CommunityToolkit Mvvm package (with Messenger) and a single page, MainPage. When the App launches, it starts a RandomMessageGenerator thread that periodically issues a TraceMessage using the WeakReferenceMessenger.Default channel from the Toolkit. The UI thread receives these messages and stores them in a List.

The user may create a StatusWindow (a secondary Window on the UI thread) by toggling a switch on MainPage. The StatusWindow should open, request and load previous messages from the App, then register for new TraceMessages. All TraceMessages (including new ones) are displayed in a ListView on the StatusWindow.

The problem is that, on the first new TraceMessage sent after the StatusWindow has been opened, the same WeakReferenceMessenger.Default.Send() method that has been happily sending messages between the RandomMessageGenerator thread and the UI thread throws a "The application called an interface that was marshalled for a different thread. (0x8001010E (RPC_E_WRONG_THREAD))" exception and the RandomMessageGenerator thread dies.

The exception is thrown by the WeakReferenceMessenger.Default.Send(tm); statement in the RandomMessageGenerator() method. I assume the issue is in the IMessenger interface (the only interface involved here). Briefly, as I understand it, this interface builds a table of subscribed receivers for each message type. Each receiver is then signaled on each Send().

One possibility is that references in the receiver list are all assumed to marshal to the same thread. If that were so, none of the messages between threads would work but they do before the StatusWindow is opened so that's unlikely. Changing the list of receivers is an obvious place where threading issues might occur. As WeakReferenceMessenger.Default is thread-safe, I thought adding (and deleting) registered receivers would be thread-safe but doesn't seem to be the case here. Finally, it could be the message itself (a string in this case) that is at fault. I don't know for sure but assumed that the Send method took a private copy of the message to deliver to the marshaled thread.

Could any of you please help me understand the mistake I've made here?

The latest commit shows how to fix this threading-related issue. Basically, message receive handlers run on the thread the message was sent from. If the handler uses objects created on a different thread (from the message sender), you have to marshal any updates to the correct thread. The StatusWindow class now does this in its StatusMessages_Loaded handler where it registers to receive TraceMessage messages.
