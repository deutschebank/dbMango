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
﻿using Rms.Risk.Mango.Pivot.Core.Models;

namespace Tests.Rms.Risk.Mango;

[TestFixture]
public class ExpiringConcurrentDictionaryTests
{
    [Test]
    public void GetOrAdd_ShouldAddAndRetrieveValue()
    {
        // Arrange  
        var dictionary = new ExpiringConcurrentDictionary<string, string>(
            key => $"Value for {key}",
            TimeSpan.FromSeconds(2.5),
            TimeSpan.FromSeconds(0.5)
        );

        // Act  
        var value = dictionary.GetOrAdd("TestKey");

        // Assert  
        Assert.AreEqual("Value for TestKey", value);
    }

    [Test]
    public void GetOrAdd_ShouldRenewExpiration()
    {
        // Arrange  
        var dictionary = new ExpiringConcurrentDictionary<string, string>(
            key => $"Value for {key}",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(0.5)
        );

        // Act  
        var value1 = dictionary.GetOrAdd("TestKey");
        Thread.Sleep(1000); // Wait for 1 second  
        var value2 = dictionary.GetOrAdd("TestKey");

        // Assert  
        Assert.AreEqual(value1, value2);
    }

    [Test]
    public void RemoveExpiredElements_ShouldRemoveExpiredEntries()
    {
        // Arrange  
        var dictionary = new ExpiringConcurrentDictionary<string, string>(
            key => $"Value for {key}",
            TimeSpan.FromSeconds(0.5),
            TimeSpan.FromSeconds(0.5)
        );

        dictionary.GetOrAdd("TestKey");
        Thread.Sleep(1500); // Wait for expiration  

        // Act  
        dictionary.RemoveExpiredElements();

        // Assert  
        Assert.AreEqual(0, dictionary.Count);
    }

    [Test]
    public void TryRemove_ShouldRemoveSpecificEntry()
    {
        // Arrange  
        var dictionary = new ExpiringConcurrentDictionary<string, string>(
            key => $"Value for {key}",
            TimeSpan.FromSeconds(2.5),
            TimeSpan.FromSeconds(0.5)
        );

        dictionary.GetOrAdd("TestKey");

        // Act  
        var removed = dictionary.TryRemove("TestKey");

        // Assert  
        Assert.IsTrue(removed);
        Assert.AreEqual(0, dictionary.Count);
    }

    [Test]
    public void Dispose_ShouldClearDictionaryAndDisposeElements()
    {
        // Arrange  
        var dictionary = new ExpiringConcurrentDictionary<string, string>(
            key => $"Value for {key}",
            TimeSpan.FromSeconds(2.5),
            TimeSpan.FromSeconds(0.5)
        );

        dictionary.GetOrAdd("TestKey");

        // Act  
        dictionary.Dispose();

        // Assert  
        Assert.AreEqual(0, dictionary.Count);
    }

    [Test]
    public void ContainsKey_ShouldReturnTrueForExistingKey()
    {
        // Arrange  
        var dictionary = new ExpiringConcurrentDictionary<string, string>(
            key => $"Value for {key}",
            TimeSpan.FromSeconds(2.5),
            TimeSpan.FromSeconds(0.5)
        );

        dictionary.GetOrAdd("TestKey");

        // Act  
        var containsKey = dictionary.ContainsKey("TestKey");

        // Assert  
        Assert.IsTrue(containsKey);
    }

    [Test]
    public void ContainsKey_ShouldReturnFalseForExpiredKey()
    {
        // Arrange  
        var dictionary = new ExpiringConcurrentDictionary<string, string>(
            key => $"Value for {key}",
            TimeSpan.FromSeconds(0.5),
            TimeSpan.FromSeconds(0.5)
        );

        dictionary.GetOrAdd("TestKey");
        Thread.Sleep(1500); // Wait for expiration  

        // Act  
        var containsKey = dictionary.ContainsKey("TestKey");

        // Assert  
        Assert.IsFalse(containsKey);
    }

    [Test]
    public void ContainsKey_ShouldReturnTrueAfterRenewal()
    {
        // Arrange  
        var dictionary = new ExpiringConcurrentDictionary<string, string>(
            key => $"Value for {key}",
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(0.5)
        );

        dictionary.GetOrAdd("TestKey");
        Thread.Sleep(500); // Wait for half expiration duration  
        dictionary.GetOrAdd("TestKey"); // Renew expiration  

        // Act  
        var containsKey = dictionary.ContainsKey("TestKey");

        // Assert  
        Assert.IsTrue(containsKey);
    }

}