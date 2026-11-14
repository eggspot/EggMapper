namespace EggMapper.Internal;

internal static class ExceptionExtensions
{
    internal static bool IsFatal(this Exception exception) =>
        exception is OutOfMemoryException
            or StackOverflowException
            or ThreadAbortException
            or ThreadInterruptedException
            or AccessViolationException;
}
