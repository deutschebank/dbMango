/* 
 *                                dbMango
 *
 * Copyright 2025 Deutsche Bank AG
 * SPDX-License-Identifier: Apache-2.0
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System.Collections.Concurrent;

namespace Rms.Risk.Mango.Pivot.Core.Models;

/// <summary>
/// A thread-safe dictionary with elements that expire after a specified duration.
/// Accessing an element renews its expiration time.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
public class ExpiringConcurrentDictionary<TKey, TValue> : IDisposable where TValue : class where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, (TValue Value, DateTime Expiration)> _dictionary = new();
    private readonly TimeSpan                                                        _expirationDuration;
    private readonly bool                                                            _shouldDispose;
    private readonly Func<TKey, TValue>?                                             _elementFactory;
    private readonly Timer                                                           _cleanupTimer;
    private          bool                                                            _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpiringConcurrentDictionary{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="expirationDuration">The duration after which elements expire.</param>
    /// <param name="elementFactory">The factory method to create elements for missing keys.</param>
    /// <param name="cleanupInterval">The interval at which expired elements are removed.</param>
    /// <param name="shouldDispose">Indicates whether elements should be disposed when removed.</param>
    public ExpiringConcurrentDictionary(Func<TKey, TValue> elementFactory, TimeSpan expirationDuration, TimeSpan cleanupInterval, bool shouldDispose = true)
        : this(expirationDuration, cleanupInterval, shouldDispose)
    {
        _elementFactory = elementFactory ?? throw new ArgumentNullException(nameof(elementFactory));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ExpiringConcurrentDictionary{TKey, TValue}"/> class.
    /// Element factory must be provided later via <see cref="GetOrAdd(TKey, Func{TKey, TValue}?)"/>.
    /// </summary>
    /// <param name="expirationDuration">The duration after which elements expire.</param>
    /// <param name="cleanupInterval">The interval at which expired elements are removed.</param>
    /// <param name="shouldDispose">Indicates whether elements should be disposed when removed.</param>
    public ExpiringConcurrentDictionary(TimeSpan expirationDuration, TimeSpan cleanupInterval, bool shouldDispose = true)
    {
        _expirationDuration = expirationDuration;
        _shouldDispose      = shouldDispose;
        _elementFactory     = null;
        _cleanupTimer       = new(_ => RemoveExpiredElements(), null, cleanupInterval, cleanupInterval);
    }

    ~ExpiringConcurrentDictionary()
    {
        Dispose(false);
    }

    /// <summary>
    /// Gets or adds an element by key. If the element exists and is not expired, its expiration is renewed.
    /// If the element does not exist or is expired, a new element is created using the factory method.
    /// </summary>
    /// <param name="key">The key of the element.</param>
    /// <param name="elementFactory">The factory method to create the element if it does not exist or is expired.</param>
    /// <returns>The element associated with the key.</returns>
    public TValue GetOrAdd(TKey key, Func<TKey, TValue>? elementFactory = null)
    {
        while (true)
        {
            var now = DateTime.UtcNow;
            if (_dictionary.TryGetValue(key, out var entry))
            {
                if (entry.Expiration > now)
                {
                    // Renew expiration and return the value
                    _dictionary[key] = (entry.Value, now.Add(_expirationDuration));
                    return entry.Value;
                }
                else
                {
                    // Remove expired entry
                    RemoveEntry(key, entry.Value);
                }
            }

            // Create a new value using the factory method
            var factory  = elementFactory ?? _elementFactory;
            if (factory == null)
            {
                throw new InvalidOperationException("Element factory is not specified.");
            }
            var newValue = factory(key);
            var newEntry = (newValue, now.Add(_expirationDuration));
            if (_dictionary.TryAdd(key, newEntry))
            {
                return newValue;
            }
        }
    }

    public void Clear()
    {
        var values = _dictionary.Values.ToList();
        _dictionary.Clear();

        foreach (var entry in values)
        {
            DisposeIfNecessary(entry.Value);
        }
    }

    /// <summary>
    /// Removes expired elements from the dictionary.
    /// If elements implement <see cref="IDisposable"/>, they are disposed upon removal.
    /// </summary>
    public void RemoveExpiredElements()
    {
        var now = DateTime.UtcNow;
        foreach (var key in _dictionary.Keys)
        {
            if (_dictionary.TryGetValue(key, out var entry) && entry.Expiration <= now)
            {
                RemoveEntry(key, entry.Value);
            }
        }
    }

    /// <summary>
    /// Removes an element by key.
    /// If the element implements <see cref="IDisposable"/>, it is disposed upon removal.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns>True if the element was removed; otherwise, false.</returns>
    public bool TryRemove(TKey key)
    {
        if (_dictionary.TryRemove(key, out var entry))
        {
            DisposeIfNecessary(entry.Value);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Disposes the dictionary and its elements if they implement <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _cleanupTimer.Dispose();
                foreach (var entry in _dictionary.Values)
                {
                    DisposeIfNecessary(entry.Value);
                }
                _dictionary.Clear();
            }

            _disposed = true;
        }
    }

    private void RemoveEntry(TKey key, TValue value)
    {
        if (_dictionary.TryRemove(key, out _))
        {
            DisposeIfNecessary(value);
        }
    }

    private void DisposeIfNecessary(TValue value)
    {
        if (_shouldDispose && value is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
    /// <summary>
    /// Determines whether the dictionary contains the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the dictionary.</param>
    /// <returns>True if the dictionary contains the key; otherwise, false.</returns>
    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    /// <summary>
    /// Gets the count of elements in the dictionary.
    /// </summary>
    public int Count => _dictionary.Count;
}