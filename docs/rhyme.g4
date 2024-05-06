/*
    Rhyme ANTLR4 grammar
*/
grammar rhyme;


// Syntactical Rules
compilationUnit
    : moduleDecl importStmt* topLevelDeclaration+
    ;

moduleDecl
    : MODULE IDENTIFIER SEMI
    ;

importStmt
    : IMPORT IDENTIFIER SEMI
    ;

topLevelDeclaration
    : bindingDeclaration
    | functionDeclaration
    ;


bindingDeclaration
    : type declarator (',' declarator)* ';'
    ;

functionDeclaration
    : type IDENTIFIER OPEN_PAREN ()? CLOSE_PAREN
    ;

type
    : IDENTIFIER
    | FN func_type 
    ;

func_type
    : type OPEN_PAREN (paramDecl (',' paramDecl)*)? CLOSE_PAREN
    ;

parameters
    : paramDecl (',' paramDecl)*
    ;

paramDecl
    : type IDENTIFIER
    ;

declarator
    : IDENTIFIER ('=' expression)?
    ;

expression
    : IDENTIFIER
    ;

// Lexical Rules
IDENTIFIER: [_a-zA-Z][_a-zA-Z0-9]*;

EXPORT: 'export';
FN:     'fn';
GLOBAL: 'global';
IMPORT: 'import';
MODULE: 'module';

OPEN_PAREN: '(';
CLOSE_PAREN: ')';
SEMI: ';';
