using Codex.ObjectModel;

namespace Codex.Sdk.Search
{
    public record ArgumentsModifierCodex(ICodex BaseCodex, Action<ContextCodexArgumentsBase> ModifyArgs) 
        : CodexWrapper(BaseCodex)
    {
        protected override void ModifyArguments(ContextCodexArgumentsBase arguments)
        {
            ModifyArgs(arguments);
        }
    }

    public static class ArgumentsModifierCodexExtensions
    {
        public static ICodex ScopeToRepo(this ICodex codex, string repo)
        {
            return new ArgumentsModifierCodex(codex, args =>
            {
                args.RepositoryScopeId = repo;
            });
        }

        public static ICodex Apply(this ICodex codex, string repo, RepoAccess? access, bool forceAccessReposSummary = false)
        {
            return new ArgumentsModifierCodex(codex, args =>
            {
                if (repo != null)
                {
                    args.RepositoryScopeId = repo;
                }

                if (args is GetRepositoryHeadsArguments && !forceAccessReposSummary)
                {
                    args.AccessLevel ??= access;
                }
                else
                {
                    args.AccessLevel = access;
                }
            });
        }
    }
}
