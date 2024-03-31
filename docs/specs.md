# The Rhyme Programming Language Specification
*0.1.0-alpha* 


## Introduction
This is the reference specifications for The Rhyme Programming Language. It serves reference for: 
- The language design and constructs.
- The language models.
- Implemented or planned features.

## Notation
The syntax for the *syntactical* and *lexical* grammar is based on the [ANTLR4 Grammar](https://github.com/antlr/antlr4/blob/master/doc/grammars.md):

```ebnf
rule: production | alternative1 | alternative2...;
```

## Lexical Grammar
### Comments
- Single-line comments: (`//`)

### Keywords
- `else`
- `extern`
- `false`
- `for`
- `global`
- `if`
- `import`
- `module`
- `null`
- `return`
- `str`
- `true`
- `using`
- `var`
- `void`
- `while`


### Literals
Literals are immediate and concrete values used in expressions.

#### Number Literals
- Integral Literals
