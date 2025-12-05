# bigzip (because big is better)

Like the opposite of winrar
Makes files larger rather than compress them

It's completely useless

## How to Build

### Prerequisites
- Go 1.x or later
- .NET 8 SDK (for the UI)

### Build Everything
Run the build script:
```
.\scripts\build.ps1
```
This will build both the command line tool (`dist\bz.exe`) and the UI (`dist\bigzip.exe`).

## How to Use

### Command-Line Tool
The command-line tool is located at `dist\bz.exe`.

### How to use
Run `dist\bigzip.exe`
Select or drag a file 
Modify settings as needed
click `Run BigZip`

#### How to use (CLI)
```
bz.exe -input <input_file> [-output <output_file>] [-factor <multiplier>] [-mode <mode>]
```
- `-input` or `-i`: Path to the input file (required)
- `-output` or `-o`: Path to the output file (default: `<input>.bigzip`)
- `-factor` or `-f`: Size multiplier (must be >= 1.0, default: 1.0, but will be adjusted to minimum required)
- `-mode`: Inflation mode: `repeat`, `zero`, or `random` (default: `repeat`)
- `-force`: Overwrite existing output file

Example:
```
bz.exe -i example.png -f 2.5 -mode random
```

#### Unbigzip a file (restore original)
```
bz.exe -unbigzip -input <bigzip_file> [-output <output_file>]
```
- `-unbigzip` or `-uz`: Restore mode
- `-input` or `-i`: Path to the .bigzip file (if not entered will use next arg as path)
- `-output` or `-o`: Path to the restored file (default: remove .bigzip extension or add .orig)
- `-force`: Overwrite existing output file

Example:
```
bz.exe -uz -i example.png.bigzip
```

the example .png will go from `3.52 kb` to a whopping `225 kb`
the size increase is exponential - if we run bigzip on `example.png.bigzip` it increases from `225 kb` to `14.0 MB`
