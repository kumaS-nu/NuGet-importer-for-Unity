# Cording Conventions

 Basically, this project follows Microsoft's [Framework Design Guidelines](https://docs.microsoft.com/en-US/dotnet/standard/design-guidelines/) or [C# Coding Conventions](https://docs.microsoft.com/en-US/dotnet/csharp/programming-guide/inside-a-program/coding-conventions). I also defines the code style to be used in this project in `NuGetImporterForUnity/.editorconfig`. If you clean up your code with this setting, you are basically fine as for the code style. (Please let me know if there's any part of the code style I'm not following.)

## Naming Guidelines

 The camelCasing convention, used only for parameter names, capitalizes the first character of each word except the first word. The PascalCasing convention, used for all identifiers except parameter names, capitalizes the first character of each word (including acronyms over two letters in length). Do choose easily readable identifier names, and DO NOT use abbreviations or contractions as part of identifier names.
  For more infomation, see [Naming Guidelines - Microsoft](https://docs.microsoft.com/en-US/dotnet/standard/design-guidelines/naming-guidelines).

## Layout Conventions

* Use the default Code Editor settings (smart indenting, four-character indents, tabs saved as spaces).
* Write only one statement per line.
* Write only one declaration per line.
* If continuation lines are not indented automatically, indent them one tab stop (four spaces).
* Add at least one blank line between method definitions and property definitions.

## Commenting Conventions

* Place the comment on a separate line, not at the end of a line of code.
* Begin comment text with an uppercase letter.
* End comment text with a period (except in the case of a single noun).

## Implicitly Typed Variables

* Use implicit typing for local variables when the type of the variable is obvious from the right side of the assignment, or when the precise type is not important.
* Do not use var when the type is not apparent from the right side of the assignment.
* Use implicit typing to determine the type of the loop variable in for loops.
* Do not use implicit typing to determine the type of the loop variable in foreach loops.

## using directive

 The using directives must be written outside of the namespace (basically at the top of the file). The order of using directives is as follows, and each group should be separated by one line.

1. System
1. other


 For example

``` csharp
using System;
using System.Collections.Generic;
using System.Linq;

using kumaS.NuGetImporter.Editor.DataClasses;

using UnityEditor;

using UnityEngine;
```