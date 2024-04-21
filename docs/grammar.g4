/*
    Rhyme ANTLR4 grammar
*/
grammar Rhyme;


// Syntactical Rules
compilationUnit
    : moduleDecl topLevelDeclaration*
    ;

moduleDecl
    : 'module' IDENTIFIER ';'
    ;


topLevelDeclaration
    : variableDeclaration
    | functionDeclaration
    ;


variableDeclaration
    : type declarator (',' declarator)* ';'
    ;

declarator
    : IDENTIFIER ('=' expression)
    ;


// Lexical Rules
IDENTIFIER: [_a-zA-Z][_a-zA-Z0-9]*;
