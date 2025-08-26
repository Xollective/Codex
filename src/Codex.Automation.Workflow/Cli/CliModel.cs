using System.Collections;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.IO;
using System.CommandLine.Parsing;
using System.Runtime.CompilerServices;
using Azure.Storage.Blobs.Models;
using Codex.Sdk;
using Codex.Utilities;
using CommunityToolkit.HighPerformance;
using static Codex.Cli.CliModel;

namespace Codex.Cli;

public record struct CliAlias(string Alias);

public interface IInitializedCliArgument
{
    public void PostSetInitialize();
}

public static class CliModel
{
    [CollectionBuilder(typeof(AliasList), nameof(Create))]
    public struct AliasList(params string[] aliases) : IEnumerable<string>
    {
        public string[] Aliases => aliases ?? [];

        public static implicit operator AliasList(string alias) => new([alias]);
        public static implicit operator AliasList(char alias) => new([alias.ToString()]);
        public static implicit operator AliasList(string[] aliases) => new(aliases);

        public static AliasList Create(ReadOnlySpan<string> aliases) => aliases.ToArray();

        public IEnumerator<string> GetEnumerator() => Aliases.AsEnumerable().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static void Add(this IdentifierSymbol command, CliAlias alias)
    {
        command.AddAlias(alias.Alias);
    }

    public static void Add(this Command command, IEnumerable<Option> options)
    {
        foreach (var option in options)
        {
            command.Add(option);
        }
    }

    public static CliModel<T> Bind<T>(Command command, Func<CliModel<T>, T> getOptions, Func<T, Task<int>> runAsync, ICliModel<T>? parent = null)
    {
        var model = new CliModel<T>(command, (model, context) =>
        {
            var target = getOptions(model);
            model.Apply(target, context);
            return target;
        });

        getOptions(model);

        // Disable options mode so that real target values get created in handler
        model.OptionsMode = false;

        if (runAsync != null)
        {
            command.SetHandler(async context =>
            {
                model.Console = context.Console ?? model.Console;
                var target = model.Create(context);
                context.ExitCode = await runAsync(target);
            });
        }

        return model;
    }
}

public interface ICliModel<in T>
{
    Command Command { get; }

    void Apply(T target, InvocationContext context);
}

public record CliModel<T>(Command Command, Func<CliModel<T>, InvocationContext, T> CreateValue) : ICliModel<T>
{
    public bool OptionsMode { get; set; } = true;
    public IConsole Console { get; set; } = new SystemConsole();
    public CancellationToken Token { get; set; } = default;

    private List<Action<T, InvocationContext>> SetFields { get; } = new();

    public void AddHandler(Action<T> handler)
    {
        if (OptionsMode)
        {
            SetFields.Add((m, c) => handler(m));
        }
    }

    public void Add(ICliModel<T> model)
    {
        if (OptionsMode)
        {
            SetFields.Add(model.Apply);
        }
    }

    public void Apply(T target, InvocationContext context)
    {
        foreach (var item in SetFields)
        {
            item(target, context);
        }
    }

    public T Create(InvocationContext context)
    {
        Token = context.GetCancellationToken();
        return CreateValue(this, context);
    }

    public static implicit operator Command(CliModel<T> model) => model.Command;

    public TField SharedOptions<TField>(RefFunc<T, TField> getFieldRef, CliModel<TField> sharedModel)
    {
        if (OptionsMode)
        {
            SetFields.Add((model, context) =>
            {
                var value = sharedModel.Create(context);
                getFieldRef(model) = value;
            });

            foreach (var option in sharedModel.Command.Options)
            {
                Command.Add(option);
            }
        }

        return default!;
    }

    public TField Option<TField>(
        RefFunc<T, TField> getFieldRef,
        string name,
        string? description = null,
        bool required = false,
        Optional<TField> defaultValue = default,
        bool isHidden = false,
        RefFunc<T, bool>? isExplicitRef = null,
        ParseArgument<TField>? parse = null,
        Func<TField, TField> transform = null,
        AliasList aliases = default,
        Action<T, TField>? init = null)
    {
        string processName(string name)
        {
            name = name.Trim().TrimStart("-").Trim();
            return name.Length == 1 ? $"-{name}" : $"--{name}";
        }

        if (OptionsMode)
        {
            name = processName(name);

            var option = parse != null
                ? new Option<TField>(name, parseArgument: argResult =>
                    {
                        if (defaultValue.HasValue)
                        {

                        }

                        return parse!(argResult);
                    },
                    isDefault: defaultValue.HasValue,
                    description: description)
                : defaultValue.HasValue
                ? new Option<TField>(name, getDefaultValue: () => defaultValue.Value!, description: description)
                : new Option<TField>(name, description: description);

            option.IsRequired = required;
            option.IsHidden = isHidden;
            option.AllowMultipleArgumentsPerToken = true;

            foreach (var alias in aliases)
            {
                option.AddAlias(processName(alias));
            }

            SetFields.Add((model, context) =>
            {
                var result = context.ParseResult.FindResultFor(option);
                if (result != null)
                {
                    var value = context.ParseResult.GetValueForOption(option)!;
                    if (transform != null)
                    {
                        value = transform(value);
                    }

                    getFieldRef(model) = value;

                    init?.Invoke(model, value);

                    if (isExplicitRef != null)
                    {
                        isExplicitRef(model) = !result.IsImplicit;
                    }
                }
            });

            Command.AddOption(option);
        }

        return default!;
    }

    public TField Argument<TField>(
        RefFunc<T, TField> getFieldRef,
        string name,
        string? description = null,
        ArgumentArity? arity = null,
        Optional<TField> defaultValue = default,
        bool isHidden = false,
        RefFunc<T, bool>? isExplicitRef = null,
        ParseArgument<TField>? parse = null)
    {
        if (OptionsMode)
        {
            var option = parse != null
                ? new Argument<TField>(name, parse: argResult =>
                    {
                        if (defaultValue.HasValue)
                        {

                        }

                        return parse!(argResult);
                    },
                    isDefault: defaultValue.HasValue,
                    description: description)
                : defaultValue.HasValue
                ? new Argument<TField>(name, getDefaultValue: () => defaultValue.Value!, description: description)
                : new Argument<TField>(name, description: description);

            option.Arity = arity ?? ArgumentArity.ExactlyOne;
            option.IsHidden = isHidden;

            SetFields.Add((model, context) =>
            {
                var result = context.ParseResult.FindResultFor(option);
                if (result != null)
                {
                    getFieldRef(model) = context.ParseResult.GetValueForArgument(option)!;

                    if (isExplicitRef != null)
                    {
                        isExplicitRef(model) = result.Children.Count != 0;
                    }
                }
            });

            Command.Add(option);
        }

        return default!;
    }
}
