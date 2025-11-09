using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanDownloads.Extensions;

public static class DictionaryExtensions
{
    public static Task<TValue> GetValueOrWaitDefaultAsync<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<Task<TValue>> defaultValueProvider)
    {
        if (dictionary.TryGetValue(key, out var value))
            return Task.FromResult(value);

        return defaultValueProvider();
    }
}