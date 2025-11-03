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
﻿namespace Rms.Risk.Mango.Interfaces;

public record MenuItem
(
    string Menu,
    string Title,
    string Url
);

/// <summary>
/// Provides methods to manage and retrieve menu items and menus.
/// </summary>
public interface IMenuService
{
    /// <summary>
    /// Adds a new menu item to a specified menu.
    /// </summary>
    /// <param name="menu">The name of the menu to which the item will be added.</param>
    /// <param name="title">The title of the menu item.</param>
    /// <param name="url">The URL associated with the menu item.</param>
    public void AddMenuItem(string menu, string title, string url);

    /// <summary>
    /// Retrieves all menu items for a specified menu.
    /// </summary>
    /// <param name="menu">The name of the menu whose items are to be retrieved.</param>
    /// <returns>A list of <see cref="MenuItem"/> objects for the specified menu.</returns>
    public List<MenuItem> Get(string menu);

    /// <summary>
    /// Retrieves a list of all available menu names.
    /// </summary>
    /// <returns>A list of menu names.</returns>
    public List<string> GetMenus();
}

