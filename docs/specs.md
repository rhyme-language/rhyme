# The Rhyme Programming Language Reference
*0.1.0-alpha* 


## Introduction
This is the reference specifications for The Rhyme Programming Language. It serves as a reference for: 
- language design and constructs.
- defined compiler behavior

## Notation
The syntax for the *syntactical* and *lexical* grammar is based on the [ANTLR4 Grammar](https://github.com/antlr/antlr4/blob/master/doc/grammars.md):

```ebnf
rule: production | alternative1 | alternative2...;
```
Lexical rules are in UPPERCASE and syntactical rules are in lowercase.

## Lexical Grammar
### Comments
These types of commments are ignored by the compiler:
```
// This is single-line comment

/*
    This is a
    multi-line comment
*/
```

### Keywords
The following is the reserved keywords which can't be as an identifier.
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
Literals are immediate and concrete values used in expressions. they are treated as constants and qualified for compile-time operations.

#### Number Literals
Number literals represents numerical values. It can be seperated using (underscore `_`) for readibility.  

##### Integral Literals
```antlr
DEC_INT: DEC_DIG+;
BIN_INT: '0' ('b' | 'b') BIN_DIG ('_' | BIN_DIG)*;
OCT_INT: '0' ('o' | 'O') OCT_DIG ('_' | OCT_DIG)*;
HEX_INT: '0' ('x' | 'X') HEX_DIG ('_' | HEX_DIG)*;

DEC_DIG: [0-9];
BIN_DIG: [01]; 
OCT_DIG: [0-7];
HEX_DIG: [0-9a-fA-F];
```
Integral literals are constants.  

## Bindings
```antlr4
binding_decl: data_type identifier; 
```
A binding is a named reference to a value.

## Types
Types dictates the structure and operations of values.

### Numeric Types
Numeric types are integral and floating-point types. It handle the arithmetic operations and calculations.

Type | Description
-----|----------------------------
i8   | signed 8-bit integer
i16  | signed 16-bit integer
i32  | signed 32-bit integer
i64  | signed 64-bit integer
u8   | unsigned 8-bit integer
u16  | unsigned 16-bit integer
u32  | unsigned 32-bit integer
u64  | unsigned 64-bit integer

### String Type (`str`)

### Namespace Type (`nsp`)
Namespace type represents a collection of a bindings under a name.  
As example, The directive `%cinclude` loads the declarations in the header into a namespace binding.
## Functions

## Modules
Each file must belong to a module this is achieved by declaring that in the top of the file:
```antlr4
module_decl: 'module' identifier ';';
```