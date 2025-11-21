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
﻿namespace Rms.Risk.Mango.Services;

/// <summary>  
/// Provides methods to retrieve documentation for MongoDB commands.  
/// </summary>  
public interface IDocumentationService
{
    /// <summary>  
    /// Attempts to retrieve the documentation for a specified MongoDB command in plain text format.  
    /// </summary>  
    /// <param name="commandName">The name of the MongoDB command.</param>  
    /// <param name="token">A cancellation token to cancel the operation.</param>  
    /// <returns>A task that represents the asynchronous operation. The task result contains the documentation as a string, or null if the documentation could not be retrieved.</returns>  
    Task<string?> TryGetHint(string commandName, CancellationToken token = default);

    /// <summary>  
    /// Attempts to retrieve the documentation for a specified MongoDB command in Markdown format.  
    /// This method fetches the documentation, processes it to extract relevant Markdown content,  
    /// and ensures efficient retrieval by utilizing caching mechanisms.  
    /// </summary>  
    /// <param name="commandName">The name of the MongoDB command.</param>  
    /// <param name="token">A cancellation token to cancel the operation.</param>  
    /// <returns>A task that represents the asynchronous operation. The task result contains the documentation in Markdown format as a string, or null if the documentation could not be retrieved.</returns>  
    Task<string?> TryGetMarkdown(string commandName, CancellationToken token = default);
}