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
namespace Rms.Risk.Mango.Services;

/// <summary>
/// Temporary files storage. All files there will be erased when workflow finishes (even if failed).
/// </summary>
public interface ITempFileStorage
{
    /// <summary>
    /// Local folder that persist between workflow runs. However, contents of this folder can be cleared without any warnings.
    /// Use for persistent caching only. Do not use for storage.
    /// </summary>
    string LocalPersistentFolder { get; }
    /// <summary>
    /// Temporary files folder path. It's not recommended to use. Better use <see cref="GetTempFileName(System.Type)"/>
    /// </summary>
    string TempFolder { get; }
    /// <summary>
    /// Create a new temp file name. You can't control its name, but name will contain type name of "t" argument.
    /// </summary>
    /// <param name="t">Type that requested the temp file creation</param>
    /// <returns>Unique temp file name</returns>
    string GetTempFileName(Type t);

    /// <summary>
    /// Create a new temp file name. You can't control its name, but name will contain type name of "t" argument.
    /// </summary>
    /// <returns>Unique temp file name</returns>
    string GetTempFileName<T>();
    /// <summary>
    /// Create a new temp file name. You can't control its name, but name will contain type name of "t" argument.
    /// </summary>
    /// <param name="key">Type name that requested the temp file creation or any other indicative string</param>
    /// <returns>Unique temp file name</returns>
    string GetTempFileName(string key);
}