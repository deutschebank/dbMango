//
//                                dbMango
//
// Copyright 2025 Deutsche Bank AG
// SPDX-License-Identifier: Apache-2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
// 
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//
grammar JsonGrammar;

json
    : object
    | array
    ;

value
    : object
    | array
    | 'true'
    | 'false'
    | 'null'
    | NUMBER
    | STRING
    | VARIABLE
    ;

object
    : '{' pair (',' pair)* '}'
    | '{' '}'
    ;

pair
    : object_name ':' value
    ;

object_name
    : VARIABLE
    | STRING
    ;

array
    : '[' value (',' value)* ']'
    | '[' ']'
    ;

// ---------------------- LEXER ----------------------------

// Fragments
fragment DIGIT    : [0-9];
fragment INT      : '-'? DIGIT+;
fragment EXPONENT : [Ee] [+-]? DIGIT+;
fragment FLOAT    : INT ('.' DIGIT+)? EXPONENT?;

//fragment VERY_COMPLEX_VAR_FRAGMENT : '\'' [^']+ '\'';

fragment VAR_FRAGMENT    : ([a-zA-Z_] [a-zA-Z0-9_]*) | ('$' [a-zA-Z_] [a-zA-Z0-9_.]*) | ('\'' (ESC | ~['\\])* '\'');
fragment STRING_FRAGMENT : ('"' (ESC | ~["\\])* '"') | ('""');
fragment ESC : '\\' (["'\\/bfnrt] | UNICODE);
fragment UNICODE : 'u' HEX HEX HEX HEX;
fragment HEX : [0-9a-fA-F];

// Tokens

NUMBER     : INT | FLOAT;
STRING     : STRING_FRAGMENT;
VARIABLE   : VAR_FRAGMENT;

WS         : [ \t\r\n]+ -> skip;


COMMENT 
    : ('/*' .*? '*/' | '//' .*? '\n') -> skip 
    ; 
