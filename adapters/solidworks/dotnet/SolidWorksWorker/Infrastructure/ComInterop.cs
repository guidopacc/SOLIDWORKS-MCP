using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

namespace SolidWorksMcp.SolidWorksWorker.Infrastructure;

internal static class ComInterop
{
    public static object? Invoke(object target, string methodName, params object?[] args)
    {
        return InvokeInternal(target, methodName, args, byRefIndices: null);
    }

    public static object? InvokeWithByRefArgs(
        object target,
        string methodName,
        object?[] args,
        params int[] byRefIndices)
    {
        return InvokeInternal(target, methodName, args, byRefIndices);
    }

    private static object? InvokeInternal(
        object target,
        string methodName,
        object?[] args,
        int[]? byRefIndices)
    {
        try
        {
            ParameterModifier[]? modifiers = null;
            if (byRefIndices is { Length: > 0 })
            {
                var modifier = new ParameterModifier(args.Length);
                foreach (var index in byRefIndices)
                {
                    modifier[index] = true;
                }

                modifiers = [modifier];
            }

            return target.GetType().InvokeMember(
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.InvokeMethod,
                binder: null,
                target: target,
                args: args,
                modifiers: modifiers,
                culture: CultureInfo.InvariantCulture,
                namedParameters: null);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            throw error.InnerException;
        }
    }

    public static T? GetProperty<T>(object target, string propertyName)
    {
        object? value;

        try
        {
            value = target.GetType().InvokeMember(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty,
                binder: null,
                target: target,
                args: null,
                culture: CultureInfo.InvariantCulture);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            throw error.InnerException;
        }

        if (value is null)
        {
            return default;
        }

        return (T?)value;
    }

    public static void SetProperty(object target, string propertyName, object? value)
    {
        try
        {
            target.GetType().InvokeMember(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty,
                binder: null,
                target: target,
                args: [value],
                culture: CultureInfo.InvariantCulture);
        }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        {
            throw error.InnerException;
        }
    }

    public static bool TrySetProperty(object target, string propertyName, object? value)
    {
        try
        {
            SetProperty(target, propertyName, value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static string? InvokeString(object target, string methodName, params object?[] args)
    {
        return Invoke(target, methodName, args) as string;
    }

    public static int ToInt32(object? value)
    {
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    public static bool ToBoolean(object? value)
    {
        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }

    public static void ReleaseIfComObject(object? value)
    {
        if (value is null || !Marshal.IsComObject(value))
        {
            return;
        }

        try
        {
            Marshal.FinalReleaseComObject(value);
        }
        catch
        {
            // Best-effort release only.
        }
    }
}
