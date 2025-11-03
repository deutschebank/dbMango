# Browse

## Overview and Purpose  
The Browse page is the primary workspace for interactive exploration and controlled manipulation of MongoDB data 
within dbMango. It unifies ad-hoc querying, structured result inspection, lightweight operational metadata, and 
document-level editing under an authorization-aware interface. Users can fluidly move between collections,
compose AFH (Aggregation for Humans) filter expressions, inspect results either as a pivoted grid or 
raw JSON, and-where permitted-perform targeted updates or deletions with auditable command stamping. The page is 
optimized for investigative, diagnostic, and operational tasks where rapid iteration on queries and minimal context 
switching are critical.

## Navigation, Routing, and Context Synchronization  
The component is addressable via route variants supporting database, instance, and collection scoping. 
A `q` query parameter preserves the current AFH filter expression for deep linking and shareable reproducibility.
Each meaningful user action (changing database, instance, collection, or filter) triggers URL synchronization
without a full navigation cycle, allowing the user to bookmark or distribute links that reconstruct the exact state.
This enhances continuity across sessions and reduces onboarding friction.

Examples:

```
https://dbmango.example.com/user/browse"
https://dbmango.example.com/user/browse/MyDatabase/CollectionNameHere
https://dbmango.example.com/user/browse/MyDatabase/InstanceName/CollectionNameHere"
https://dbmango.example.com/user/browse/MyDatabase/CollectionNameHere?q=Amount&gt;100

```

## Authorization Model  
All content is gated behind a `ReadAccess` policy; the page denies rendering of its functional core until that 
is satisfied. Write operations (update and delete) are conditionally exposed after a per-document interaction 
check that requests `WriteAccess` against the active database resource. This layered approach ensures read scenarios
do not degrade due to write entitlement evaluation while preventing accidental surface of modification controls. 
When write access is absent, document dialogs are strictly view-only, eliminating ambiguity.

## Layout and Interaction Flow  
The horizontal split panel establishes a left-hand collection navigator and a right-hand query-and-results workspace.
The collection list is populated asynchronously, grouped under synthetic headers (" Meta", " Cache", " Data") that 
visually segment functional categories without being selectable themselves. Actual collection items are interleaved 
after their group markers and may display real-time statistics: total size (human-readable) and a "Sharded" indicator
when applicable. Collections ending with `-Meta` are visually deemphasized using a neutral grey class, signaling their
systemic nature.

Selecting a collection immediately resets the current filter expression, collapses any autocomplete dropdown, and
updates the route context. This deliberate reset prevents accidental reuse of filters constructed for structurally 
different datasets.

## Query Composition with AFH  
Filter input employs a multiline text area with autocomplete driven by a per-(Database|Instance|Collection) history set.
The AFH expression supplied by the user is internally wrapped into a canonical script fragment:  

```afh
FROM "{Collection}" PIPELINE { WHERE { <user expression> } }  
```

This is parsed into an AST using the language subsystem; any parse error halts execution and presents the exception
content as JSON to avoid silent misinterpretation. A successful parse yields structured JSON pipeline text passed
to the backend aggregation executor. When the filter field is empty or resolves to `{}`, a simple find query is 
issued instead, reducing overhead.

The history mechanism captures unique, case-insensitive filter expressions up to a capped limit (100 per context),
storing them in local browser storage. It is automatically rehydrated on first render, enriching subsequent 
interaction with suggestion recall. History entries are sorted alphabetically on access, but newly executed 
expressions are prepended to reinforce temporal relevance.

## Execution Parameters and Performance Controls  
Timeout governs the maximum duration of the backend operation (find or aggregate). This converts directly to a 
cancellation token defending against long-running pipelines or network anomalies. "Max fetch size" constrains 
internal batch acquisition for pivot population rather than the logical MongoDB result limit, providing a throttle 
against rendering overhead for large documents. The "Rows" selector influences client-side display density (not 
the query cardinality) and triggers a state update to rerender the pivot grid. These controls together form a 
pragmatic triad: execution ceiling (Timeout), ingestion pacing (Max fetch size), and presentation scope (Rows).

## Result Rendering Modes  
Two mutually exclusive representations are available: pivot grid and raw JSON. The grid leverages `ArrayBasedPivotData`
to dynamically derive headers and maintain typed values, enabling structured scanning, sorting hooks, and 
cell-level interaction. JSON mode serves both as a diagnostic fallback (automatically engaged on exceptions or 
command responses) and as an intentional view when users need unmodified serialization fidelity. A toggle 
("Show as Json") lets users force raw mode if pivot abstraction is undesirable (e.g., deeply nested documents
whose structure resists tabular normalization).

When a query yields no documents, a synthetic placeholder BsonDocument communicates the absence explicitly rather
than presenting an empty visual target, preserving cognitive continuity.

## Document-Level Interaction  
Clicking within the pivot grid resolves the underlying `_id` across the backing `ResultBson` collection. A modal
dialog is launched displaying the selected document. If the user possesses write authorization, the dialog enables 
either update (full document replacement) or deletion. The system prompts for confirmation before any mutation, 
then constructs the corresponding MongoDB command (`update` or `delete`) embedding the `_id` match predicate.
Each command is annotated with operational metadata (ticket and user email) to enforce auditing standards.

Update failure (non-single modification) or delete failure (non-single deletion) surfaces both the server response
and the original command as concatenated JSON blocks, facilitating rapid post-mortem analysis without external log 
correlation. Success responses are intentionally terse, delivered through confirmation dialogs.

## State Persistence and Resilience  
Volatile UI state-selected collection, filter expression, row display count, and historical filters-is serialized 
to local storage under a stable key ("Browse"). Retrieval occurs during initial render with a defensive timeout; 
transient errors in loading or saving are swallowed deliberately to avoid compromising core functionality for ancillary
persistence concerns. This local-first design avoids server round trips while respecting per-context isolation.

## Asynchronous Behavior and Concurrency  
Initial collection enumeration, stats retrieval, and pivot data fetching execute on background tasks governed by 
linked cancellation tokens. Component disposal propagates cancellation to all outstanding operations, preventing 
resource leaks and race conditions after navigation away from the page. Short-lived secondary tokens (5-second windows)
shield auxiliary calls (list collections, stats) from indefinite latency when infrastructure load spikes.

## Error Handling Philosophy  
Errors during parsing, execution, or stats retrieval are surfaced promptly in JSON view, maintaining transparency.
Non-critical failures (history save, individual stat acquisition for a collection) are intentionally ignored to 
preserve flow. This stratification of error concern levels ensures that core capabilities (query, browse, inspect) 
remain prioritized over embellishments (per-collection metrics for every item).

## Data Modeling in the Pivot  
The pivot infrastructure captures headers once derived, caches column type hints, and supports future extensions 
like custom sort or column projections. Each query result set is transformed into an `ArrayBasedPivotData` instance
with a stable identifier ("find"), supporting cell handlers for user interactions without binding deeply to raw Bson
structures. This abstraction enables uniform treatment of heterogeneous documents by projecting them into a consistent
visual schema.

## Operational Usage Pattern  
A typical investigative session involves choosing a collection, drafting an AFH expression (often incrementally refined),
executing with a tuned timeout and fetch size, inspecting structured output, drilling into specific documents, optionally
performing corrective edits under audit, and bookmarking the resulting URL for future replication. The design emphasizes
low friction between these steps: minimal blocking reloads, persistent context memory, and instantaneous toggling between
raw and structured modes.

## Safeguards and Integrity Measures  
Mutation pathways require explicit user confirmation, validated authorization, and successful ticket retrieval from an
external execution gate (`Shell.CanExecuteCommand`). Absence of any prerequisite silently aborts the operation without 
exposing unauthorized flows. Full document replacement semantics ensure predictable outcome (no partial patch ambiguity),
while reliance on `_id` guarantees precise target identification.

## Visual Indicators and Semantic Cues  
Group headers prefixed by a space serve as non-interactive separators; their formatting differentiates them from 
actionable collection items. The "Sharded" badge informs users of potential distribution-induced latency considerations.
Grey styling for meta collections provides a subtle cognitive hint to treat those datasets cautiously (often schema metadata
rather than business entities).

## Summary  
The Browse page synthesizes adaptive query input, intelligent result shaping, contextual persistence, and guarded 
modification into a cohesive tool optimized for MongoDB operational insight. Its architecture balances responsiveness 
with safety: asynchronous loading, cancellable tasks, structured error visibility, and tightly scoped authorization gates.
By embedding AFH parsing, pivot transformation, and local session memory, it empowers users to iterate rapidly, diagnose 
issues, and act confidently within their data landscape.