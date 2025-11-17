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
grammar MongoAggregationForHumans;
import JsonGrammar;

file
    : 'FROM' STRING pipeline_def
    ;

pipeline_def
    : 'PIPELINE' '{'  stages_list '}'
    ;

 stages_list
    : stage_def
    | stages_list stage_def
    ;

stage_def
    : match_def      ('OPTIONS' json)?
    | bucket_def     ('OPTIONS' json)?
    | facet_def      ('OPTIONS' json)?
    | addfields_def  ('OPTIONS' json)?
    | project_def    ('OPTIONS' json)?
    | group_by_def   ('OPTIONS' json)?
    | sort_def       ('OPTIONS' json)?
    | join_def       ('OPTIONS' json)?
    | unwind_def     ('OPTIONS' json)?
    | replace_def    ('OPTIONS' json)?
    | do_def         ('OPTIONS' json)?
    ;

match_def: 'WHERE' expression
    ;

bucket_def
    : 'BUCKET' expression 'BOUNDARIES' NUMBER (',' NUMBER)* ('DEFAULT' defaultBucket=(NUMBER | STRING))? ('LET' let_list)? # BucketPlain
    | 'BUCKET' 'AUTO' expression 'BUCKETS' NUMBER ('GRANULARITY'  STRING)? ('LET' let_list)?                               # BucketAuto
    ;

facet_def
    : 'FACET' VARIABLE pipeline_def (',' VARIABLE pipeline_def)*
    ;

addfields_def
    : 'ADD' let_list
    ;

project_def
    : 'PROJECT' ('ID' '{' id_list=let_list '}')? data_list=let_list     # ProjectInclude
    | 'PROJECT' 'EXCLUDE' var_list                                      # ProjectExclude
    ;

replace_def
    : 'REPLACE' 'ID' '{' id_list=let_list '}' data_list=let_list
    ;


group_by_def: 'GROUP' 'BY' id_list=let_list ('LET' data_list=let_list)?
    ;

sort_def: 'SORT' 'BY' sort_var_list
    ;

join_def: 'JOIN' STRING 'AS' (VARIABLE | STRING) 'ON' equivalence_list ('LET' let_list )? (pipeline_def)?
    ;

unwind_def: 'UNWIND' VARIABLE ('INDEX' VARIABLE)?
    ;

do_def: 'DO' json
    ;

equivalence_list
    : left=VARIABLE '==' right=VARIABLE       #VarEquivalence
    | equivalence_list ',' equivalence_list   #EquivalenceList
    ;

sort_var_list
    : VARIABLE (ASC | DESC)?
    | sort_var_list ',' sort_var_list
    ;

var_list
    : VARIABLE
    | var_list ',' var_list
    ;

let_list
    : let_list_item
    | let_list ',' let_list
    ;

let_list_item
    : expression ('AS' (VARIABLE | STRING))?        # LetExpressionAs
    | '[' let_list ']' ('AS' (VARIABLE | STRING))?  # LetArray
    | '{' let_list '}' ('AS' (VARIABLE | STRING))?  # LetObject
    ;

expression
    : comparizon_expression ( (AND | OR) comparizon_expression )*
    ;

comparizon_expression
    : additive_expression ( (EQ | NEQ | LT | LTE | GT | GTE) additive_expression )*
    ;

additive_expression
    : multiplicative_expression ( (PLUS | MINUS) multiplicative_expression )*
    ;

multiplicative_expression
    : unary_expression ( (MUL | DIV) unary_expression )*
    ;

unary_expression
    : (PLUS | MINUS | NOT) unary_expression                   # UnaryExpression
    | brackets_expression                                     # PrimaryExpression
    ;

brackets_expression
    : atom                                                    # AtomExpression
    | VARIABLE NOT? 'IN' '(' expression (',' expression)* ')' # InExpression
    | VARIABLE '(' (unnamed_args_list | named_args_list ) ')' # FuncExpression
    | VARIABLE 'IS' json                                      # ProjectionExpression
    | VARIABLE NOT? 'EXISTS'                                  # ExistsExpression
    | '(' expression ')'                                      # BracketsExpression
    ;

named_args_list
    : left=VARIABLE ':' expression
    |  left=VARIABLE ':' expression_array
    | named_args_list ',' named_args_list
    ;

unnamed_args_list
    : expression 
    | expression_array
    | unnamed_args_list ',' unnamed_args_list
    ;

expression_array
    : '[' expression_array_item (',' expression_array_item)* ']'
    | '[' ']'
    ;

expression_array_item
    : expression
    | expression_array
    ;


atom
    : STRING
    | NUMBER
    | 'true'
    | 'false'
    | 'null'
    | VARIABLE
    ;

AND: 'AND' | '&&';
OR: 'OR' | '||';
NOT: 'NOT' | '!';
EQ: '==';
NEQ: '<>' | '!=';
GT: '>';
GTE: '>=';
LT: '<';
LTE: '<=';
ASC: 'ASC';
DESC: 'DESC';
MUL: '*';
DIV: '/';
PLUS: '+';
MINUS: '-';
