using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bot.Api;

namespace Bot.Remora
{
    internal static class RemoraSlashCommandArgumentExtensions
    {
        public static T GetRequired<T>(this IReadOnlyDictionary<string, object> arguments, string key)
        {
            if (!arguments.TryGetValue(key, out var value) || value is null)
            {
                throw new ArgumentException($"Required argument '{key}' was not provided.");
            }
            if (value is not T typed)
            {
                throw new ArgumentException($"Argument '{key}' is not of expected type {typeof(T).Name}.");
            }
            return typed;
        }

        public static T? GetOptional<T>(this IReadOnlyDictionary<string, object> arguments, string key) where T : class
        {
            if (!arguments.TryGetValue(key, out var value) || value is null)
            {
                return null;
            }
            return value as T;
        }

        public static bool GetBool(this IReadOnlyDictionary<string, object> arguments, string key, bool defaultValue)
        {
            if (!arguments.TryGetValue(key, out var value) || value is null)
            {
                return defaultValue;
            }
            return value is bool b ? b : defaultValue;
        }
    }
}
