namespace Codex.ObjectModel
{
    [GeneratorExclude]
    public interface IVisitor :
        IValueVisitor<IQueryFactory, ReadOnlyMemory<byte>>,
        IValueVisitor<IQueryFactory, TextSourceBase>,
        IValueVisitor<IQueryFactory, MurmurHash>,
        IValueVisitor<IQueryFactory, ShortHash>,
        IValueVisitor<IQueryFactory, string>,
        IValueVisitor<IQueryFactory, bool>,
        IValueVisitor<IQueryFactory, long>,
        //IValueVisitoINullQueryFactory, r<byte[]>,
        IValueVisitor<IQueryFactory, SymbolId>,
        IValueVisitor<IQueryFactory, DateTime>,
        IValueVisitor<IQueryFactory, int>,
        IValueVisitor<IQueryFactory, ReferenceKind>,
        IValueVisitor<IQueryFactory, ReferenceKindSet>,
        IValueVisitor<IQueryFactory, StringEnum<PropertyKey>>,
        IValueVisitor<IQueryFactory, StringEnum<SymbolKinds>>
    {
    }

    [GeneratorExclude]
    public interface IValueVisitor<TValue> : IValueVisitor
    { 
        void Visit(IMappingField mapping, TValue value);
    }

    [GeneratorExclude]
    public interface IValueVisitor
    {
        public bool HandlesNoneBehavior { get; }
    }

    [GeneratorExclude]
    public interface IQueryFactory<TQuery> : 
        IQueryFactory<IVisitor, TQuery, ReadOnlyMemory<byte>>,
        IQueryFactory<IVisitor, TQuery, TextSourceBase>,
        IQueryFactory<IVisitor, TQuery, MurmurHash>,
        IQueryFactory<IVisitor, TQuery, ShortHash>,
        IQueryFactory<IVisitor, TQuery, string>,
        IQueryFactory<IVisitor, TQuery, bool>,
        IQueryFactory<IVisitor, TQuery, long>,
        IQueryFactory<IVisitor, TQuery, SymbolId>,
        IQueryFactory<IVisitor, TQuery, DateTime>,
        IQueryFactory<IVisitor, TQuery, int>,
        IQueryFactory<IVisitor, TQuery, ReferenceKind>,
        IQueryFactory<IVisitor, TQuery, ReferenceKindSet>,
        IQueryFactory<IVisitor, TQuery, StringEnum<SymbolKinds>>,
        IQueryFactory<IVisitor, TQuery, StringEnum<PropertyKey>>
    {
    }

    [GeneratorExclude]
    public interface IQueryFactory<TQuery, TValue>
    {
        TQuery TermQuery(IMappingField mapping, TValue term);
    }

    // The types below enable compile time checking that handling is done both for
    // IVisitor and IQueryFactory

    public class QueryPlaceHolder { }

    [GeneratorExclude]
    public interface IValueVisitor<TQueryFactory, TValue> : IValueVisitor<TValue>
        where TQueryFactory : IQueryFactory<QueryPlaceHolder, TValue>
    {
    }

    [GeneratorExclude]
    public interface IQueryFactory : IQueryFactory<QueryPlaceHolder>
    {

    }

    [GeneratorExclude]
    public interface IQueryFactory<TVisitor, TQuery, TValue> : IQueryFactory<TQuery, TValue>
        where TVisitor : IValueVisitor<TValue>
    {

    }
}