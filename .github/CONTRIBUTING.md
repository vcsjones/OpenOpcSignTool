Contributing
============

There is no too small a way to contribute to this project. If you have a question because something
is unclear or found unexpected behavior, please [open][1] an issue to see hope things can be improved.

# Pull Requests

Please fork the repository and create a pull request if you would like to submit a code change. If you
need help with your pull request, please open one anyway and put "WIP" as the first word of the pull
request so we can look at it together.

If you have an idea for substantial improvements or a large refactoring, please [open][1] an issue first
so there is an understanding of the work being done. That helps make sure the pull request doesn't
contain any wasted effort.

Ideally any non-trivial change will include additional unit tests. All unit tests must pass before a pull
request will be merged.

# Building

 This project targets `net462` as well as `netcoreapp2.1`.

## Windows + Visual Studio

Visual Studio 2017 or higher is required. You open the solution and build and run unit tests.

## dotnet CLI

From the command line, building can be done with:

```
dotnet build
```

If you are on a non-Windows platform, you may not be able to build the `net462` targets. In that case,
use the `--framework netcoreapp2.1` option to only build netcoreapp2.1.

# Running Tests

## Windows + Visual Studio

Visual Studio 2017 or higher is required. You open the solution and build and run unit tests from the
Test Explorer.

## dotnet CLI

From the command line, building can be done with:

```
dotnet test
```

If you are on a non-Windows platform, you may not be able to test the `net462` targets. In that case,
use the `--framework netcoreapp2.1` option to only test netcoreapp2.1.

[1]: https://github.com/vcsjones/OpenOpcSignTool/issues/new
