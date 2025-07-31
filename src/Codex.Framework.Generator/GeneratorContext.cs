using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.ContractsLight;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Codex.ObjectModel;
using Codex.ObjectModel.Internal;
using Codex.ObjectModel.CompilerServices;
using Codex.Utilities;
using Codex.Utilities.Serialization;
using Meziantou.Framework.CodeDom;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Simplification;
using Dom = Meziantou.Framework.CodeDom;
using Codex.Sdk.Utilities;
using System.Text.Encodings.Web;
using Microsoft.CodeAnalysis.Classification;
using System.Text;
using System.Collections;

namespace Codex.Framework.Generator;

public partial record GeneratorContext(string OutputPath)
{
    public readonly string ObjectModelProjectGenPath = GetProjectOutputPath(OutputPath, "Codex.ObjectModel");
    public readonly string SdkProjectGenPath = GetProjectOutputPath(OutputPath, "Codex.Sdk");
    public readonly string GeneratorProjectGenPath = GetProjectOutputPath(OutputPath, "Codex.Framework.Generator");
    public readonly string GeneratorProjectPath = GetProjectOutputPath(OutputPath, "Codex.Framework.Generator", output: false);
    public readonly string WebCommonProjectGenPath = GetProjectOutputPath(OutputPath, "Codex.Web.Common");
    public readonly string ManagedProjectGenPath = GetProjectOutputPath(OutputPath, "Codex.Analysis.Managed");
    public string ModelGenPath => Path.Combine(SdkProjectGenPath, "Model.g.cs");

    public IReadOnlyDictionary<Type, TypeDefinition> Types;

    public NamespaceDeclaration ModelNamespace = new("Codex.ObjectModel.Implementation")
    {
        Usings =
        {
            new Dom.UsingDirective($"static {nameof(PropertyTarget)}"),
            new Dom.UsingDirective(typeof(IJsonRangeTracking<>).Namespace)
        }
    };

    CompilationUnit CompileUnit = new CompilationUnit();
    public ClassDeclaration SearchMappingsClass = new("SearchMappings")
    {
        Modifiers = Modifiers.Public
    };


    public ClassDeclaration DescriptorsClass = new("Descriptors")
    {
        Modifiers = Modifiers.Public
    };

    public KeyTrackingInfo KeyTracking { get; set; }

    public Workspace Workspace = new AdhocWorkspace();

    private string KeyTrackingPath;

    public void Initialize()
    {
        var options = new JsonSerializerOptions()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        KeyTrackingPath = Path.Combine(GeneratorProjectGenPath, "KeyTrackingInfo.json");
        var propertyMappingJson = File.Exists(KeyTrackingPath)
            ? File.ReadAllText(KeyTrackingPath)
            : "{}";

        KeyTracking = JsonSerializer.Deserialize<KeyTrackingInfo>(propertyMappingJson, options);
        GenerateLocalsWalker();
        CheckEnums();
        GenerateClassificationTypes();

        var assembly = typeof(SearchBehavior).Assembly;

        AddSearchMappings();

        Types = assembly.GetTypes().OrderBy<Type, string>(t => t.Name)
            .Where<Type>(t => ShouldIncludeType(t))
            .Select<Type, TypeDefinition>(ToTypeDefinition)
            .Where<TypeDefinition>(t => !t.IsExcluded)
            .ToImmutableDictionary<TypeDefinition, Type>(t => t.Type);

        CompileUnit.Usings.Add(new Dom.UsingDirective(typeof(PropertyTarget).Namespace));
        CompileUnit.AddNamespace(ModelNamespace);

        DiscoverBaseTypes();

        AddProperties();

        ModelNamespace.Types.Add(DescriptorsClass);

        AddObjectStages();

        //using var modelFileWriter = new StreamWriter(Path.Combine(OutputPath, "Model.g.cs"));
        PostProcessAndSave(CompileUnit, ModelGenPath);

        KeyTracking.SortedEntityPropertyNames = KeyTracking.EntityPropertyNames?.ToImmutableSortedDictionary(StringComparer.OrdinalIgnoreCase);

        File.WriteAllText(KeyTrackingPath, JsonSerializer.Serialize(KeyTracking, options));
    }

    private static bool ShouldIncludeType(Type t)
    {
        bool result = t.IsInterface
                        && !t.IsNested
                        && t.GetMethods().Where(m => !m.IsSpecialName && m.IsAbstract).Count() == 0;

        result &= t.Namespace != "Codex.Sdk.Utilities";

        return result;
    }

    private void PostProcessAndSave(CompilationUnit unit, string path)
    {
        CSharpCodeGenerator generator = new CodeGenerator();
        var modelWriter = new StringWriter();
        modelWriter.NewLine = Environment.NewLine;
        generator.Write(modelWriter, unit);

        var tree = CSharpSyntaxTree.ParseText(modelWriter.ToString())
            .WithFilePath(path);
        PostProcessAndSave(tree);
    }

    private static string GetProjectOutputPath(string outputPath, string projectName, bool output = true)
    {
        var path = Path.Combine(outputPath, projectName, output ? ".gen" : "");
        Directory.CreateDirectory(path);
        return path;
    }

    private record ClassificationTypeInfo(string RealName)
    {
        private int? integralValue;
        public int? IntegralValue
        {
            get => integralValue;
            set
            {
                integralValue = value;
                Member.Value = value;
            }
        }
        public EnumerationMember Member { get; set; }
    }

    private void GenerateClassificationTypes()
    {
        var ns = new Dom.NamespaceDeclaration("Codex.ObjectModel");
        var typeName = "ClassificationName";
        var fullName = $"{ns.Name}.{typeName}";

        var decl = new Dom.EnumerationDeclaration(typeName)
        {
            Modifiers = Modifiers.Public
        };
        ns.AddType(decl);

        var xmlFields = typeof(Microsoft.Language.Xml.ClassificationTypeNames).GetFields()
            .Where(f => f.IsStatic)
            .Where(f => f.FieldType == typeof(string));

        var xmlClassifications = xmlFields.Select(f => f.GetRawConstantValue().ToString()).ToArray();

        var classificationTypeMap = ClassificationTypeNames.AllTypeNames
            .Concat(xmlClassifications)
            .Concat(Enum.GetNames<AdditionalClassificationNames>())
        .ToDictionary(n => GetRealClassificationName(n), n => new ClassificationTypeInfo(n));

        HashSet<int> availableClassTypeInts = Enumerable.Range(1, classificationTypeMap.Count).ToHashSet();

        var keyInfo = KeyTracking.Enums.GetOrAdd(fullName, new KeyTrackingInfo.EnumKeyTrackingInfo());

        foreach ((var name, var info) in classificationTypeMap)
        {
            info.Member = new EnumerationMember(name);
            decl.Members.Add(info.Member);

            if (keyInfo.FieldNames.TryGetValue(name, out int value))
            {
                info.IntegralValue = value;
                availableClassTypeInts.Remove(value);
            }
        }

        decl.Members.Add(new EnumerationMember("RecordName")
        {
            Value = new SnippetExpression(GetRealClassificationName(ClassificationTypeNames.RecordClassName))
        });

        foreach ((var name, var info) in classificationTypeMap)
        {
            if (info.IntegralValue == null)
            {
                var value = availableClassTypeInts.FirstOrDefault();
                availableClassTypeInts.Remove(value);
                info.IntegralValue = value;
            }
        }

        PostProcessAndSave(new Dom.CompilationUnit()
        {
            Namespaces =
            {
                ns
            }
        },
        Path.Combine(ObjectModelProjectGenPath, "ClassificationName.g.cs"));
    }

    private static string GetRealClassificationName(string n)
    {
        var sb = new StringBuilder();
        bool capitalizeNextLetter = true;
        for (int i = 0; i < n.Length; i++)
        {
            var ch = n[i];
            if (char.IsAsciiLetterOrDigit(ch))
            {
                sb.Append(capitalizeNextLetter ? char.ToUpperInvariant(ch) : ch);
                capitalizeNextLetter = false;
            }
            else
            {
                capitalizeNextLetter = true;
            }
        }


        return sb.ToString();
    }

    private void CheckEnums()
    {
        var assembly = typeof(SearchBehavior).Assembly;
        var enumTypes = assembly.GetTypes()
            .Where(t => t.IsEnum)
            .Where(t => t.GetCustomAttribute<GeneratorExcludeAttribute>() == null)
            .ToArray();

        var trackedEnums = KeyTracking.Enums;
        KeyTracking.Enums = new();

        foreach (var enumType in enumTypes)
        {
            if (!trackedEnums.TryGetValue(enumType.FullName, out var keyInfo))
            {
                keyInfo = new KeyTrackingInfo.EnumKeyTrackingInfo();
            }

            KeyTracking.Enums[enumType.FullName] = keyInfo;

            var intToNameMap = keyInfo.FieldNames.ToDictionarySafe(e => e.Value, e => e.Key);

            var names = Enum.GetNames(enumType);
            foreach (string name in names)
            {
                var member = Enum.Parse(enumType, name);
                var displayName = member.ToString();
                var value = ((IConvertible)Enum.Parse(enumType, name)).ToInt32(null);

                if (keyInfo.FieldNames.TryGetValue(name, out var oldValue))
                {
                    Contract.Check(oldValue == value)
                        ?.Assert($"Enum value changed(name={name}): old:'{oldValue}' != new:'{value}'");
                }
                else
                {
                    if (intToNameMap.TryGetValue(value, out var expectedName))
                    {
                        Contract.Check(displayName == expectedName)
                            ?.Assert($"Enum name changed(value={value}): old:'{expectedName}' != new:'{name}'");
                    }

                    keyInfo.FieldNames[name] = value;
                }
            }
        }
    }

    private void AddObjectStages()
    {
        var internalNamespace = new NamespaceDeclaration("Codex.ObjectModel.Internal")
        {
            Usings =
            {
                new UsingDirective(typeof(IBox).Namespace)
            }
        };
        CompileUnit.AddNamespace(internalNamespace);

        var stages = new ClassDeclaration("ObjectStages")
        {
            Modifiers = Modifiers.Public | Modifiers.Static
        };

        var getBox = new MethodDeclaration("GetBox")
        {
            Modifiers = Modifiers.Public | Modifiers.Static,
            ReturnType = new TypeReference(typeof(IBox<>)).MakeGeneric(typeof(IObjectStage)),
            Arguments =
            {
                new MethodArgumentDeclaration(typeof(ObjectStage), "stage")
            },
        }.Apply(m => stages.AddMember(m));

        var getBoxSwitch = new SwitchStatement()
        {
            Expresion = getBox.Arguments[0],
        };

        getBox.Statements = new()
        {
            getBoxSwitch,
            new ThrowStatement(new SnippetExpression("System.Diagnostics.ContractsLight.Contract.AssertFailure($\"Invalid object stage: {stage}\")"))
        };

        internalNamespace.AddType(stages);

        foreach (var stage in Enum.GetValues<ObjectStage>())
        {
            var type = new ClassDeclaration(stage.ToString())
            {
                Implements =
                {
                    new TypeReference(typeof(ObjectStageBase<>)).MakeGeneric(new TypeReference(stage.ToString())),
                    typeof(IObjectStage)
                }
            }.Apply(t => stages.AddType(t));

            var stageReference = new MemberReferenceExpression(typeof(ObjectStage), stage.ToString());
            getBoxSwitch.Cases.Add(new SwitchCaseDeclaration()
            {
                Case = stageReference,
                Statements = new()
                {
                    new ReturnStatement(new MemberReferenceExpression(
                        type,
                        nameof(IObjectStage.Box)))
                }
            });

            type.Members.Add(new MethodDeclaration(nameof(IObjectStage.GetValue))
            {
                Modifiers = Modifiers.Static,
                ReturnType = typeof(ObjectStage),
                PrivateImplementationType = typeof(IObjectStage),
                Statements = new ExpressionBodyStatementCollection()
                {
                    BodyExpression = stageReference
                }
            });
        }
    }

    private void AddSearchMappings()
    {
        ModelNamespace.Usings.Add(new UsingDirective("Codex.Sdk.Search"));
        ModelNamespace.Usings.Add(new UsingDirective("static Descriptors"));

        var clientInterface = new InterfaceDeclaration("IClient")
        {
            Modifiers = Modifiers.Public
        };

        var clientBase = new ClassDeclaration("ClientBase")
        {
            Modifiers = Modifiers.Partial
        };

        var clientBaseConstructor = new ConstructorDeclaration()
        {
            Modifiers = Modifiers.Protected
        };

        clientBase.Members.Add(clientBaseConstructor);

        ModelNamespace.Types.Add(clientInterface);
        ModelNamespace.Types.Add(clientBase);

        ModelNamespace.Types.Add(SearchMappingsClass);

        foreach (var searchType in SearchTypes.RegisteredSearchTypes.OrderBy(p => p.Name))
        {
            clientInterface.Members.Add(new PropertyDeclaration(searchType.Name + "Index",
                new TypeReference("IIndex").MakeGeneric(searchType.Type)).AddAutoGet());

            var lazyIndexField = new FieldDeclaration($"_lazy{searchType.Name}Index",
                new TypeReference("Lazy").MakeGeneric(new TypeReference("IIndex").MakeGeneric(searchType.Type)))
            {
                Modifiers = Modifiers.Private | Modifiers.ReadOnly
            };

            clientBase.Members.Add(lazyIndexField);

            clientBaseConstructor.Statements.Add(new AssignStatement(lazyIndexField,
                new MemberReferenceExpression(null, "GetIndexFactory").InvokeMethod(new TypeReferenceExpression(typeof(SearchTypes))
                    .Member(searchType.Name))));

            clientBase.Members.Add(new PropertyDeclaration(searchType.Name + "Index",
                new TypeReference("IIndex").MakeGeneric(searchType.Type))
            {
                Modifiers = Modifiers.Public,
                Getter = new PropertyAccessorDeclaration()
                {
                    Statements =
                    {
                        new ReturnStatement(lazyIndexField.Member("Value"))
                    }
                }
            });

            var mappingType = new ClassDeclaration(searchType.Name)
            {
                Modifiers = Modifiers.Public
            };
            SearchMappingsClass.Types.Add(mappingType);
            foreach (var searchField in searchType.Fields.Values)
            {
                if (searchField.Behavior == SearchBehavior.None) continue;

                var fieldType = searchField.FieldType;
                Contract.Check(fieldType == typeof(string) || !fieldType.IsAssignableTo(typeof(IEnumerable)))
                    ?.Assert($"Field {searchType.Name}.{searchField.Name} is {fieldType} which implements IEnumerable. Use SearchMultiField.");
                Contract.Check(typeof(IVisitor).IsAssignableTo(typeof(IValueVisitor<>).MakeGenericType(searchField.FieldType)))
                    ?.Assert($"IVisitor does not implement IValueVisitor<{searchField.FieldType}> for field {searchType.Name}.{searchField.Name}");
                Contract.Check(typeof(IQueryFactory<int>).IsAssignableTo(typeof(IQueryFactory<,>).MakeGenericType(typeof(int), searchField.FieldType)))
                    ?.Assert($"IQueryFactory<TQuery> does not implement IQueryFactory<TQuery,{searchField.FieldType}> for field {searchType.Name}.{searchField.Name}");

                var propertyType = new TypeReference(searchField.Behavior != SearchBehavior.Sortword && searchField.Behavior != SearchBehavior.SortValue
                        ? typeof(IMappingField<,>)
                        : typeof(ISortField<,>))
                    .MakeGeneric(searchType.Type, searchField.FieldType);
                var property = new InitializedPropertyDeclaration(searchField.Name, propertyType)
                {
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    InitExpression = new MemberReferenceExpression(typeof(SearchTypes), searchType.Name)
                    .Member(nameof(SearchType<ISearchEntity>.GetMappingField))
                    .InvokeMethod(new[] { new TypeReference(searchField.FieldType) })
                }
                .AddAutoGet();
                mappingType.Members.Add(property);
            }

            foreach ((string mode, int index) in searchType.Includes.WithIndices())
            {
                var propertyType = new TypeReference(typeof(Include<>))
                    .MakeGeneric(searchType.Type);
                var property = new InitializedPropertyDeclaration("Include" + mode, propertyType)
                {
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    InitExpression = new MemberReferenceExpression(typeof(SearchTypes), searchType.Name)
                        .Member(nameof(SearchType<ISearchEntity>.GetInclude))
                        .InvokeMethod(new[] { new LiteralExpression(index) })
                }
                .AddAutoGet();
                mappingType.Members.Add(property);
            }
        }
    }

    private void PostProcessAndSave(SyntaxTree tree)
    {
        var rewriter = new PostProcessRewriter();
        SyntaxNode node = tree.GetRoot();
        node = rewriter.Visit(node.NormalizeWhitespace());
        node = Formatter.Format(node, Workspace);
        using var modelFileWriter = new StreamWriter(tree.FilePath);
        node.WriteTo(modelFileWriter);
    }

    private void AddProperties()
    {
        foreach (var type in Types.Values.OrderBy(p => p.Type.Name))
        {
            AddProperties(type);
        }
    }

    private void AddProperties(TypeDefinition type)
    {
        if (type.Properties != null)
        {
            return;
        }

        if (type.BaseDefinition != null && type.BaseDefinition.Properties == null)
        {
            AddProperties(type.BaseDefinition);
        }

        var decl = type.ModelDeclaration;
        type.Interfaces = new[] { type.Type }.Concat(type.GetBaseInterfaces()).ToImmutableHashSet()
            .Union(type.BaseDefinition?.Interfaces ?? ImmutableHashSet<Type>.Empty);
        type.Properties = type.BaseDefinition?.Properties ?? ImmutableDictionary<string, PropertyData>.Empty;

        decl.Members.Add(new ConstructorDeclaration()
        {
            Modifiers = Modifiers.Public
        });

        //decl.Members.Add(new MethodDeclaration(nameof(IPropertyTarget.CreateClone))
        //{
        //    PrivateImplementationType = typeof(IPropertyTarget),
        //    ReturnType = typeof(object),
        //    Statements = new()
        //    {
        //        new ReturnStatement(new NewObjectExpression(decl, new CastExpression(new ThisExpression(), type.Type)))
        //    }
        //});

        var copyMethod = new MethodDeclaration("CopyFrom")
        {
            Modifiers = Modifiers.Public,
            Statements = new StatementCollection()
        };

        type.CopyMethodDeclaration = copyMethod;
        copyMethod.AddArgument("source", type.Type);
        var arg = copyMethod.AddArgument("shallow", typeof(bool));
        arg.DefaultValue = false;

        foreach (var baseType in type.Interfaces.OrderBy(i => i.Name))
        {
            if (Types.TryGetValue(baseType, out var baseTypeDefinition) && baseTypeDefinition.ShouldGenerate)
            {
                var sourceArg = new MethodArgumentDeclaration(baseType, "source");
                var shallowArg = new MethodArgumentDeclaration(typeof(bool), "shallowCopy")
                {
                    DefaultValue = false
                };

                decl.Members.Add(new ConstructorDeclaration()
                {
                    Modifiers = Modifiers.Public,
                    Arguments = { sourceArg, shallowArg },
                    Statements = new()
                    {
                        new MethodInvokeExpression(new MemberReferenceExpression(new ThisExpression(), nameof(PropertyTarget.Apply)), sourceArg, shallowArg)
                    }
                });

                if (type.BaseDefinition?.Interfaces.Contains(baseType) != true)
                {
                    var propertyTargetType = new TypeReference(nameof(IPropertyTarget<IEnt, Ent>)).MakeGeneric(
                        baseType, baseTypeDefinition.ModelDeclaration);

                    //type.ModelDeclaration.AddMember(new PropertyDeclaration(nameof(IPropertyTarget<IEnt>.BaseSourceDescriptor), typeof(IBaseDescriptor<>).MakeGenericType(baseType))
                    //{
                    //    Getter = new ExpressionBodyStatementCollection()
                    //    {
                    //        BodyExpression = new Dom.MemberReferenceExpression(
                    //            new TypeReference(baseTypeDefinition.ModelDeclaration),
                                
                    //            ) 
                    //    }
                    //});

                    decl.Implements.Add(propertyTargetType);
                    //if (baseType == type.Type)
                    //{
                    //    type.ModelDeclaration.Members.Add(copyMethod);
                    //}
                    //else
                    //{
                    //    type.ModelDeclaration.Members.Add(new ReparentedMemberDeclaration()
                    //    {
                    //        GetExternalMemberDeclaration = () => baseTypeDefinition.CopyMethodDeclaration
                    //    });
                    //}
                }
            }

            foreach (var property in baseType.GetProperties().OrderBy(p => p.Name))
            {
                if (property.GetCustomAttribute<GeneratorExcludeAttribute>() != null)
                {
                    continue;
                }

                AddProperty(type, property, out var propertyType, out bool isList, out bool isModelType, out var copyTypeRef);
            }
        }

        foreach (var property in type.Properties.Values.OrderBy(p => p.Name))
        {
            AddProperty(type, property.Info, out var propertyType, out bool isList, out bool isModelType, out var copyTypeRef);

            type.DescriptorConstructor.Statements.Add(property.GetAddExpression());

            copyMethod.Statements.Add(
                new AssignStatement(
                    new MemberReferenceExpression(null, property.Name),
                    new MethodInvokeExpression(
                        new MemberReferenceExpression(null, nameof(PropertyTarget.GetOrCopy)),
                        new MemberReferenceExpression(null, property.Name),
                        new MemberReferenceExpression(copyMethod.Arguments[0], property.Name),
                        copyMethod.Arguments[1])
                    {
                        Parameters =
                        {
                                   copyTypeRef,
                                   propertyType
                        }
                    }));
        }

        type.DescriptorConstructor.Initializer = new ConstructorBaseInitializer(
            new LiteralExpression(
            type.Properties.Count != 0
                ? type.Properties.Max(p => p.Value.FieldNumber)
                : 0),
            new LiteralExpression(type.Properties.Count));

        decl.Implements.Add(type.IEntityType);
        decl.Members.Sort(new ComparerBuilder<MemberDeclaration>()
            .CompareByAfter(m => m is ConstructorDeclaration ? 0 : (m is MethodDeclaration ? 1 : 2)));

        //decl.Members.Add(new MethodDeclaration(nameof(IEntity<Ent, IEnt>.GetDescriptor))
        //{
        //    ReturnType = new TypeReference(nameof(DescriptorBase<Ent, IEnt>)).MakeGeneric(
        //        type.DescriptorDeclaration.BaseType.Parameters[0],
        //        type.DescriptorDeclaration.BaseType.Parameters[1]),
        //    Modifiers = Modifiers.Static,
        //    PrivateImplementationType = type.IEntityType,
        //    Statements = new ExpressionBodyStatementCollection()
        //    {
        //        BodyExpression = new MemberReferenceExpression(
        //            new TypeReference(type.DescriptorDeclaration), "Instance")
        //    }
        //});
    }

    private void AddProperty(
        TypeDefinition type,
        PropertyInfo property,
        out Type propertyType,
        out bool isList,
        out bool isModelType,
        out TypeReference copyTypeRef)
    {
        bool isListCoerce = false;
        var coerceGet = property.GetCustomAttribute<CoerceGetAttribute>();
        var isReadOnlyList = property.GetCustomAttribute<ReadOnlyListAttribute>() != null;
        var useInterface = property.GetCustomAttribute<UseInterfaceAttribute>() != null;

        propertyType = property.PropertyType;
        isList = propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);
        if (isList)
        {
            propertyType = propertyType.GenericTypeArguments[0];
        }

        isModelType = Types.TryGetValue(propertyType, out var modelType) && !useInterface;
        var propertyTypeReference = getPropertyTypeReference(propertyType, isList, isModelType);


        TypeReference getPropertyTypeReference(Type propertyType, bool isList, bool isModelType)
        {
            if (isList)
            {
                if (coerceGet == null && !isReadOnlyList)
                {
                    isListCoerce = true;
                    coerceGet = new CoerceGetAttribute(useRef: true);
                }

                string listType = isReadOnlyList ? "IReadOnlyList" : "List";
                if (isModelType)
                {
                    return new TypeReference(listType).MakeGeneric(new TypeReference(modelType.BaseName));
                }
                else
                {
                    return new TypeReference(listType).MakeGeneric(new TypeReference(propertyType));
                }
            }
            else if (isModelType)
            {
                return new TypeReference(modelType.BaseName);
            }
            else
            {
                return new TypeReference(propertyType);
            }
        }

        copyTypeRef = isList ? propertyTypeReference.Parameters[0] : propertyTypeReference;

        if (type.Properties.TryGetValue(property.Name, out _))
        {
            return;
        }

        KeyTracking.EntityPropertyNames.TryAdd(property.Name, KeyTracking.EntityPropertyNames.Count);

        var fieldNumber = KeyTracking.EntityPropertyNames[property.Name];

        var propertyData = new PropertyData(property)
        {
            FieldNumber = fieldNumber
        };
        type.Properties = type.Properties.SetItem(property.Name, propertyData);

        var propertyDeclaration = new InitializedPropertyDeclaration(property.Name, propertyTypeReference)
            .Apply(p => p.Modifiers = Modifiers.Public);

        MemberDeclaration interfaceBackingMember = propertyDeclaration;

        type.ModelDeclaration.Members.Add(propertyDeclaration);

        createDescriptorPropertyAdd(propertyType, isList, isModelType, modelType, propertyData);

        void createDescriptorPropertyAdd(Type propertyType, bool isList, bool isModelType, TypeDefinition modelType, PropertyData propertyData)
        {
            propertyData.GetAddExpression = () => new MethodInvokeExpression(new MemberReferenceExpression(null, "Add"),
                new NewObjectExpression(
                    new TypeReference(isList
                    ? nameof(DescriptorBase<Ent, IEnt>.ListProperty<int, int>)
                    : nameof(DescriptorBase<Ent, IEnt>.Property<int, int>)).MakeGeneric(
                        isModelType ? new TypeReference(modelType.BaseName) : new TypeReference(propertyType),
                        propertyType),
                    new LiteralExpression(fieldNumber),
                    new LiteralExpression(property.Name),
                    new SnippetExpression($"e => e.{property.Name}"),
                    new SnippetExpression($"(e, v) => e.{property.Name} = v")
                ));
        }

        if (coerceGet == null)
        {
            var localPropertyType = propertyType;
            propertyDeclaration
                .ApplyIf(isList || isReadOnlyList, p => p.InitExpression =
                    isReadOnlyList
                    ? new MemberReferenceExpression(typeof(Array), "Empty").InvokeMethod(new[] { propertyTypeReference.Parameters[0] })
                    : new NewObjectExpression())
                .ApplyIf(propertyType.Namespace == "System.Collections.Immutable",
                    p => p.InitExpression = new MemberReferenceExpression(localPropertyType, "Empty"))
            .AddAutoGet()
            .AddAutoSet();
        }
        else
        {
            var field = new FieldDeclaration("m_" + property.Name, coerceGet.CoercedSourceType ?? propertyTypeReference)
                .Apply(f => f.Modifiers = Modifiers.Private);
            type.ModelDeclaration.Members.Add(field);

            if (isListCoerce)
            {
                interfaceBackingMember = field;
            }

            // get => Coerce{PropertyName}(m_{PropertyName});
            propertyDeclaration.Getter = new ExpressionBodyStatementCollection(
                CreateCoerceCall(
                    isListCoerce ? nameof(PropertyTarget.Coerce) : "Coerce" + property.Name,
                    field,
                    useRef: isListCoerce));

            // set => m_{PropertyName} = value;
            propertyDeclaration.Setter = new AssignStatement(
                field,
                new ValueArgumentExpression());
        }

        if (isList || isModelType)
        {
            // Need to generate explicit interface implementation since
            // signature will not match for generated property

            CodeMemberProperty interfaceImplProperty = new PropertyDeclaration(property.Name, new TypeReference(property.PropertyType))
            {
                PrivateImplementationType = new TypeReference(property.DeclaringType)
            };

            
            interfaceImplProperty.Getter = new PropertyAccessorDeclaration(
                   new ExpressionBodyStatementCollection(
                        isListCoerce 
                        ? CreateCoerceCall(nameof(PropertyTarget.CoerceReadOnly), interfaceBackingMember, useRef: true)
                        : new MemberReferenceExpression(null, property.Name))
                   );


            type.ModelDeclaration.Members.Add(interfaceImplProperty);
        }
    }

    private MethodInvokeExpression CreateCoerceCall(string name, Expression member, bool useRef)
    {
        return new MethodInvokeExpression(new MemberReferenceExpression(null, name),
            new MethodInvokeArgumentExpression(member, useRef ? Direction.InOut : Direction.In));
    }

    private void DiscoverBaseTypes()
    {
        var typeMap = new ClassDeclaration("EntityTypes")
        {
            Modifiers = Modifiers.Public | Modifiers.Static | Modifiers.Partial
        };

        CollectionInitializerExpression createMap(string name)
        {
            var initializer = new CollectionInitializerExpression(typeof(Dictionary<Type, Type>));

            typeMap.Members.Add(new InitializedPropertyDeclaration(name, typeof(IReadOnlyDictionary<Type, Type>))
            {
                Modifiers = Modifiers.Public | Modifiers.Static,
                InitExpression = initializer
            });

            return initializer;
        }

        var toImplInitializer = createMap("ToImplementationMap");
        var toAdapterImplInitializer = createMap("ToAdapterImplementationMap");
        var fromImplInitializer = createMap("FromImplementationMap");
        var fromAdapterImplInitializer = createMap("FromAdapterImplementationMap");

        ModelNamespace.Types.Add(typeMap);

        foreach (var typeDefinition in Types.Values.OrderBy(p => p.Type.Name))
        {
            if (typeDefinition.ShouldGenerate || typeDefinition.IsAdapter)
            {
                (typeDefinition.IsAdapter ? toAdapterImplInitializer : toImplInitializer)
                    .Items.Add(new ExpressionCollectionStatement2(
                        new TypeOfExpression(typeDefinition.Type),
                        new TypeOfExpression(new TypeReference(typeDefinition.BaseName))
                    ));

                (typeDefinition.IsAdapter ? fromAdapterImplInitializer : fromImplInitializer)
                    .Items.Add(new ExpressionCollectionStatement2(
                        new TypeOfExpression(new TypeReference(typeDefinition.BaseName)),
                        new TypeOfExpression(typeDefinition.Type)
                    ));

                var toImplMethod = new MethodDeclaration("ToImplementation")
                {
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Arguments =
                    {
                        new MethodArgumentDeclaration(typeDefinition.Type, "entity") { IsExtension = true },
                    },
                    ReturnType = new TypeReference(typeDefinition.BaseName)
                }.Apply(m =>
                    m.Statements = new ExpressionBodyStatementCollection(new CastExpression(m.Arguments[0], m.ReturnType)));

                var fromImplMethod = new MethodDeclaration("FromImplementation")
                {
                    Modifiers = Modifiers.Public | Modifiers.Static,
                    Arguments =
                    {
                        new MethodArgumentDeclaration(toImplMethod.ReturnType, "entity") { IsExtension = true },
                    },
                    ReturnType = toImplMethod.Arguments[0].Type
                }.Apply(m =>
                    m.Statements = new ExpressionBodyStatementCollection(new CastExpression(m.Arguments[0], m.ReturnType)));

                typeMap.AddMember(toImplMethod);
                typeMap.AddMember(fromImplMethod);
            }


            var baseType = typeDefinition.GetBaseInterfaces(generatedOnly: true).FirstOrDefault();
            if (baseType != null)
            {
                typeDefinition.BaseDefinition = Types.GetValueOrDefault(baseType);
                if (typeDefinition.BaseDefinition != null)
                {
                    typeDefinition.ModelDeclaration.BaseType = new TypeReference(typeDefinition.BaseDefinition.BaseName);
                }
            }
            else
            {
                typeDefinition.ModelDeclaration.BaseType = typeof(EntityBase);
            }

            foreach (var baseInterface in new[] { typeDefinition.Type }.Concat(
                typeDefinition.GetBaseInterfaces().Skip(1)))
            {
                typeDefinition.ModelDeclaration.Implements.Add(baseInterface);
            }
        }
    }

    private TypeDefinition ToTypeDefinition(Type type)
    {
        return new TypeDefinition(type, this);
    }

    private class CodeGenerator : CSharpCodeGenerator
    {
        private readonly WriteStatementOptions _inlineStatementWriteStatementOptions = new WriteStatementOptions
        {
            EndStatement = false
        };

        protected override void WritePropertyDeclaration(IndentedTextWriter writer, PropertyDeclaration member)
        {
            base.WritePropertyDeclaration(writer, member);

            if (member is InitializedPropertyDeclaration init && init.InitExpression != null)
            {
                writer.Write(" = ");
                WriteExpression(writer, init.InitExpression);
                writer.WriteLine(";");
            }
        }

        protected override void WriteStatement(IndentedTextWriter writer, Statement statement)
        {
            if (statement is SwitchStatement sw)
            {
                writer.Write("switch (");
                writer.Write(sw.Expresion);
                writer.WriteLine(")");
                writer.WriteLine("{");
                writer.Indent++;
                foreach (var c in sw.Cases)
                {
                    if (c.Case != null)
                    {
                        writer.Write("case ");
                        writer.Write(c.Case);
                    }
                    else
                    {
                        writer.Write("default");
                    }

                    writer.WriteLine(":");

                    if (c.Statements != null)
                    {
                        WriteStatements(writer, c.Statements);
                    }
                }
                writer.Indent--;
                writer.WriteLine("}");

                return;
            }

            base.WriteStatement(writer, statement);
        }

        protected override void WriteStatements(IndentedTextWriter writer, CodeStatementCollection statements)
        {
            if (statements is ExpressionBodyStatementCollection expressionBody)
            {
                writer.Write(" => ");
                WriteExpression(writer, expressionBody.BodyExpression);
                writer.WriteLine(";");
            }
            else
            {
                base.WriteStatements(writer, statements);
            }
        }

        protected override void WriteNewObjectExpression(IndentedTextWriter writer, NewObjectExpression expression)
        {
            base.WriteNewObjectExpression(writer, expression);
            if (expression is CollectionInitializerExpression initializer)
            {
                writer.WriteLine();
                writer.WriteLine("{");
                writer.Indent++;
                {
                    foreach (var item in initializer.Items)
                    {
                        writer.Write("{ ");
                        WriteExpressionCollectionStatement(writer, item, _inlineStatementWriteStatementOptions);
                        writer.WriteLine(" },");
                    }
                }
                writer.Indent--;
                writer.WriteLine("}");

            }
        }

        protected override void WriteMemberDeclaration(IndentedTextWriter writer, MemberDeclaration member)
        {
            if (member is ReparentedMemberDeclaration reparented)
            {
                member = reparented.GetExternalMemberDeclaration();
            }
            base.WriteMemberDeclaration(writer, member);
        }
    }

    public class ExpressionCollectionStatement2 : ExpressionCollectionStatement
    {
        public ExpressionCollectionStatement2(params Expression[] expressions)
        {
            foreach (var expression in expressions)
            {
                Expression ignored = null;
                SetParent(ref ignored, expression);
                Add(expression);
            }
        }
    }

    public class ReparentedMemberDeclaration : MemberDeclaration
    {
        public Func<MemberDeclaration> GetExternalMemberDeclaration { get; init; }
    }

    public class ExpressionBodyStatementCollection : StatementCollection
    {
        public Expression BodyExpression { get; set; }

        public ExpressionBodyStatementCollection(Expression bodyExpression)
        {
            BodyExpression = bodyExpression;
        }

        public ExpressionBodyStatementCollection()
        {
        }
    }

    public class SwitchStatement : Statement
    {
        public Expression Expresion { get; set; }
        public List<SwitchCaseDeclaration> Cases { get; } = new List<SwitchCaseDeclaration>();
    }

    public class SwitchCaseDeclaration
    {
        public Expression Case { get; set; }
        public StatementCollection Statements { get; set; }
    }

    public class InitializedPropertyDeclaration : PropertyDeclaration
    {
        public InitializedPropertyDeclaration(string name, CodeTypeReference type)
            : base(name, type)
        {
            this.AddAutoGet();
        }

        public Expression InitExpression { get; set; }
    }

    public class LambdaExpression : Expression
    {

    }

    public class CollectionInitializerExpression : NewObjectExpression
    {
        public CollectionInitializerExpression(CodeTypeReference type, params Expression[] arguments) : base(type, arguments)
        {
        }

        public List<ExpressionCollectionStatement> Items { get; } = new();
    }

    private class PostProcessRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitUsingDirective(UsingDirectiveSyntax node)
        {
            return node;
        }

        public override SyntaxNode VisitAliasQualifiedName(AliasQualifiedNameSyntax node)
        {
            return Visit(node.Name);
        }

        public override SyntaxNode VisitQualifiedName(QualifiedNameSyntax node)
        {
            return Visit(node.Right);
        }

        public override SyntaxNode VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
        {
            return node.WithMembers(VisitList(node.Members));
        }
    }
}