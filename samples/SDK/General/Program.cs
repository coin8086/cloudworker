using CloudWorker.Client.SDK;

namespace General;

class Program
{
    async Task CreateNewSessionAndDestroyItOnExit()
    {
        var config = new ClusterConfig() { /* settings */ };
        var session = await Session.CreateAsync(config);

        //send
        //var sender = session.CreateSender();
        //...

        //receive
        //var receiver = session.CreateReceiver();
        //...

        await session.DestroyAsync();
    }

    async Task CreateNewSessionButDoNotDestroyItOnExit()
    {
        var config = new ClusterConfig() { /* settings */ };
        var session = await Session.CreateAsync(config);

        //send only
        //var sender = session.CreateSender();
        //...
    }

    async Task UseExistingSession()
    {
        var id = "session id";
        var session = await Session.GetAsync(id);

        //send
        //var sender = session.CreateSender();
        //...

        //receive
        //var receiver = session.CreateReceiver();
        //...
    }

    async Task DestroySession()
    {
        var id = "session id";
        await Session.DestroyAsync(id);
    }

    static void Main(string[] args)
    {
    }
}
