namespace CodexTestProject;

public enum EnumConstant
{
    Value0,
    WriteThrough = unchecked((int)0x80000000),
    Value1 = 1,
    Value2,

}

public enum EnumConstantUInt64 : ulong
{
    Max = ulong.MaxValue
}