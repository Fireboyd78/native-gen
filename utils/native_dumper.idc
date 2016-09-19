/**
    Native Table Dumper Script [r195]
    
    Copyright (C)2016 Mark Ludwig (mk.ludwig1@gmail.com)
    https://github.com/Fireboyd78/native-gen
    
    Permission is hereby granted to anyone who wishes to use this code for ALL intents and purposes.
    Credits to the original author is not necessary, but very much appreciated.
    ====================================================
    Description:
    
    Dumps a binary file of every registered native hash and function.
    For use with third-party tools that will generate a list of native functions and their locations inside the EXE.
    
    The resulting dump file will be located in the same directory as the IDB file.
    ====================================================
    File structure:
    
    struct NativeDumpFile
    {
        int32 magic = 0x5654414E; // 'NATV'
        int32 version;            // version of dump
        int32 native_count;       // number of dumped natives (does not include failed ones during native dump!)
        /!*
            This space may include more information in future versions.
            Do not assume the native list will follow directly after the native_count!
        *!/
        struct NativeEntry
        {
            int64 hash; // native hash
            int64 func_offset; // function offset in the EXE
        } natives[native_count]; // Native list size will be (native_count * sizeof(NativeTableEntry) /!* 0x10 *!/)
        /!*
            This space should not be used to store extra data.
        *!/
    }
**/

#include <ida.idc>

// Address of the RegisterNative function
#define REGISTER_NATIVE_ADDR 0x14072FBB0

#define isReg(ea,o) (GetOpType(ea,o) == 1)
#define isRegN(ea,o,n) (isReg(ea,o) && GetOperandValue(ea,o) == n)

#define isRegRCX(ea,o) isRegN(ea,o,1)
#define isRegRDX(ea,o) isRegN(ea,o,2)

#define isMemRef(ea,o) (GetOpType(ea,o) == 2)
#define isImmediate(ea,o) (GetOpType(ea,o) == 5)

static strstr_last(str1, str2)
{
    auto str = str1;
    auto idx = 0;
    auto result = -1;
    while (idx != -1) {
        idx = strstr(str, str2);
        if (idx != -1)
            result = result + (idx + 1);
        str = substr(str, (idx + 1), -1);
    }
    return result;
}

static GetIdbDirectory()
{
    auto idb_path = GetIdbPath();
    return substr(idb_path, 0, strstr_last(idb_path, "\\") + 1);
}

static GetPrevMnemEA(ea, maxEA, maxLoops, name)
{
    auto tries = 0;
    
    if (maxEA == -1)
        maxEA = GetFunctionAttr(ea, FUNCATTR_START);
    
    while ((ea = PrevHead(ea, maxEA)) >= maxEA)
    {
        if (GetMnem(ea) == name)
            return ea;
            
        if ((maxLoops != -1) && (++tries == maxLoops))
            break;
    }
}

static GetNextMnemEA(ea, maxEA, maxLoops, name)
{
    auto tries = 0;
    
    if (maxEA == -1)
        maxEA = GetFunctionAttr(ea, FUNCATTR_END);
    
    while ((ea = NextHead(ea, maxEA)) <= maxEA)
    {
        if (GetMnem(ea) == name)
            return ea;
            
        if ((maxLoops != -1) && (++tries == maxLoops))
            break;
    }
    // not found / out of bounds
    return -1;
}

// Checks if the EA is a call/jmp to RegisterNative
static IsValidRegisterNativeCall(ea)
{
    auto mnem = GetMnem(ea);
    return (((mnem == "jmp") || (mnem == "call")) && Rfirst0(ea) == REGISTER_NATIVE_ADDR);
}

static GetNativeFuncHash(ea)
{
    // mov rcx <hash>
    if ((ea != -1) && (GetMnem(ea) == "mov") && isRegRCX(ea, 0) && isImmediate(ea, 1))
        return GetOperandValue(ea, 1);
        
    return -1;
}

// Gets the function address from the instruction
// Returns address of native function; -1 if invalid
static GetNativeFuncAddr(ea)
{
    // lea rdx <func_addr>
    if ((ea != -1) && (GetMnem(ea) == "lea") && isRegRDX(ea, 0) && isMemRef(ea, 1))
        return GetOperandValue(ea, 1);
    
    return -1;
}

static main()
{
    auto dump_file = sprintf("%snative_lookup.dat", GetIdbDirectory());
    auto handle = fopen(dump_file, "wb");
    
    writelong(handle, 0x5654414E, 0);
    writelong(handle, 1, 0);
    
    // don't know the count yet
    fseek(handle, 4, 1);
    
    auto ea;
    auto next = RfirstB0(REGISTER_NATIVE_ADDR);

    auto numRefs = 0;
    auto problemRefs = 0; // out of bounds
    auto invalidRefs = 0; // not considered valid
    
    while ((ea = next) != -1)
    {
        auto limit = GetFunctionAttr(ea, FUNCATTR_START);
        
        if (IsValidRegisterNativeCall(ea)) {
            auto ea_func = GetNativeFuncAddr(GetPrevMnemEA(ea, limit, 4, "lea"));
            auto ea_hash = GetNativeFuncHash(GetPrevMnemEA(ea, limit, 4, "mov"));
            
            if ((ea_func != -1) && (ea_hash != -1)) {
                // dump data to file              
                
                writelong(handle, ea_hash, 0);
                writelong(handle, (ea_hash >> 32), 0);
                
                writelong(handle, ea_func, 0);
                writelong(handle, (ea_func >> 32), 0);
                  
                if (isData(GetFlags(ea_func)))
                    MakeUnkn(ea_func, 0);
                if (isUnknown(GetFlags(ea_func)))
                    MakeCode(ea_func);
                if (isCode(GetFlags(ea_func))) {
                    if (GetFunctionFlags(ea_func) == -1)
                        MakeFunction(ea_func, -1);
                } else {
                    Message("WARNING: Couldn't determine type of data @ 0x%08X!\n", ea, ea_func);
                }
            } else {
                problemRefs++;
            }
        } else {
            invalidRefs++;
        }
    
        next = RnextB0(REGISTER_NATIVE_ADDR, ea);
        numRefs++;
    }
    
    auto numGoodRefs = (numRefs - (invalidRefs + problemRefs));
    
    // write number of exported entries
    fseek(handle, 8, 0);
    writelong(handle, numGoodRefs, 0);
    
    fclose(handle);
    
    Message("Finished parsing %d refs to RegisterNative.\n", numRefs);

    if (numGoodRefs == numRefs) {
        Message(" - Completed with no errors :)\n");
    } else {
        Message(" - Completed with errors:\n");
        if (problemRefs > 0)
            Message("  - Detected %d problem reference(s)\n", problemRefs);
        if (invalidRefs > 0)
            Message("  - Detected %d invalid reference(s)\n", invalidRefs);
    }
}