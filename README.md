# The Rhyme Programming Language
**Rhyme** is a multi-paradigm general-purpose programming language that emphasis simplicity, interoperability, and control.
*[Specification Reference](https://github.com/rhyme-language/rhyme/blob/master/docs/specs.md)*

## Main Design Outlines
- Clear and simple syntax and semantics.
- Interoperability with C.
- High-level code features and constructs like Object-Oriented Programming.
- Asynchronous programming.
- Compile-time programming.
- Balancing between system and application programming.

## Building
The compiler is written in C# on .NET 8 which can be run on any supported platform. It uses Clang/LLVM 17.x.
### Windows
- Install [LLVM (Clang comes shipped with it)](https://github.com/llvm/llvm-project/releases/tag/llvmorg-17.0.1) and set up the `bin` folder in PATH variable.
- Install Visual Studio (Desktop C++ Development) specifically MSVC++ x86/x64 build tools, as clang is configured x86_64-pc-windows-msvc by default.
- Run...
### Linux
- Install .NET 8 SDK, LLVM, and Clang.
```
sudo apt install -y dotnet-sdk-8.0 llvm clang
```
- Build and run the application in `\src` directory.
```
dotnet run
```
