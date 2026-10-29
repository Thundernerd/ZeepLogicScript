using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Tests;

/// <summary>
/// Runs code on a thread with a quarter of the 1 MB stack a Windows main thread gets, so recursion that only just fits
/// the test runner's larger stacks still fails here.
/// </summary>
internal static class SmallStack
{
    private const int Size = 256 * 1024;

    public static T Run<T>(Func<T> function)
    {
        T result = default;
        Exception exception = null;
        Thread thread = new(
            () =>
            {
                try
                {
                    result = function();
                }
                catch (Exception e)
                {
                    exception = e;
                }
            },
            Size);
        thread.Start();
        thread.Join();
        if (exception != null)
            ExceptionDispatchInfo.Capture(exception).Throw();

        return result;
    }
}
