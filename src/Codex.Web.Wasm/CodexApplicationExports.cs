using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Codex.Utilities;
using Codex.View;
using Codex.Web.Common;

public partial class CodexApplicationExports
{
    [JSExport]
    internal static void Message(string message)
    {
        Console.WriteLine($"Received message {message}");
    }

    [JSExport]
    internal static async Task<string> UpdateState(string pageRequestJson)
    {
        try
        {
            Console.WriteLine("Started UpdateState");
            await TaskEx.Yield();
            var request = pageRequestJson.DeserializeEntity<PageRequest>();
            bool log = false;
            var result = await request.NavigateAsync(MainController.App, log: log);
            return result.SerializeEntity();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception:\n{ex}");
            throw;
        }
        finally
        {
            Console.WriteLine("Finished UpdateState");
        }
    }
}
