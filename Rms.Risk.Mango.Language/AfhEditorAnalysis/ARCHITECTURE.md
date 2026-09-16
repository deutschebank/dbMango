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

# AFH Context-Aware IntelliSense Architecture

## Overview

This document describes the architecture of the AFH (Aggregation for Humans) context-aware intellisense system, which provides intelligent code completion and stage syntax hints in the CodeMirror editor.

## Components

### 1. Core Analysis Engine (`Rms.Risk.Mango.Language.AfhEditorAnalysis/`)

The analysis engine is a .NET library that provides error-tolerant AFH parsing and context inference:

- **AfhStageCatalog.cs**: Centralized metadata for all 11 AFH stages
  - Stage keywords (WHERE, ADD, PROJECT, GROUP, SORT, BUCKET, FACET, JOIN, UNWIND, REPLACE, DO)
  - Syntax previews (e.g., "WHERE <expression>")
  - Completion templates for each stage
  - Valid stage continuations after each stage

- **AfhEditorAnalysis.cs**: DTOs for analysis results
  - `AfhDiagnostic`: Parse errors, warnings, and info messages
  - `ActiveStageContext`: Current stage info, nesting level, JSON block detection
  - `AfhCompletion`: Rich completion item with display text and detail
  - `AfhEditorAnalysis`: Full analysis result combining all above

- **TolerantAfhParser.cs**: Error-tolerant ANTLR-based parser
  - Uses ANTLR in error-recovery mode (not bail-on-first-error)
  - Collects parse diagnostics without throwing exceptions
  - Falls back to token-based heuristics when full parsing fails
  - Infers nesting levels, detects JSON blocks, estimates stage context
  - Generates completion suggestions from stage metadata

- **AfhEditorAnalyzerFactory.cs**: Factory pattern for analyzer creation
- **AfhEditorAnalysisService.cs**: Public static API for internal use

### 2. JSInterop Bridge (`Rms.Risk.Mango/Services/AfhEditorAnalysisJsInteropService.cs`)

Direct .NET exposure for JavaScript calls via Blazor JSInterop:

```csharp
[JSInvokable("AfhEditorAnalysisService.AnalyzeScript")]
public static AfhEditorAnalysis AnalyzeScript(string script, int cursorLine, int cursorColumn)
```

Methods:
- `AnalyzeScript()`: Analyze script at cursor position (main intellisense method)
- `GetAllStages()`: Get all stage metadata
- `GetStageMetadata()`: Get metadata for a specific stage
- `GetStageKeywords()`: Get all available keywords

### 3. CodeMirror Integration (`wwwroot/scripts/utils.js`)

JavaScript layer that integrates analysis with CodeMirror:

- **fetchAfhAnalysis(editor)**: 
  - Primary method: Uses JSInterop to call C# analysis directly (when in Blazor context)
  - Fallback: HTTP API to `/api/afheditor/analyze` (for standalone or external use)
  - Caches results per editor instance using WeakMap
  - Debounced 300ms to avoid excessive calls

- **afhScriptHint(editor, options)**:
  - CodeMirror hint function that provides intelligent completions
  - Uses cached analysis results
  - Filters completions based on token text
  - Falls back to static keyword list if analysis unavailable

- **updateAfhAnalysisCache(editor)**:
  - Called on editor change events
  - Triggers debounced analysis fetch

### 4. REST API (`Rms.Risk.Mango/Controllers/AfhEditorController.cs`)

External API endpoints for clients outside dbMango:

- `POST /api/afheditor/analyze` - Analyze script at cursor
- `GET /api/afheditor/stages` - List all stages
- `GET /api/afheditor/stages/{keyword}` - Get stage metadata
- `GET /api/afheditor/stage-keywords` - List keywords

Kept for backward compatibility and external API use.

## Data Flow

### Within dbMango (Preferred - Direct JSInterop)

```
CodeMirror Editor Change
  ↓
updateAfhAnalysisCache() [debounced 300ms]
  ↓
fetchAfhAnalysis() [JSInterop path]
  ↓
DotNet.invokeMethodAsync('AfhEditorAnalysisService.AnalyzeScript', ...)
  ↓
AfhEditorAnalysisJsInteropService.AnalyzeScript() [.NET]
  ↓
AfhEditorAnalysisService.AnalyzeForIntellisense() [core analysis]
  ↓
TolerantAfhParser [ANTLR parsing + fallback]
  ↓
AfhEditorAnalysis [result JSON]
  ↓
Cache (WeakMap)
  ↓
afhScriptHint() [filtered completions]
  ↓
CodeMirror popup
```

### External Clients (HTTP API)

```
POST /api/afheditor/analyze
  ↓
AfhEditorController.Analyze()
  ↓
AfhEditorAnalysisService.AnalyzeForIntellisense()
  ↓
[Same core analysis path as above]
  ↓
JSON response
```

## Performance Optimizations

1. **Debouncing**: Analysis requests debounced 300ms during typing
2. **Caching**: Results cached per editor instance using WeakMap (auto-cleanup)
3. **Direct JSInterop**: Avoids HTTP round-trip overhead when inside Blazor
4. **Fallback HTTP API**: Seamlessly falls back if JSInterop unavailable
5. **Error Recovery**: Parser continues analyzing incomplete scripts without throwing

## Error Handling

- All errors are caught and returned as diagnostics in the analysis result
- Parser never throws; always returns a valid `AfhEditorAnalysis` object
- Lexer and parser errors collected separately and merged
- Token-based fallback kicks in automatically if full parsing fails

## Testing

Comprehensive test suite (`Tests.Rms.Risk.Mango.Language.AfhEditorAnalysisTests.cs`) with 24 tests covering:

- Empty, complete, broken, and incomplete scripts
- All 11 AFH stages
- Cursor positioning accuracy
- Nested pipeline detection
- JSON block identification
- Multi-line script handling
- Error recovery scenarios
- Missing braces and commas
- Partial syntax

**Result: 24/24 tests passing ✅**

## Configuration Points

No configuration required. The system is self-contained and works out-of-the-box.

### Optional: Adjust debounce delay

In `utils.js`, modify `AFH_ANALYSIS_DEBOUNCE_MS`:
```javascript
const AFH_ANALYSIS_DEBOUNCE_MS = 300; // Change as needed
```

## Future Enhancements

1. **Persistent syntax preview**: Show current stage syntax in a dedicated UI widget (not just popup)
2. **Parameter hints**: Show valid parameters as user types inside a stage
3. **Variable resolution**: Track defined variables and suggest them in expressions
4. **Import statements**: Support AFH module/template imports
5. **Caching strategy**: Cache analysis results on disk for faster startup
6. **Language server**: Implement LSP for use with other editors

## Maintenance Notes

- Keep `AfhStageCatalog` updated when new stages are added to the grammar
- `TolerantAfhParser` uses ANTLR-generated parser classes (`MongoAggregationForHumansParser`, `MongoAggregationForHumansLexer`)
- Regenerate ANTLR classes if `MongoAggregationForHumans.g4` changes
- JSInterop method names must match between C# `[JSInvokable]` attribute and JS `invokeMethodAsync` calls
