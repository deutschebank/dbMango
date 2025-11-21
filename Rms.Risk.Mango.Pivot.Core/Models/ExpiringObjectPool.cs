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
﻿using System.Collections.Concurrent;

namespace Rms.Risk.Mango.Pivot.Core.Models;

public class ExpiringObjectPool<TKey, TValue, TArg>
    where TValue : class
    where TKey : notnull
{
    public static TimeSpan DefaultExpiryDuration  = TimeSpan.FromHours(3);
    public static TimeSpan DefaultCleanupInterval = TimeSpan.FromMinutes(5); 

    private readonly ConcurrentDictionary<TKey, (TValue Item, DateTime Expiry)> _pool;
    private readonly Func<TKey, TArg, CancellationToken, Task<TValue>>          _objectGenerator;
    private readonly TimeSpan                                                   _expiryDuration;
    private readonly Timer                                                      _cleanupTimer;
    private readonly ConcurrentDictionary<TKey, Task<TValue>>                   _loadingTasks = new();

    public ExpiringObjectPool(Func<TKey, TArg, CancellationToken, Task<TValue>> objectGenerator, TimeSpan expiryDuration = default, TimeSpan cleanupInterval = default )
    {
        if ( cleanupInterval == TimeSpan.Zero )
            cleanupInterval = DefaultCleanupInterval;
        if (expiryDuration == TimeSpan.Zero )
            expiryDuration = DefaultExpiryDuration;

        _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        _expiryDuration  = expiryDuration;
        _pool            = new ConcurrentDictionary<TKey, (TValue, DateTime)>();

        // Set up a timer to clean up expired objects
        _cleanupTimer = new Timer(CleanupExpiredObjects, null, cleanupInterval, cleanupInterval);
    }

    public void Clear() => _pool.Clear();

    public async Task<TValue> Get(TKey key, TArg arg, CancellationToken token = default)
    {
        if (_pool.TryGetValue(key, out var entry) && entry.Expiry > DateTime.UtcNow)
        {
            return entry.Item;
        }

        // Check if the key is already being loaded
        var loadingTask = _loadingTasks.GetOrAdd(key, _ => LoadNewItem(key, arg, token));

        try
        {
            var newItem = await loadingTask;
            return newItem;
        }
        finally
        {
            // Remove the task once loading is complete
            _loadingTasks.TryRemove(key, out _);
        }
    }

    private async Task<TValue> LoadNewItem(TKey key, TArg arg, CancellationToken token)
    {
        var newItem = await _objectGenerator(key, arg, token);
        _pool[key] = (newItem, DateTime.UtcNow.Add(_expiryDuration));
        return newItem;
    }

    public void ReturnObject(TKey key, TValue item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        _pool[key] = (item, DateTime.UtcNow.Add(_expiryDuration));
    }

    private void CleanupExpiredObjects(object? state)
    {
        var now = DateTime.UtcNow;

        // Remove expired objects
        var expiredKeys = _pool.Where(pair => pair.Value.Expiry <= now).Select(pair => pair.Key).ToList();
        foreach (var key in expiredKeys)
        {
            _pool.TryRemove(key, out _);
        }
    }

    public void Dispose()
    {
        _cleanupTimer.Dispose();
    }

    public void Remove(TKey key)
    {
        _pool.TryRemove(key, out _);
    }
}