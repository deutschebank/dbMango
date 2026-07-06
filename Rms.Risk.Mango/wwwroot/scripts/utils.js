
window.DashboardUtils = window.DashboardUtils || {};
var DashboardUtils = window.DashboardUtils;

DashboardUtils.GetLocalTimeZoneOffset = function() {
  return new Date().getTimezoneOffset();
}

DashboardUtils.GetUtcDate = function() {
  return new Date().toISOString();
}

//Set the text on the browser tab
DashboardUtils.SetDocumentTitle = function (title) {
    document.title = title;
};

DashboardUtils.ChangeUrl = function (url) {
    history.pushState(null, '', url);
};

DashboardUtils.ChangeLocationHash = function(hash) {
    document.location.hash = hash;
}

DashboardUtils.CopyToClipboard = function(text) {
    navigator.clipboard.writeText(text);
}

DashboardUtils.PasteFromClipboard = async function() {
    try {
        // Check if clipboard-read permission is granted or prompt the user
        const permissionStatus = await navigator.permissions.query({ name: "clipboard-read" });

        if (permissionStatus.state === "granted" || permissionStatus.state === "prompt") {
            // Read text from the clipboard
            return await navigator.clipboard.readText();
        } else {
            console.error("Clipboard access denied by the user or browser.");
            return null;
        }
    } catch (err) {
        console.error("Failed to read from clipboard:", err);
        return null;
    }
};

DashboardUtils.SetFavicon = function (url) {
    var link = document.querySelector("link[rel~='icon']");
    if (!link) {
        link = document.createElement('link');
        link.rel = 'icon';
        document.getElementsByTagName('head')[0].appendChild(link);
    }
    link.href = url;
};

DashboardUtils.MongoDbCommands = ["hello", "collStats" ];

DashboardUtils.SetMongoDbCommands = function(commands) {
    DashboardUtils.MongoDbCommands = commands;
}

DashboardUtils.AfhCommands = [
    "AND"    , "AS"     , "ASC", "AUTO"    , "BOUNDARIES", "BUCKET" , "BUCKETS", "BY",
    "DEFAULT", "DESC"   , "DO" , "EXCLUDE" , "EXISTS"    , "FACET"  , "FROM"   , "GRANULARITY",
    "GROUP"  , "ID"     , "IN" , "INDEX"   , "IS"        , "JOIN"   , "LET"    , "NOT",
    "ON"     , "OPTIONS", "OR" , "PIPELINE", "PROJECT"   , "REPLACE", "SORT"   , "UNWIND",
    "WHERE"
];

DashboardUtils.SetAfhCommands = function (commands) {
    DashboardUtils.AfhCommands = commands;
}


function searchBackwards(cur, editor, match) {
    var lineTokens = editor.getLineTokens(cur.line).filter(t => t.start < cur.ch);

    // traverse back

    var matched = match(lineTokens);
    if (matched == null) {
        var line = cur.line;
        while (matched == null && line > 0) {
            lineTokens = editor.getLineTokens(line);
            matched = match(lineTokens);
            line -= 1;
        }
    }
    return matched;
}


function suggestCommand(cur, token, commands) {
    var suggestions = commands
        .filter(x => token.type == null || x.toLowerCase().startsWith(token.string.toLowerCase().replace(/^['"]/, '')))
        .map(x => token.string.startsWith('"') || token.string.startsWith("'") ? token.string[0] + x + token.string[0] : x)
        .sort();

    var hint = {
        list: suggestions,
        from: CodeMirror.Pos(cur.line, token.start),
        to: CodeMirror.Pos(cur.line, token.end)
    };

    return hint;
}

function mongodbScriptHint(editor, options) {
    // Find the token at the cursor
    var
        cur = editor.getCursor(),
        token = editor.getTokenAt(cur)
        ;

    return suggestCommand(cur, token, DashboardUtils.MongoDbCommands);
};

// Cache for AFH editor analysis results per editor instance
let afhAnalysisCache = new WeakMap();
let afhAnalysisTimeout = null;

// Debounce delay for AFH analysis requests (ms)
const AFH_ANALYSIS_DEBOUNCE_MS = 300;

/**
 * Fetch analysis from the AFH editor analysis service via JSInterop.
 * Caches result per editor instance.
 * Uses direct .NET method invocation through Blazor JSInterop.
 */
async function fetchAfhAnalysis(editor) {
    try {
        const script = editor.getValue();
        const cursor = editor.getCursor();

        // JSInterop call to .NET analysis service
        // Format: DotNet.invokeMethodAsync(assemblyName, 'ClassName.MethodName', ...args)
        const analysis = await DotNet.invokeMethodAsync(
            'Rms.Risk.Mango',
            'AfhEditorAnalysisService.AnalyzeScript',
            script,
            cursor.line,
            cursor.ch
        );

        if (analysis) {
            afhAnalysisCache.set(editor, analysis);
            return analysis;
        }

        return null;
    } catch (err) {
        console.error('Error fetching AFH analysis:', err);
        return null;
    }
}

function createAfhSuggestion(text, displayText, detail) {
    return {
        text: text,
        displayText: displayText,
        detail: detail,
        render: function(element, _self, _data) {
            const row = document.createElement("div");
            row.style.display = "grid";
            row.style.gridTemplateColumns = "180px minmax(220px, 1fr)";
            row.style.columnGap = "10px";
            row.style.alignItems = "center";
            row.style.width = "100%";

            const leftCell = document.createElement("span");
            leftCell.textContent = displayText;
            leftCell.style.fontWeight = "600";
            leftCell.style.overflow = "hidden";
            leftCell.style.textOverflow = "ellipsis";
            leftCell.style.whiteSpace = "nowrap";

            const rightCell = document.createElement("span");
            rightCell.textContent = detail || "";
            rightCell.style.opacity = "0.75";
            rightCell.style.color = "#9aa0a6";
            rightCell.style.overflow = "hidden";
            rightCell.style.textOverflow = "ellipsis";
            rightCell.style.whiteSpace = "nowrap";

            row.appendChild(leftCell);
            row.appendChild(rightCell);
            element.appendChild(row);
        }
    };
}

function getAfhPrefix(token) {
    if (!token || !token.string) {
        return "";
    }
    return token.string.toLowerCase().replace(/^['"]/, "");
}

function getPreviousNonWhitespaceChar(editor, cur, token) {
    let line = cur.line;
    let ch = (token && typeof token.start === "number") ? token.start - 1 : cur.ch - 1;

    while (line >= 0) {
        const text = editor.getLine(line) || "";
        while (ch >= 0) {
            const c = text[ch];
            if (!/\s/.test(c)) {
                return c;
            }
            ch--;
        }

        line--;
        if (line >= 0) {
            ch = (editor.getLine(line) || "").length - 1;
        }
    }

    return null;
}

function shouldSuggestStageKeywords(editor, cur, token, analysis) {
    if (analysis && analysis.currentStage && analysis.currentStage.isInsideJsonBlock) {
        return false;
    }

    const previous = getPreviousNonWhitespaceChar(editor, cur, token);
    if (previous === "," || previous === ":" || previous === "=" || previous === "(" || previous === "[" || previous === ".") {
        return false;
    }

    return true;
}

function collectExistingAfhIdentifiers(editor, prefix) {
    const text = editor.getValue() || "";
    const keywordSet = new Set(DashboardUtils.AfhCommands.map(x => x.toUpperCase()));
    const identifiers = new Set();

    const matches = text.match(/\b[A-Za-z_][A-Za-z0-9_.]*\b/g) || [];
    matches.forEach(word => {
        const upper = word.toUpperCase();
        if (keywordSet.has(upper)) {
            return;
        }

        if (prefix && !word.toLowerCase().startsWith(prefix)) {
            return;
        }

        identifiers.add(word);
    });

    return Array.from(identifiers).sort((a, b) => a.localeCompare(b));
}

/**
 * Enhanced AFH hinting with context-aware completions.
 */
function afhScriptHint(editor, options) {
    var cur = editor.getCursor();
    var token = editor.getTokenAt(cur);
    var prefix = getAfhPrefix(token);

    // Try to use cached analysis
    var analysis = afhAnalysisCache.get(editor);

    // Fallback to simple keyword completion if analysis not available
    if (!analysis) {
        return suggestCommand(cur, token, DashboardUtils.AfhCommands);
    }

    const allowStageSuggestions = shouldSuggestStageKeywords(editor, cur, token, analysis);
    const stageKeywordSet = new Set(["WHERE", "BUCKET", "FACET", "ADD", "PROJECT", "GROUP", "SORT", "JOIN", "UNWIND", "REPLACE", "DO"]);

    let suggestions = [];

    if (analysis.completions && analysis.completions.length > 0) {
        suggestions = analysis.completions
            .filter(x => {
                const display = (x.displayText || "").toLowerCase();
                if (prefix && !display.startsWith(prefix)) {
                    return false;
                }

                const isStage = (x.type && x.type.toLowerCase() === "stage") || stageKeywordSet.has((x.displayText || "").toUpperCase());
                if (!allowStageSuggestions && isStage) {
                    return false;
                }

                return true;
            })
            .map(x => createAfhSuggestion(x.insertText || x.displayText, x.displayText, x.detail));
    }

    // Add existing identifiers (variables/fields/functions) when typing inside a stage expression
    const existingIdentifiers = collectExistingAfhIdentifiers(editor, prefix);
    existingIdentifiers.forEach(identifier => {
        suggestions.push(createAfhSuggestion(identifier, identifier, "existing field/identifier"));
    });

    // De-duplicate by display text
    const seen = new Set();
    suggestions = suggestions.filter(item => {
        const key = (item.displayText || item.text || "").toLowerCase();
        if (seen.has(key)) {
            return false;
        }

        seen.add(key);
        return true;
    });

    // If no suggestions left, fallback to command list
    if (suggestions.length === 0) {
        suggestions = DashboardUtils.AfhCommands
            .filter(x => !prefix || x.toLowerCase().startsWith(prefix))
            .map(x => createAfhSuggestion(x, x, null));
    }

    var hint = {
        list: suggestions,
        from: CodeMirror.Pos(cur.line, token.start),
        to: CodeMirror.Pos(cur.line, token.end)
    };

    console.log('AFH analysis:', hint);

    return hint;
}

/**
 * Update AFH analysis cache when editor content changes.
 * Debounced to avoid excessive API calls.
 */
function updateAfhAnalysisCache(editor) {
    clearTimeout(afhAnalysisTimeout);
    afhAnalysisTimeout = setTimeout(() => {
        fetchAfhAnalysis(editor);
    }, AFH_ANALYSIS_DEBOUNCE_MS);
};


function multiselectById(id) {
    $(id).multiselect();
}

let codeMirrorExtensionsInitialized = false;

function initCodeMirrorExtensions() {
    if (codeMirrorExtensionsInitialized || typeof CodeMirror === "undefined") {
        return;
    }

    if (typeof CodeMirror.defineSimpleMode === "function") {
        // Aggregation Framework for Humans
        CodeMirror.defineSimpleMode("afh", {
            // The start state contains the rules that are initially used
            start: [
                { regex: /\/\*/, token: "comment", next: "comment" },

                // The regex matches the token, the token property contains the type
                { regex: /([-+\/*=<>!\[\]\(\)]+)|(AND|OR)/, token: "operator" },
                { regex: /(?:[A-Z]+)\b/, token: "keyword" },
                { regex: /(?:[A-Za-z][A-Za-z0-9_]*)\s*\:/, token: "argument" },

                { regex: /[A-Za-z][A-Za-z_0-9.]+/, token: "variable1" },
                { regex: /\$(?:[A-Za-z][A-Za-z0-9_\\.]*)/, token: "variable2" },
                { regex: /'(?:[^@\\]|\\.)*?(?:'|$)/, token: "variable3" },

                { regex: /"(?:[^@\\]|\\.)*?(?:"|$)/, token: "string" },
                { regex: /0x[a-f\d]+|[-+]?(?:\.\d+|\d+\.?\d*)(?:e[-+]?\d+)?/i, token: "number" },
                { regex: /\/\/.*/, token: "comment" },
                // A next property will cause the mode to move to a different state

                { regex: /[{};]+/, token: "keyword" },
            ],
            // The multi-line comment state.
            comment: [
                { regex: /.*?\*\//, token: "comment", next: "start" },
                { regex: /.*/, token: "comment" }
            ],
            // The meta property contains global information about the mode. It
            // can contain properties like lineComment, which are supported by
            // all modes, and also directives like dontIndentStates, which are
            // specific to simple modes.
            meta: {
                dontIndentStates: ["comment"],
                lineComment: "//",
            }
        });

        CodeMirror.defineMIME("text/x-afh", "afh");
    }

    if (typeof CodeMirror.registerHelper === "function") {
        CodeMirror.registerHelper("hint", "afh", afhScriptHint);
        CodeMirror.registerHelper("hint", "javascript", mongodbScriptHint);
    }

    codeMirrorExtensionsInitialized = true;
}

let timeout = null;
DashboardUtils.LoadCodeEditor = function (elementid, mode, refElement, dontNetObjRef, methodName, isReadOnly) {
    
    if (isReadOnly === undefined) {
        isReadOnly = false;
    }

    initCodeMirrorExtensions();

    if (typeof CodeMirror === "undefined" || typeof CodeMirror.fromTextArea !== "function") {
        console.error("CodeMirror is not available. Ensure CodeMirror scripts are loaded before utils.js.");
        return;
    }

    //console.log("CodeMirror init for mode: ", mode);

    var textArea = document.getElementById(elementid);
    var codemirrorEditor = CodeMirror.fromTextArea(
        textArea,
        {
            autoRefresh: true,
            styleActiveLine: true,
            matchBrackets: true,
            mode: mode,
            scrollbarStyle: "overlay",
            viewportMargin: Infinity,
            theme: "lucario",
            lineNumbers: true,
            readOnly: isReadOnly,
            extraKeys: {
                "F11": function (cm) {
                    cm.setOption("fullScreen", !cm.getOption("fullScreen"));
                }
                , "Esc": function (cm) {
                    if (cm.getOption("fullScreen")) cm.setOption("fullScreen", false);
                }
                , "Ctrl-Q": function(cm) { cm.foldCode(cm.getCursor()); }
                , "Ctrl-Space": "autocomplete"
                , "Ctrl-F": "findPersistent"
                , "Ctrl-H": "replace"                
            },
            foldGutter: true,
            //gutters: ["CodeMirror-linenumbers", "CodeMirror-foldgutter"]
        });
    //save reference to element
    refElement.codeMirrorLink = codemirrorEditor;

    if (Object.prototype.hasOwnProperty.call(refElement, "pendingCodeMirrorValue")) {
        DashboardUtils.CodeEditor_SetValue(refElement, refElement.pendingCodeMirrorValue);
        delete refElement.pendingCodeMirrorValue;
        codemirrorEditor.refresh();
    }

    //setup code callback
    if (dontNetObjRef) {
        codemirrorEditor.on("change",
            function (editor) {
                clearTimeout(timeout);
                timeout = setTimeout(function () {
                    var val = editor.getValue();
                    dontNetObjRef.invokeMethodAsync(methodName, val);
//                    console.log('Input Value');
                }, 1000);

                // For AFH mode, also update analysis cache
                if (mode === "text/x-afh") {
                    updateAfhAnalysisCache(editor);
                }
            });

        // Initial analysis fetch for AFH mode
        if (mode === "text/x-afh") {
            setTimeout(() => {
                fetchAfhAnalysis(codemirrorEditor);
            }, 500);

            // Trigger autocomplete while typing so AFH hint provider is invoked continuously
            codemirrorEditor.on("inputRead", function (cm, changeObj) {
                if (!changeObj || !changeObj.text || changeObj.text.length === 0) {
                    return;
                }

                const typed = changeObj.text[0];
                if (!typed || !/^[A-Za-z_$]$/.test(typed)) {
                    return;
                }

                if (typeof cm.showHint === "function") {
                    cm.showHint({ completeSingle: false });
                }
            });
        }
    }

    //codemirrorEditor.refresh();
    //codemirrorEditor.setSize(null, 500);
};

DashboardUtils.CodeEditor_SetCaret = function (codemirrorEditor, row, col) {
    setTimeout(() => {
        codemirrorEditor.focus();
        codemirrorEditor.setCursor({
            line: row,
            ch: col,
        });
    }, 0);
}

DashboardUtils.CodeEditor_SetValue = function (codemirrorEditor, value) {
    if (!codemirrorEditor) {
        return;
    }

    if (!codemirrorEditor.codeMirrorLink) {
        codemirrorEditor.pendingCodeMirrorValue = value;
        return;
    }

    var existing = codemirrorEditor.codeMirrorLink.getDoc().getValue();
    if (existing == value)
        return;

    codemirrorEditor.codeMirrorLink.getDoc().setValue(value);
    codemirrorEditor.codeMirrorLink.refresh();
};

DashboardUtils.CodeEditor_Refresh = function (codemirrorEditor) {
    if (!codemirrorEditor || !codemirrorEditor.codeMirrorLink) {
        return;
    }

    codemirrorEditor.codeMirrorLink.refresh();
};

DashboardUtils.CodeEditor_GetValue = function (codemirrorEditor) {
    if (!codemirrorEditor || !codemirrorEditor.codeMirrorLink) {
        return;
    }
    return codemirrorEditor.codeMirrorLink.getValue();
};


DashboardUtils.CodeEditor_InsertTextAtCursor = function (codemirrorEditor, text) {
    if (!codemirrorEditor || !codemirrorEditor.codeMirrorLink) {
        return;
    }
    const doc = codemirrorEditor.codeMirrorLink.getDoc();
    const cursor = doc.getCursor();
    doc.replaceRange(text, cursor);
};

DashboardUtils.CodeEditor_SetParam = function (codemirrorEditor, paramName, paramValue) {
    if (!codemirrorEditor || !codemirrorEditor.codeMirrorLink) {
        return;
    }
    codemirrorEditor.codeMirrorLink.setOption(paramName, paramValue);
};

DashboardUtils.CheckBox = function () {
    var selected = [];
    var chosen;
    $('input[type=checkbox]:checked').each(function () {
        selected.push(this.value);
    });
    
    chosen = selected.toString();
    return chosen;
};
        
