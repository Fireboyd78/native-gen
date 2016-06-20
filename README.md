# Native Generator
Tool for generating IDA scripts to aid in reverse-engineering of GTA 5.

## Requirements
* Visual Studio 2015 or higher
* .NET Framework 4.5

## Usage
This tool requires a dump file that is usually generated by the reverse engineer. It is not meant to read from an EXE or any game files.
See below for how your dump file should be structured.

NOTE: I haven't implemented native hash translation yet, so it's likely you won't be able to generate for now. If you would like to help, please submit a pull request.

## File Format
#### ASCII
Not yet implemented.
#### Binary
Binary dumps are simple files that must be in little-endian. For most purposes, version 1 dumps should suffice.
```
struct NativeDumpFile
{
    int32 magic = 0x5654414E; // 'NATV'
    int32 version;            // version of dump
    int32 native_count;       // number of dumped natives (MUST NOT include failed ones during native dump!)
    /*
        Depending on which version dump you are using, the natives list
        may or may not follow directly after the native count.
        
        For version 1 dumps, the list is directly after the count.
    */
    struct NativeEntry
    {
        int64 hash; // native hash
        int64 func_offset; // function offset in the EXE
    } natives[native_count]; // Native list size will be (native_count * sizeof(NativeTableEntry))
    /!*
        This space should NOT be used to store extra data.
    *!/
}
```

# Contributing
Want to contribute to the project? Submit a pull request!

Please keep the following guidelines in mind:
* Use size 4 spaces. No tabs please.
* Try to keep code style consistent.
* Don't use anyone else's code without attributing them.

# Special Thanks
* [JohnnyCrazy](https://github.com/JohnnyCrazy) -- For writing most of the original code in his [NativeGenerator](https://github.com/JohnnyCrazy/scripthookvdotnet/tree/native-generator/helpers/NativeGenerator).
