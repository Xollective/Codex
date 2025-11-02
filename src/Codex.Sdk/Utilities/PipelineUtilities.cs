namespace Codex.Utilities;

public static class PipelineUtilities
{
    public static string SetPipelineVariable(string name, string value, bool isSecret, bool isOutput = false)
    {
        return PrintAndReturn(AzureDevOps.GetSetPipelineVariableText(name, value, isSecret, isOutput), print: true);
    }

    private static string PrintAndReturn(string value, bool print)
    {
        if (print) Console.WriteLine(value);
        return value;
    }

    public static void TryLogProgressCommand(byte progress, string message = "")
    {
        Console.WriteLine($"##vso[task.setprogress value={progress};]{message}");
    }

    public static class AzureDevOps
    {
        public static string GetSetPipelineVariableText(string name, string value, bool isSecret, bool isOutput = false)
        {
            string additionalArgs = "";
            if (isSecret)
            {
                additionalArgs += "issecret=true;";
            }

            if (isOutput)
            {
                additionalArgs += "isOutput=true;";
            }

            return $"##vso[task.setvariable variable={name};{additionalArgs}]{value}";
        }

        public static string AddBuildTag(string tag, bool print = true)
        {
            return PrintAndReturn($"##vso[build.addbuildtag]{tag}", print: print);
        }

        public static string TryGetBuildUrl()
        {
            if (MiscUtilities.TryGetEnvironmentVariable("SYSTEM_COLLECTIONURI", out var orgUriString)
                && MiscUtilities.TryGetEnvironmentVariable("SYSTEM_TEAMPROJECT", out var project)
                && Uri.TryCreate(orgUriString, UriKind.Absolute, out var orgUri) 
                && MiscUtilities.TryGetEnvironmentVariable("BUILD_BUILDID", out var buildIdString)
                && BuildUri.ParseId(buildIdString) is int buildId)
            {
                return new BuildUri(orgUri, project) { BuildId = buildId }.GetBuildWebUrl();
            }

            return null;
        }
    }
}