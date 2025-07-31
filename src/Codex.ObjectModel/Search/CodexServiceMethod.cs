namespace Codex.Sdk.Search
{
    public enum CodexServiceMethod
    {
        Search,
        FindAllRefs,
        FindDef,
        FindDefLocation,
        GetSource,
        GetProject,
        GetRepoHeads,
    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class SearchMethodAttribute : Attribute
    {
        public SearchMethodAttribute(CodexServiceMethod method)
        {
            Method = method;
        }

        public CodexServiceMethod Method { get; }

        public bool DisablePost { get; set; }
    }
}
