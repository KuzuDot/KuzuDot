namespace KuzuDot.Enums
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32", Justification = "Matching native KuzuDB type")]
    public enum KuzuDataTypeId : uint
    {
        KuzuAny = 0,
        KuzuNode = 10,
        KuzuRel = 11,
        KuzuRecursiveRel = 12,
        KuzuSerial = 13,
        KuzuBool = 22,
        KuzuInt64 = 23,
        KuzuInt32 = 24,
        KuzuInt16 = 25,
        KuzuInt8 = 26,
        KuzuUInt64 = 27,
        KuzuUInt32 = 28,
        KuzuUInt16 = 29,
        KuzuUInt8 = 30,
        KuzuInt128 = 31,
        KuzuDouble = 32,
        KuzuFloat = 33,
        KuzuDate = 34,
        KuzuTimestamp = 35,
        KuzuTimestampSec = 36,
        KuzuTimestampMs = 37,
        KuzuTimestampNs = 38,
        KuzuTimestampTz = 39,
        KuzuInterval = 40,
        KuzuDecimal = 41,
        KuzuInternalId = 42,
        KuzuString = 50,
        KuzuBlob = 51,
        KuzuList = 52,
        KuzuArray = 53,
        KuzuStruct = 54,
        KuzuMap = 55,
        KuzuUnion = 56,
        KuzuPointer = 58,
        KuzuUUID = 59
    }
}