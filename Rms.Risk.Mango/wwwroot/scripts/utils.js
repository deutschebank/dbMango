
var DashboardUtils = {};

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

function afhScriptHint(editor, options) {
    // Find the token at the cursor
    var
        cur = editor.getCursor(),
        token = editor.getTokenAt(cur)
        ;

    return suggestCommand(cur, token, DashboardUtils.AfhCommands);
};


function multiselectById(id) {
    $(id).multiselect();
}

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

CodeMirror.registerHelper("hint", "afh",        afhScriptHint    );
CodeMirror.registerHelper("hint", "javascript", mongodbScriptHint);

let timeout = null;
DashboardUtils.LoadCodeEditor = function (elementid, mode, refElement, dontNetObjRef, methodName, isReadOnly) {
    
    if (isReadOnly === undefined) {
        isReadOnly = false;
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

            });
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
    if (!codemirrorEditor || !codemirrorEditor.codeMirrorLink) {
        return;
    }
    var existing = codemirrorEditor.codeMirrorLink.getDoc().getValue(value);
    if (existing == value)
        return;
    codemirrorEditor.codeMirrorLink.getDoc().setValue(value);
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
        
