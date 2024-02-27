using Encrypting;
using KubeMQ.Contract;
using Messages;

var sourceCancel = new CancellationTokenSource();

var opts = new ConnectionOptions()
{ };

var conn = opts.EstablishRPCQueryConnection(globalMessageEncryptor:new GlobalEncryptor());

//Add listener for Query
var listener = conn.SubscribeToRPCQuery<Hello2, Greeting>(
    message =>
    {
        return new TaggedResponse<Greeting>()
        {
            Response=new Greeting()
            {
                Message=$"Greetings {message.Data.Salutation} {message.Data.FirstName} {message.Data.LastName}"
            }
        };
    },
    error =>
    {
        Console.WriteLine(error.Message);
    }
);

//Send Query calls
var response1 = await conn.SendRPCQuery<Hello, Greeting>(new Hello()
{
    FirstName="Bob",
    LastName="Loblaw"
});

var response2 = await conn.SendRPCQuery<Hello2, Greeting>(new Hello2()
{
    Salutation="Mr.",
    FirstName="Bob",
    LastName="Loblaw"
});

if (response1.Response!=null)
    Console.WriteLine($"Response 1: {response1.Response.Message}");
else
    Console.WriteLine($"Response 1 Error: {response1.Error}");

if (response2.Response!=null)
    Console.WriteLine($"Response 2: {response2.Response.Message}");
else
    Console.WriteLine($"Response 2 Error: {response2.Error}");