using Codex.ObjectModel;
using Codex.ObjectModel.CompilerServices;
using Codex.Sdk.Search;
using Codex.Sdk.Utilities;
using Codex.Storage.BlockLevel;
using Codex.Utilities;
using Codex.Web.Common;
using Codex.Web.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Web;

#nullable enable annotations

namespace Codex.View
{
    public record ViewModelAddress
    {
        public string repo;
        public string signature;
        public SourceControlUri[]? indexRepo;
        public string rightProjectId;
        public string leftProjectId;
        public string filePath;
        public string leftSymbolId;
        public string projectScope;
        public string rightSymbolId;
        public ReferenceKind? refKind;
        public int? lineNumber;
        public int? buildId;
        public string searchText;
        public RepoAccess? access;
        public RightPaneMode rightPaneMode;
        public LeftPaneMode leftPaneMode;
        public OverviewKind? overviewMode;

        private class Converter<T>
        {
            public static Func<string, T> Convert;
            public static Func<T, string> ToQueryString;
        }

        private delegate ref T FieldFunc<T>(ViewModelAddress address);

        private static QueryDescriptor Descriptor { get; } = new QueryDescriptor();

        private static FieldFunc<string> RightProjectId = address => ref address.rightProjectId;

        static ViewModelAddress()
        {
            Converter<SourceControlUri[]?>.Convert = s => SourceControlUri.ParseRepoList(s);
            Converter<SourceControlUri[]?>.ToQueryString = s => s?.GetRepoListString();
            Converter<RepoAccess?>.Convert = s => string.IsNullOrEmpty(s) ? null : Enum.Parse<RepoAccess>(s, ignoreCase: true);
            Converter<RightPaneMode>.Convert = s => Enum.Parse<RightPaneMode>(s, ignoreCase: true);
            Converter<LeftPaneMode>.Convert = s => Enum.Parse<LeftPaneMode>(s, ignoreCase: true);
            Converter<ReferenceKind?>.Convert = s => string.IsNullOrEmpty(s) ? null : Enum.Parse<ReferenceKind>(s, ignoreCase: true);
            Converter<OverviewKind?>.Convert = s => string.IsNullOrEmpty(s) ? null : Enum.Parse<OverviewKind>(s, ignoreCase: true);
            Converter<string>.Convert = s => s;
            Converter<int?>.Convert = s => string.IsNullOrEmpty(s) ? null : int.Parse(s);
        }

        public record struct InferMode(bool Enabled, bool OnlyIfNoModeSpecified = true, bool IsStartup = false)
        {
            public static implicit operator InferMode(bool value)
            {
                return new InferMode(value);
            }

            public static InferMode Default { get; } = new InferMode(true);
            public static InferMode Startup { get; } = new InferMode(true, IsStartup: true);
        }

        public async Task NavigateAsync(MainController app, InferMode? infer = null)
        {
            if (indexRepo != null)
            {
                await app.Controller.IndexRepository(indexRepo);
            }

            var view = app.ViewModel;
            //view.ApplyAddress(this);

            var beforeNavigationBar = view.NavigationBar;
            await Descriptor.Navigate(this.GetClone(), app, infer ?? InferMode.Default);

            var afterNavigationBar = view.NavigationBar;
            if (beforeNavigationBar == afterNavigationBar)
            {
                // Navigation bar was not updated by view model
                view.ApplyAddress(this);
            }

            app.Controller.UpdateNavigationBar();
        }

        public ViewModelAddress With(ViewModelAddress source)
        {
            var clone = GetClone();
            Descriptor.Apply(source, clone);
            return clone;
        }

        public ViewModelAddress GetClone()
        {
            return this with { };
        }

        public static ViewModelAddress ForSymbol(IReferenceSymbol symbol, string referencingProjectId, out bool isDistributedDefinition, out bool isDefinition, out bool isProjectScopedReference)
        {
            isDistributedDefinition =
                symbol.Kind == SymbolKinds.MSBuildItem ||
                symbol.Kind == SymbolKinds.MSBuildItemMetadata ||
                symbol.Kind == SymbolKinds.MSBuildProperty ||
                symbol.Kind == SymbolKinds.MSBuildTarget ||
                symbol.Kind == SymbolKinds.MSBuildTask ||
                symbol.Kind == SymbolKinds.MSBuildTaskParameter;
            isDefinition = symbol.ReferenceKind == ReferenceKind.Definition || isDistributedDefinition;

            isProjectScopedReference = symbol.ReferenceKind == ReferenceKind.ProjectLevelReference;

            if (isProjectScopedReference && !symbol.Id.IsValid)
            {
                return ViewReferenceSymbolsXml(referencingProjectId, symbol.ProjectId);
            }

            ReferenceKind? refKind = symbol.ReferenceKind.FindAllReferenceKinds(fallbackToNull: true) != null
                ? symbol.ReferenceKind
                : null;

            if (isDefinition || isProjectScopedReference || refKind != null)
            {
                return FindAllReferences(
                    symbol.ProjectId,
                    symbol.Id.Value,
                    projectScope: isProjectScopedReference ? referencingProjectId : null,
                    refKind: refKind);
            }
            else
            {
                return GoToDefinition(symbol.ProjectId, symbol.Id.Value);
            }
        }

        public override string ToString()
        {
            return ToUrl().ToString();
        }

        public static ViewModelAddress ViewReferenceProjectsXml(string projectId)
        {
            return new ViewModelAddress()
            {
                rightPaneMode = RightPaneMode.referenceprojectsxml,
                rightProjectId = projectId
            };
        }

        public static ViewModelAddress ViewReferenceSymbolsXml(string projectId, string projectReferenceId)
        {
            return new ViewModelAddress()
            {
                rightPaneMode = RightPaneMode.referencesymbolsxml,
                rightProjectId = projectId,
                rightSymbolId = projectReferenceId
            };
        }

        public static ViewModelAddress GoToFile(string projectId, string filePath)
        {
            return GoToSpan(projectId, filePath, null);
        }

        public static ViewModelAddress GoToSpan(string projectId, string filePath, int? lineNumber = default, TargetSpan? targetSpan = null)
        {
            return new ViewModelAddress()
            {
                rightPaneMode = RightPaneMode.file,
                rightProjectId = projectId,
                filePath = filePath,
                lineNumber = lineNumber ?? targetSpan?.LineNumber,
                rightSymbolId = targetSpan?.SymbolId.Value.Value
            };
        }

        public static ViewModelAddress GoToDefinition(IDefinitionSymbol definition)
        {
            if (definition.Kind == SymbolKinds.Repo)
            {
                Placeholder.Todo("Open repo scope?");
                return Search($"#project {definition.ShortName}");
            }
            else if (definition.Kind == SymbolKinds.Project)
            {
                // TODO: Eventually we should navigate to the project applying repo scope
                // since project with same id can appear in multiple repos
                return ShowProjectExplorer(definition.DisplayName);
            }
            return GoToDefinition(definition.ProjectId, definition.Id);
        }

        public static ViewModelAddress GoToDefinition(string projectId, SymbolIdArgument symbolId)
        {
            return new ViewModelAddress()
            {
                rightPaneMode = RightPaneMode.symbol,
                rightProjectId = projectId,
                rightSymbolId = symbolId
            };
        }

        public static ViewModelAddress ShowOverview()
        {
            return new ViewModelAddress()
            {
                rightPaneMode = RightPaneMode.overview,
            };
        }

        public static ViewModelAddress ShowReposSummary(RepoAccess access = RepoAccess.Internal)
        {
            return new ViewModelAddress()
            {
                rightPaneMode = RightPaneMode.repossummary,
                access = access
            };
        }

        public static ViewModelAddress ShowDocumentOutline(string projectId, string filePath)
        {
            return new ViewModelAddress()
            {
                leftPaneMode = LeftPaneMode.outline,
                leftProjectId = projectId,
                filePath = filePath
            };
        }

        public static ViewModelAddress ShowNamespaceExplorer(string projectId)
        {
            return new ViewModelAddress()
            {
                leftPaneMode = LeftPaneMode.namespaces,
                leftProjectId = projectId,
            };
        }

        public static ViewModelAddress ShowProjectExplorer(string projectId)
        {
            return new ViewModelAddress()
            {
                leftPaneMode = LeftPaneMode.project,
                leftProjectId = projectId,
            };
        }

        public static ViewModelAddress Search(string text)
        {
            return new ViewModelAddress()
            {
                leftPaneMode = LeftPaneMode.search,
                searchText = text,
            };
        }

        public static ViewModelAddress FindAllReferences(string projectId, string symbolId, string projectScope = null, ReferenceKind? refKind = null)
        {
            return new ViewModelAddress()
            {
                leftPaneMode = LeftPaneMode.references,
                leftProjectId = projectId,
                leftSymbolId = symbolId,
                projectScope = projectScope,
                refKind = refKind
            };
        }

        public bool IsEmpty()
        {
            var queryParams = new Dictionary<string, string>();
            Descriptor.AppendParams(queryParams, this);
            return queryParams.Count == 0;
        }

        public static ViewModelAddress Parse(string value)
        {
            var address = new ViewModelAddress();
            try
            {
                string queryString = value?.TrimStart('/') ?? string.Empty;
                if (!queryString.StartsWith("?"))
                {
                    if (Uri.TryCreate(value, UriKind.Absolute, out var absoluteUri))
                    {
                        queryString = absoluteUri.Query;
                    }
                }
                var parameters = HttpUtility.ParseQueryString(queryString ?? string.Empty);
                var map = parameters.OfType<string>().ToDictionary(s => s, s => parameters[s], StringComparer.OrdinalIgnoreCase);
                address.Parse(map);
            }
            catch
            {
            }

            return address;
        }

        public void Parse(Dictionary<string, string> queryParams)
        {
            Descriptor.Parse(queryParams, this);
        }

        public Uri ToUrl()
        {
            var queryParams = new Dictionary<string, string>();

            Descriptor.AppendParams(queryParams, this);

            return WebHelper.ToQueryString(queryParams);
        }

        private class QueryDescriptor
        {
            private List<Param> Params { get; } = new List<Param>();
            private List<Mode> Modes { get; } = new List<Mode>();
            private Param<RightPaneMode> RightModeParam { get; }
            private Param<LeftPaneMode> LeftModeParam { get; }

            public QueryDescriptor()
            {
                var rightProjectId = CreateParam(a => ref a.rightProjectId, "rightProjectId");
                var rightSymbolId = CreateParam(a => ref a.rightSymbolId, "rightSymbolId");
                var leftSymbolId = CreateParam(a => ref a.leftSymbolId, "leftSymbolId");
                var leftProjectId = CreateParam(a => ref a.leftProjectId, "leftProjectId");
                var projectScope = CreateParam(a => ref a.projectScope, "projectScope");
                var filePath = CreateParam(a => ref a.filePath, "file");
                var line = CreateParam(a => ref a.lineNumber, "line");
                var refKind = CreateParam(a => ref a.refKind, "refKind");
                var leftMode = CreateParam(a => ref a.leftPaneMode, "leftMode");
                var rightMode = CreateParam(a => ref a.rightPaneMode, "rightMode");
                var searchText = CreateParam(a => ref a.searchText, "query");
                var repo = CreateParam(a => ref a.repo, "repo");
                var indexRepo = CreateParam(a => ref a.indexRepo);
                var buildId = CreateParam(a => ref a.buildId, "buildId");
                var access = CreateParam(a => ref a.access);
                var overviewModeParam = CreateParam(a => ref a.overviewMode);

                RightModeParam = rightMode;
                LeftModeParam = leftMode;

                Alternate(rightProjectId, leftProjectId);
                Alternate(rightSymbolId, leftSymbolId);

                CreateMode(LeftPaneMode.search, leftMode, (app, v) => app.SearchTextChanged(v.searchText),
                searchText);

                CreateMode(LeftPaneMode.outline, leftMode, (app, v) => app.ShowDocumentExplorer(new GetSourceArguments()
                {
                    ProjectId = v.leftProjectId,
                    ProjectRelativePath = v.filePath,
                    DefinitionOutline = true
                }),
                leftProjectId, filePath);

                CreateMode(RightPaneMode.symbol, rightMode, (app, v) => app.GoToDefinitionExecuted(new FindDefinitionLocationArguments()
                {
                    ProjectId = v.rightProjectId,
                    SymbolId = v.rightSymbolId,
                }),
                rightProjectId.Optional(), rightSymbolId);

                CreateMode(RightPaneMode.file, rightMode, (app, v) => app.GoToSpanExecuted(new GetSourceArguments()
                {
                    ProjectId = v.rightProjectId,
                    ProjectRelativePath = v.filePath,
                }, (v.lineNumber != null || v.rightSymbolId != null)
                    ? new TargetSpan(v.lineNumber, SymbolId: v.rightSymbolId)
                    : null),
                rightProjectId, filePath, line.Optional());

                var overviewMode = CreateMode(RightPaneMode.overview, rightMode, (app, v) =>
                {
                    return app.DisplayOverview(v.overviewMode ?? (Features.HideDefaultBranding ? OverviewKind.help : OverviewKind.home));
                }, overviewModeParam.Optional());
                overviewMode.CanInfer.Value = false;

                var reposSummaryMode = CreateMode(RightPaneMode.repossummary, rightMode, (app, v) =>
                {
                    return app.ShowRepositoriesSummary(new()
                    {
                        // Coerce Admin access to Internal access
                        AccessLevel = v.access < RepoAccess.Internal ? RepoAccess.Internal : v.access.Value
                    });
                },
                access);
                reposSummaryMode.CanInfer.Value = false;

                CreateMode(LeftPaneMode.references, leftMode, (app, v) => app.FindAllReferences(new FindAllReferencesArguments()
                {
                    ProjectId = v.leftProjectId,
                    SymbolId = v.leftSymbolId,
                    ProjectScopeId = v.projectScope,
                    ReferenceKind = v.refKind
                }),
                leftProjectId.Optional(), leftSymbolId, projectScope.Optional(), refKind.Optional());

                CreateMode(RightPaneMode.referenceprojectsxml, rightMode, (app, v) => app.ShowProjectExplorer(new GetProjectArguments()
                {
                    AddressKind = AddressKind.References,
                    ProjectId = v.leftProjectId,
                    ProjectScopeId = v.projectScope
                }),
                leftProjectId, projectScope.Optional());

                CreateMode(RightPaneMode.referencesymbolsxml, rightMode, (app, v) => app.ShowProjectExplorer(new GetProjectArguments()
                {
                    AddressKind = AddressKind.Definitions,
                    ProjectId = v.leftProjectId,
                    ReferencedProjectId = v.rightSymbolId,
                    ProjectScopeId = v.projectScope
                }),
                leftProjectId, projectScope.Optional());

                CreateMode(LeftPaneMode.project, leftMode, (app, v) => app.ShowProjectExplorer(new GetProjectArguments()
                {
                    ProjectId = v.leftProjectId,
                    ProjectScopeId = v.projectScope
                }),
                leftProjectId, projectScope.Optional());

                CreateMode(LeftPaneMode.namespaces, leftMode, (app, v) => app.ShowProjectExplorer(new GetProjectArguments()
                {
                    ProjectId = v.leftProjectId,
                    ProjectScopeId = v.projectScope,
                    AddressKind = AddressKind.TopLevelDefinitions
                }),
                leftProjectId, projectScope.Optional());
            }

            private Mode<T> GetMode<T>(T mode)
            {
                return Modes.OfType<Mode<T>>().Where(m => EqualityComparer<T>.Default.Equals(m.Value, mode)).FirstOrDefault();
            }

            private enum ParamOrigin
            {
                Unset = 0,
                Source,
                Target,
            }

            public void Apply(ViewModelAddress source, ViewModelAddress target)
            {
                Span<ParamOrigin> origins = stackalloc ParamOrigin[Params.Count];
                Span<bool> targetPriorFields = stackalloc bool[Params.Count];

                void selectOrigins(Mode sourceMode, Mode targetMode, Span<ParamOrigin> origins)
                {
                    if (sourceMode != null) setOrigins(source, sourceMode, ParamOrigin.Source, origins);
                    else if (targetMode != null) setOrigins(target, targetMode, ParamOrigin.Target, origins);
                }

                void setOrigins(ViewModelAddress address, Mode mode, ParamOrigin origin, Span<ParamOrigin> origins)
                {
                    origins[mode.ModeParam.Index] = origin;

                    foreach (var arg in mode.Arguments)
                    {
                        // Might need to pull from alterate for target
                        if (!arg.Param.IsSet(address, checkAlternate: origin == ParamOrigin.Target))
                        {
                            continue;
                        }

                        ref var originRef = ref origins[arg.Param.Index];
                        if (originRef == ParamOrigin.Unset)
                        {
                            originRef = origin;
                        }
                    }
                }

                selectOrigins(GetMode(source.leftPaneMode), GetMode(target.leftPaneMode), origins);
                selectOrigins(GetMode(source.rightPaneMode), GetMode(target.rightPaneMode), origins);

                // Get fields set on target
                foreach (var param in Params)
                {
                    if (param.IsSet(target, checkAlternate: false))
                    {
                        targetPriorFields[param.Index] = true;
                    }
                }

                // First pass for only target origin values
                foreach (var param in Params)
                {
                    var origin = origins[param.Index];
                    if (origin != ParamOrigin.Target) continue;

                    // Pull from alternate field if needed
                    param.Apply(target, target, checkAlternate: true);
                }

                // Second pass to apply source 
                foreach (var param in Params)
                {
                    var origin = origins[param.Index];
                    if (origin != ParamOrigin.Source) continue;

                    // Don't pull from alternate to overwrite target fields
                    param.Apply(source, target, checkAlternate: false);
                }

                // Simplify away added fields if possible
                foreach (var param in Params)
                {
                    var origin = origins[param.Index];

                    if (!targetPriorFields[param.Index])
                    {
                        param.Simplify(target);
                    }
                    else if (origin == ParamOrigin.Unset)
                    {
                        // Remove params with no origin
                        param.Clear(target);
                    }
                }
            }

            public async Task Navigate(ViewModelAddress address, MainController controller, InferMode infer)
            {
                InferModeOrValidate(address, infer);

                foreach (var mode in Modes)
                {
                    if (mode.IsApplicable(address))
                    {
                        await mode.Navigate(controller, address);
                    }
                }
            }

            public void AppendParams(Dictionary<string, string> queryParams, ViewModelAddress address)
            {
                foreach (var param in Params)
                {
                    param.AppendParam(queryParams, address);
                }
            }

            public void Parse(Dictionary<string, string> queryParams, ViewModelAddress address)
            {
                foreach (var param in Params)
                {
                    param.Parse(queryParams, address);
                }

                InferModeOrValidate(address, infer: false);
            }

            public bool IsEmpty(ViewModelAddress address)
            {
                foreach (var param in Params)
                {
                    if (param.IsSet(address, checkAlternate: false))
                    {
                        return false;
                    }
                }

                return true;
            }

            public void InferModeOrValidate(ViewModelAddress address, InferMode infer)
            {
                bool shouldInfer = infer.Enabled;
                if (shouldInfer
                    && infer.OnlyIfNoModeSpecified
                    && (RightModeParam.IsSet(address) || LeftModeParam.IsSet(address)))
                {
                    shouldInfer = false;
                }

                foreach (var mode in Modes)
                {
                    mode.InferOrValidate(address, shouldInfer);
                }

                if (infer.IsStartup)
                {
                    if (!RightModeParam.IsSet(address))
                    {
                        address.rightPaneMode = RightPaneMode.overview;
                    }

                    if (!LeftModeParam.IsSet(address))
                    {
                        address.leftPaneMode = LeftPaneMode.search;
                        address.searchText = "";
                    }
                }
            }

            private Param<T> CreateParam<T>(FieldFunc<T> field, string name = null, Func<T> alternateField = null,
                [CallerArgumentExpression(nameof(field))] string fieldExpression = null)
            {
                name ??= fieldExpression.AsSpan().SubstringAfterLastIndexOfAny(".").ToString();

                var param = new Param<T>()
                {
                    Name = name,
                    FieldFunc = field,
                    Index = Params.Count,
                };

                Params.Add(param);
                return param;
            }

            private Mode CreateMode<TMode>(TMode modeValue, Param<TMode> modeParam, Func<MainController, ViewModelAddress, Task> navigate, params ModeArgument[] arguments)
                where TMode : unmanaged, Enum
            {
                var valueAsInt = AsInteger(modeValue);

                var requiredParameters = arguments.Where(a => a.IsRequired).Select(a => a.Param).ToArray();

                bool isApplicable(ViewModelAddress address, bool forceCheck)
                {
                    var paramAsInt = AsInteger(modeParam.FieldFunc(address));
                    if (forceCheck || paramAsInt == valueAsInt)
                    {
                        foreach (var arg in requiredParameters)
                        {
                            if (!arg.IsSet(address, checkAlternate: true))
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                var canInfer = new Box<bool>() { Value = true };
                var mode = new Mode<TMode>()
                {
                    ModeParam = modeParam,
                    Arguments = arguments,
                    Value = modeValue,
                    Navigate = navigate,
                    CanInfer = canInfer,
                    IsSet = (address) =>
                    {
                        var addressMode = modeParam.FieldFunc(address);
                        return EqualityComparer<TMode>.Default.Equals(modeValue, addressMode);
                    },
                    ClearParams = (address) =>
                    {
                        foreach (var param in requiredParameters)
                        {
                            param.Clear(address);
                        }
                    },
                    InferOrValidate = (address, infer) =>
                    {
                        infer &= canInfer.Value;
                        var paramAsInt = AsInteger(modeParam.FieldFunc(address));
                        if ((infer && paramAsInt == 0) || paramAsInt == valueAsInt)
                        {
                            if (isApplicable(address, forceCheck: paramAsInt == 0))
                            {
                                foreach (var param in requiredParameters)
                                {
                                    param.EnsureSet(address);
                                }

                                modeParam.FieldFunc(address) = modeValue;
                            }
                            else if (paramAsInt == valueAsInt)
                            {
                                modeParam.FieldFunc(address) = default;
                            }
                        }
                    },
                    IsApplicable = address =>
                    {
                        return isApplicable(address, forceCheck: false);
                    }
                };

                Modes.Add(mode);
                return mode;
            }

            public static long AsInteger<TEnum>(TEnum enumValue)
                where TEnum : unmanaged, Enum
            {
                long value;
                if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<byte>()) value = Unsafe.As<TEnum, byte>(ref enumValue);
                else if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<short>()) value = Unsafe.As<TEnum, short>(ref enumValue);
                else if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<int>()) value = Unsafe.As<TEnum, int>(ref enumValue);
                else if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<long>()) value = Unsafe.As<TEnum, long>(ref enumValue);
                else throw new Exception("type mismatch");
                return value;
            }

            private static void Alternate<T>(Param<T> p1, Param<T> p2)
            {
                p1.Alternate = p2;
                p2.Alternate = p1;
            }

            public record ModeArgument(Param Param, bool IsRequired)
            {
                public static implicit operator ModeArgument(Param param)
                {
                    return new ModeArgument(param, IsRequired: true);
                }
            }

            public abstract class Param
            {
                public string Name { get; init; }
                public int Index { get; init; }

                public abstract void Apply(ViewModelAddress source, ViewModelAddress target, bool checkAlternate);

                public abstract void Simplify(ViewModelAddress address);

                public abstract void Parse(Dictionary<string, string> queryParams, ViewModelAddress address);

                public abstract void AppendParam(Dictionary<string, string> queryParams, ViewModelAddress address);

                public abstract bool IsSet(ViewModelAddress address, bool checkAlternate);

                public abstract void EnsureSet(ViewModelAddress address);

                public abstract void Clear(ViewModelAddress address);

                public override string ToString()
                {
                    return Name;
                }
            }

            private class Param<T> : Param
            {
                public Param<T> Alternate { get; set; }

                public FieldFunc<T> FieldFunc { get; init; }

                public ModeArgument Optional()
                {
                    return new ModeArgument(this, IsRequired: false);
                }

                public override void Simplify(ViewModelAddress address)
                {
                    if (IsSet(address, checkAlternate: false) && Alternate != null && Alternate.IsSet(address, checkAlternate: false))
                    {
                        if (EqualityComparer<T>.Default.Equals(FieldFunc(address), Alternate.FieldFunc(address)))
                        {
                            Clear(address);
                        }
                    }
                }

                public override void Apply(ViewModelAddress source, ViewModelAddress target, bool checkAlternate)
                {
                    if (IsSet(source, checkAlternate: checkAlternate))
                    {
                        var sourceField = IsSet(source, checkAlternate: false) ? FieldFunc(source) : Alternate.FieldFunc(source);
                        ref var targetField = ref FieldFunc(target);
                        targetField = sourceField;
                    }
                }

                public override void Parse(Dictionary<string, string> queryParams, ViewModelAddress address)
                {
                    if (queryParams.TryGetValue(Name, out var value))
                    {
                        ref T field = ref FieldFunc(address);
                        field = Converter<T>.Convert(value);
                    }
                }

                public override void AppendParam(Dictionary<string, string> queryParams, ViewModelAddress address)
                {
                    if (!IsSet(address, checkAlternate: false))
                    {
                        return;
                    }

                    var paramValue = FieldFunc(address);
                    var value = Converter<T>.ToQueryString is { } toQueryString
                        ? toQueryString(paramValue)
                        : paramValue?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (Alternate == null
                            || !queryParams.TryGetValue(Alternate.Name, out var alternateValue)
                            || alternateValue != value)
                        {
                            queryParams[Name] = value;
                        }
                    }
                }

                public override bool IsSet(ViewModelAddress address, bool checkAlternate = false)
                {
                    return !EqualityComparer<T>.Default.Equals(default, FieldFunc(address)) || (checkAlternate && (Alternate?.IsSet(address, false) == true));
                }

                public override void Clear(ViewModelAddress address)
                {
                    FieldFunc(address) = default;
                }

                public override void EnsureSet(ViewModelAddress address)
                {
                    if (EqualityComparer<T>.Default.Equals(default, FieldFunc(address)))
                    {
                        FieldFunc(address) = Alternate.FieldFunc(address);
                    }
                }
            }

            public class Mode
            {
                public Param ModeParam { get; init; }
                public Func<MainController, ViewModelAddress, Task> Navigate { get; init; }
                public Action<ViewModelAddress, bool> InferOrValidate { get; init; }
                public Func<ViewModelAddress, bool> IsApplicable { get; init; }
                public Func<ViewModelAddress, bool> IsSet { get; init; }
                public Action<ViewModelAddress> ClearParams { get; init; }

                public ModeArgument[] Arguments { get; init; }

                public Box<bool> CanInfer { get; init; }
            }

            public class Mode<TMode> : Mode
            {
                public TMode Value { get; init; }
            }
        }
    }

    public enum RightPaneMode
    {
        unspecified,
        overview,
        file,
        symbol,
        repossummary,
        referenceprojectsxml,
        referencesymbolsxml
    }

    public enum OverviewKind
    {
        home,
        help
    }

    public enum LeftPaneMode
    {
        unspecified,
        search,
        outline,
        project,
        references,
        namespaces
    }
}
