using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.ContractsLight;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Codex.ObjectModel;
using Codex.Sdk.Utilities;
using Codex.Utilities.Serialization;

namespace Codex
{
    public abstract class SearchType : ITypeBox, IDerivedTypeBox<ISearchEntity>
    {
        public string Name { get; protected set; }
        public string IndexName { get; protected set; }
        public int Id { get; protected set; }
        public SearchTypeId TypeId { get; protected set; }

        private List<SearchField> _sortedFields;

        public IReadOnlyList<SearchField> SortedFields => _sortedFields ??= Fields.Values
            .OrderBy(s => s.Name)
            .Select((s, index) =>
            {
                s.Index = index;
                return s;
            }).ToList();

        public IReadOnlyList<string> Includes { get; protected set; } = Array.Empty<string>();

        private Dictionary<string, SearchField> WritableFields { get; } = new();

        public IReadOnlyDictionary<string, SearchField> Fields => WritableFields;

        public SearchField this[string fieldName] => Fields[fieldName];

        public static SearchType<T> Create<T>(List<SearchType> registeredSearchTypes, SearchTypeId? explicitTypeId = null, [CallerMemberName]string name = null)
            where T : class, ISearchEntity
        {
            var searchType = new SearchType<T>(name);
            searchType.Id = registeredSearchTypes.Count;
            var typeId = explicitTypeId ?? Enum.Parse<SearchTypeId>(name);
            if (typeId.ToString() != searchType.Name)
            {
                throw new Exception($"Search type has Id={searchType.Id}, but which corresponds to TypeId={typeId}");
            }

            searchType.TypeId = typeId;
            registeredSearchTypes.Add(searchType);
            return searchType;
        }

        public abstract Type Type { get; }

        protected void AddFieldCore(SearchField field)
        {
            Contract.Assert(_sortedFields == null);
            WritableFields.Add(field.Name, field);
        }

        public static implicit operator SearchTypeId(SearchType type) => type.TypeId;
    }

    public record SearchField(string Name, SearchBehavior Behavior, Type FieldType)
    {
        public int Index { get; internal set; }

        public SearchBehaviorInfo BehaviorInfo { get; set; } = new();

        public Type QueryFieldType { get; set; } = FieldType;

        public SearchField ExcludeFromHash()
        {
            BehaviorInfo = BehaviorInfo with { IsHashExcluded = true };
            return this;
        }
    }

    public abstract record SearchFieldBase<TSearchType, TFieldType>(string Name, SearchBehavior Behavior)
        : SearchField(Name, Behavior, typeof(TFieldType)), 
            IMappingField<TSearchType, TFieldType>,
            ISortField<TSearchType, TFieldType>
    {
        public Func<TSearchType, bool> ShouldExclude;

        public void Visit(TSearchType entity, IVisitor visitor)
        {
            if (ShouldExclude != null && ShouldExclude(entity)) return;

            var valueVisitor = (IValueVisitor<TFieldType>)visitor;
            if (Behavior == SearchBehavior.None && !valueVisitor.HandlesNoneBehavior)
            {
                return;
            }

            Visit(entity, valueVisitor);
        }

        public abstract void Visit(TSearchType entity, IValueVisitor<TFieldType> visitor);
    }

    public record StoredFilterField<TSearchType>(string Name, SearchBehavior Behavior)
        : SearchFieldBase<TSearchType, string>(Name, Behavior)
    {
        public override void Visit(TSearchType entity, IValueVisitor<string> visitor)
        {
            // No values are stored of entity for stored filter fields
        }
    }
    public record SearchField<TSearchType, TFieldType>(string Name, SearchBehavior Behavior, TrySelect<TSearchType, TFieldType> TryGet)
        : SearchFieldBase<TSearchType, TFieldType>(Name, Behavior)
    {
        public override void Visit(TSearchType entity, IValueVisitor<TFieldType> visitor)
        {
            if (TryGet(entity, out var value))
            {
                visitor.Visit(this, value);
            }
        }
    }

    public record SearchMultiField<TSearchType, TFieldType>(string Name, SearchBehavior Behavior, Func<TSearchType, IEnumerable<TFieldType>> Getter)
        : SearchFieldBase<TSearchType, TFieldType>(Name, Behavior)
    {
        public override void Visit(TSearchType entity, IValueVisitor<TFieldType> visitor)
        {
            var items = Getter(entity);
            if (items == null) return;

            foreach (var item in items)
            {
                visitor.Visit(this, item);
            }
        }
    }

    public delegate bool TrySelect<TSearchType, TFieldType>(TSearchType entity, out TFieldType value);

    public interface ISearchType<in TSearchType>
        where TSearchType : class, ISearchEntity
    {
        void VisitFields(TSearchType entity, IVisitor visitor);
    }

    public class SearchType<TSearchType> : SearchType, ISearchType<TSearchType>, ITypeBox<TSearchType>, IDerivedTypeBox<TSearchType, ISearchEntity>
        where TSearchType : class, ISearchEntity
    {
        public static SearchType<TSearchType> Instance { get; private set; }

        public override Type Type => typeof(TSearchType);

        public Func<TSearchType, object> GetRoutingKey { get; private set; }

        public Func<TSearchType, bool> FieldExcludeFilter { get; private set; }

        public Type ExternalEntityType { get; private set; }
        public Func<TSearchType, bool> HasExternalLink { get; private set; } = s => false;

        public Func<TSearchType, string[]> GetObjectPath { get; private set; } = e => null;

        //public IMappingField<TSearchType, MurmurHash> EntityContentIdField { get; }
        public IMappingField<TSearchType, int> StableIdField { get; }

        public IMappingField<TSearchType, string> StoredFilterTagField { get; }

        public List<Tuple<Expression<Func<TSearchType, object>>, Expression<Func<TSearchType, object>>>> CopyToFields = new List<Tuple<Expression<Func<TSearchType, object>>, Expression<Func<TSearchType, object>>>>();

        public SearchType(string name)
        {
            Contract.Assert(Instance == null);
            Instance = this;

            Name = name;
            IndexName = Name.ToLowerInvariant();
            var storedFilterField = new StoredFilterField<TSearchType>("StoredFilterTag", SearchBehavior.NormalizedKeyword);

            AddField(storedFilterField);
            //SearchField(s => s.EntityContentId, SearchBehavior.Term);
            //SearchField(s => s.EntityContentSize, SearchBehavior.Term);
            SearchField(s => s.StableId, SearchBehavior.SortValue, configure: s => s.BehaviorInfo = s.BehaviorInfo with
            {
                IsHashExcluded = true,
                IsStableId = true
            });
            //SearchField(s => s.SnapshotId, SearchBehavior.Sortword);

            //EntityContentIdField = GetMappingField<MurmurHash>(nameof(ISearchEntity.EntityContentId));
            StableIdField = GetMappingField<int>(nameof(ISearchEntity.StableId));
            StoredFilterTagField = storedFilterField;
        }

        public bool TryGetValue<T>(SearchType<T> other, T entity, out TSearchType value)
            where T : class, ISearchEntity
        {
            if ((SearchType)other == this)
            {
                value = (TSearchType)(object)entity;
                return true;
            }

            value = default;
            return false;
        }

        public void VisitFields(TSearchType entity, IVisitor visitor)
        {
            foreach (IMappingField<TSearchType> field in SortedFields)
            {
                field.Visit(entity, visitor);
            }
        }

        public SearchType<TSearchType> WithObjectPath(Func<TSearchType, string[]> getObjectPath)
        {
            GetObjectPath = getObjectPath;
            return this;
        }

        public SearchType<TSearchType> Route(Func<TSearchType, string> getRoutingKey)
        {
            GetRoutingKey = getRoutingKey;
            return this;
        }

        public SearchType<TSearchType> SetShouldExclude(Func<TSearchType, bool> filter = null)
        {
            FieldExcludeFilter = filter;
            return this;
        }

        public SearchType<TSearchType> CopyTo(
            Expression<Func<TSearchType, object>> sourceField,
            Expression<Func<TSearchType, object>> targetField)
        {
            return this;
        }

        public SearchType<TSearchType> Exclude(
            Expression<Func<TSearchType, object>> searchField)
        {
            return this;
        }

        public SearchType<TSearchType> SearchField<T>(
            Expression<Func<TSearchType, T>> searchField,
            SearchBehavior behavior,
            string name = null,
            Action<SearchField> configure = null,
            Func<TSearchType, bool> isValid = null)
        {
            name ??= GetName(searchField);

            return SearchNamedField(searchField.Compile(), behavior, name, configure, isValid);
        }

        public SearchType<TSearchType> SearchNamedField<T>(
            Func<TSearchType, T> searchField,
            SearchBehavior behavior,
            string name,
            Action<SearchField> configure = null,
            Func<TSearchType, bool> isValid = null)
        {
            if (TypeSystemHelpers.Is<Func<TSearchType, T>, Func<TSearchType, SymbolId>>(searchField, searchSymbolField =>
            {

                Func<TSearchType, string> searchField = s => searchSymbolField(s).Value;
                SearchNamedFieldCore(searchField, behavior, name, configure: f =>
                {
                    f.BehaviorInfo = f.BehaviorInfo with { PreferBinary = true, IsSymbolId = true };
                    configure?.Invoke(f);
                }, isValid);
            }))
            {
                return this;
            }
            return SearchNamedFieldCore(searchField, behavior, name, configure, isValid);
        }

        private SearchType<TSearchType> SearchNamedFieldCore<T>(
            Func<TSearchType, T> searchField,
            SearchBehavior behavior,
            string name,
            Action<SearchField> configure = null,
            Func<TSearchType, bool> isValid = null)
        {
            bool tryGetValue(TSearchType entity, out T value)
            {
                if (isValid?.Invoke(entity) != false)
                {
                    value = searchField(entity);
                    return true;
                }

                value = default;
                return false;
            }

            var field = new SearchField<TSearchType, T>(name, behavior, tryGetValue);
            configure?.Invoke(field);
            AddField(field);
            return this;
        }

        private void AddField<T>(SearchFieldBase<TSearchType, T> field)
        {
            field.ShouldExclude = FieldExcludeFilter;
            AddFieldCore(field);
        }

        //public SearchType<TSearchType> ExternalLink<TLink>(Func<TSearchType, TLink> getExternalLink, params string[] modes)
        //{
        //    ExternalEntityType = typeof(IExternalSearchEntity<TLink>);
        //    Contract.Check(typeof(TSearchType).IsAssignableTo(ExternalEntityType))
        //        ?.Assert($"{typeof(TSearchType)} does not implement {typeof(IExternalSearchEntity<TLink>)}");
        //    HasExternalLink = s => getExternalLink(s) != null;
        //    Includes = modes;
        //    return this;
        //}

        //public SearchType<TSearchType> SearchAs<TIntermediate, TFinal>(
        //    Expression<Func<TSearchType, TIntermediate>> searchField,
        //    Func<TIntermediate, TFinal> convert,
        //    SearchBehavior behavior,
        //    string name = null)
        //{
        //    name ??= GetName(searchField);

        //    var compiledGetter = searchField.Compile();
        //    Fields.Add(name, new SearchField<TSearchType, TFinal>(name, behavior, v => convert(compiledGetter(v))));
        //    return this;
        //}

        public SearchType<TSearchType> SearchMultiField<T>(
            Expression<Func<TSearchType, IEnumerable<T>>> searchField,
            SearchBehavior behavior,
            string name = null,
            Action<SearchField> configure = null)
        {
            name ??= GetName(searchField);

            var field = new SearchMultiField<TSearchType, T>(name, behavior, searchField.Compile());
            configure?.Invoke(field);
            AddField(field);
            return this;
        }

        public SearchFieldBase<TSearchType, TFieldType> GetMappingField<TFieldType>([CallerMemberName]string name = null)
        {
            return (SearchFieldBase<TSearchType, TFieldType>)this[name];
        }

        public Include<TSearchType> GetInclude(int index)
        {
            return new Include<TSearchType>(1u << index, this);
        }

        public Include<TSearchType> IncludeAll()
        {
            return new Include<TSearchType>((uint)((1 << Includes.Count) - 1), this);
        }

        private string GetName<T>(Expression<Func<TSearchType, T>> expression)
        {
            var memberExpression = (MemberExpression)expression.Body;
            return memberExpression.Member.Name;
        }
    }
}

