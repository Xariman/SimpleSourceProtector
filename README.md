
# SimpleSourceProtector


## About

The program is designed for simple protection of source codes in C#(not assemblies). A well-compiled project is required for correct operation. In the first stage, 
all projects that are in dependence are merged. The next step is renaming the classes, methods, variables, etc. At the final stage, the assembly is assembled into 
one large file and compression is performed by removing the spaces.

The project uses Roslyn libraries.

## Usage
```
Usage: SimpleSourceProtector [Options] FullFilePath\Project.cproj" [output]
  * if the output parameter is not specified, a "output" folder will be created inside the program directory
Options:
    -h      show this page
    -n      do not rename class, methods, vars and etc.
    -c      do a check compilation before renaming
    -m      make multiple files, not a single
```

© 2018  by Xariman
