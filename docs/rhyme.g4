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
    ;


bindingDeclaration
    : type declarator (',' declarator)* ';'
    ;

type
    : IDENTIFIER
    | type OPEN_PAREN (paramDecl (',' paramDecl)*)? CLOSE_PAREN 
    ;

paramDecl
    : type IDENTIFIER
    ;

declarator
    : IDENTIFIER ('=' expression)
    ;

expression
    : IDENTIFIER
    ;

// Lexical Rules
IDENTIFIER: [_a-zA-Z][_a-zA-Z0-9]*;

EXPORT: 'export';
GLOBAL: 'global';
IMPORT: 'import';
MODULE: 'module';

OPEN_PAREN: '(';
CLOSE_PAREN: ')';
SEMI: ';';
