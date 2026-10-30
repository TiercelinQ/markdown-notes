using System.Runtime.ExceptionServices;

namespace MarkdownNotes.Tests.Support;

/// <summary>Runs code on a single-threaded apartment thread, required to build WPF controls.</summary>
internal static class StaThread
{
    public static void Run(Action action)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ExceptionDispatchInfo.Capture(ex);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}
