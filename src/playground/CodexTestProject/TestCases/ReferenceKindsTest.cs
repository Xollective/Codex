/*
 * 
 * Test
 * 
 * 
 */


using System;
using System.Numerics;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace CodexTestProject;

public record HaveIndexer(int RecordArgProperty, bool RecordArgParamAndProperty) : IHaveIndexer
{
    public static explicit operator HaveIndexer(int value)
    {
        throw new NotImplementedException();
    }

    public int field1, field2 = 0;

    public bool RecordArgParamAndProperty { get; set; } = RecordArgParamAndProperty;

    public int this[int input] { get => default; set { } }

    public bool NormalProperty { get => default; set { } }

    public bool NormalReadOnlyProperty => RecordArgProperty < 10;

    public bool NormalSetOnlyProperty { get => default; set { } }
}

public record NoArgRecord
{
    public void TestOperator(int a, int b, object oa, object od, TestOverride ta, TestOverride tb)
    {
        bool eq;
        eq = a == b;
        eq = a < b;
        eq = oa == od;
        eq = ta == tb;
        eq = (TestOverrideBase)ta == (TestOverrideBase)tb;
        ++ta;
    }
}

/// <summary>
/// 
/// </summary>
[DataContract]
public class TestOverride : TestOverrideBase, IEqualityOperators<TestOverride, TestOverride, bool>, IIncrementOperators<TestOverride>
{
    public override bool VirtualMethod() => false;

    public static TestOverride operator ++(TestOverride value)
    {
        throw new System.NotImplementedException();
    }

    public static bool operator ==(TestOverride? left, TestOverride? right)
    {
        throw new System.NotImplementedException();
    }

    public static bool operator !=(TestOverride? left, TestOverride? right)
    {
        throw new System.NotImplementedException();
    }

    //public static explicit operator bool ==(TestOverride ta, TestOverride tb)
    //{
    //}

}

public class TestOverrideBase
{
    public virtual bool VirtualMethod() => true;
}

public interface IBase
{
}

public partial interface IHaveIndexer : IBase
{
    [DataMember]
    bool NormalProperty { get; set; }

    bool NormalReadOnlyProperty { get; }
    bool NormalSetOnlyProperty { get; set; }


    int this[int input] { get; set; }
}

public record DerivedRecord(int RecordArgProperty) : HaveIndexer(RecordArgProperty, false)
{

}

public class IndexerReference
{
    /// <summary>
    /// <see cref="HaveIndexer.RecordArgParamAndProperty"/>
    /// </summary>
    /// <param name="indexer"></param>
    public void ReferenceIndexer(HaveIndexer indexer)
    {
        indexer.NormalProperty = true;
        indexer = new HaveIndexer(23, true)
        {
            NormalProperty = true,
            NormalSetOnlyProperty = false,
            [2] = 10
        };

        var s = indexer with { NormalSetOnlyProperty = true };

        var nar = new NoArgRecord();

        NoArgRecord nar2 = new();


        var np = indexer.NormalProperty;
        var result = indexer[0];
        indexer[0] = result;
    }

    public void ReferenceIndexer(IHaveIndexer indexer)
    {
        var result = indexer[1];
        indexer[1] = result;

        var intCast = (HaveIndexer)result;

        bool isHave = indexer is HaveIndexer;

        switch (indexer)
        {
            case HaveIndexer t:
                break;
        }

        if (indexer is HaveIndexer haveIndexer)
        {

        }

        var mustCast = (HaveIndexer)indexer;
        var tryCast = indexer as HaveIndexer;
    }
}

public class BaseNode
{
    public int Value1;

    public int Value2;
}

public class NodeA :BaseNode
{
    public NodeA(int value1, int value2)
    {
        this.Value1 = value1;
        this.Value2 = value2;
    }
}

public class NodeB : BaseNode
{
    public int Value3;

    public NodeB(int value1, int value2, int value3)
    {
        this.Value1 = value1;
        this.Value2 = value2;
        this.Value3 = value3;
    }
}

// Second declaration
public partial interface IHaveIndexer
{

}