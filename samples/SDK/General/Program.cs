using Azure.Core;
using Azure.Identity;
using CloudWorker.Client.SDK;

namespace General;

class Program
{
    static private TokenCredential credential = new DefaultAzureCredential();

    async Task CreateNewSessionAndDestroyItOnExit()
    {
        var config = new SessionConfig() { /* settings */ };
        var session = await Session.CreateOrUpdateAsync(credential, config);

        //send
        //var sender = session.CreateSender();
        //...

        //receive
        //var receiver = session.CreateReceiver();
        //...

        await Session.DestroyAsync(credential, session.Id);
    }

    async Task CreateNewSessionButDoNotDestroyItOnExit()
    {
        var config = new SessionConfig() { /* settings */ };
        var session = await Session.CreateOrUpdateAsync(credential, config);

        //send only
        //var sender = session.CreateSender();
        //...
    }

    async Task UseExistingSession()
    {
        var id = "existing session id";
        var session = await Session.GetAsync(credential, id);

        //send
        //var sender = session.CreateSender();
        //...

        //receive
        //var receiver = session.CreateReceiver();
        //...
    }

    async Task UpdateSession()
    {
        var id = "existing session id";
        var config = new SessionConfig() { /* settings */ };
        var session = await Session.CreateOrUpdateAsync(credential, config, id);
    }

    async Task DestroySession()
    {
        var id = "existing session id";
        await Session.DestroyAsync(credential, id);
    }

    static void Main(string[] args)
    {
    }
}
