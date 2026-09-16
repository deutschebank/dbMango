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
﻿using Rms.Risk.Mango.Interfaces;

namespace Rms.Risk.Mango.Services;

public class NoChangeNumberChecker : IChangeNumberChecker
{
    public Task<CheckerReply> IsValid(string taskNumber, string email, string? comments, DateTime whenTimeUtc = default)
        => Task.FromResult(new CheckerReply(true, ValidFromUtc : DateTime.UtcNow, ValidToUtc: DateTime.MaxValue));
}
