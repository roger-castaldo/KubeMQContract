using Encoding;
using KubeMQ.Contract;
using Messages;

var sourceCancel = new CancellationTokenSource();

var opts = new ConnectionOptions()
{ };

//Create subscriber
var conn = opts.EstablishPubSubConnection();

var listener = conn.Subscribe<Hello2>(message =>
    {
        Console.WriteLine($"Greetings {message.Data.Salutation} {message.Data.FirstName} {message.Data.LastName}");
    },
    error =>
    {
        Console.WriteLine(error.Message);
    }
);

var result1 = await conn.Send<Hello>(new Hello()
{
    FirstName="Bob",
    LastName="Loblaw"
});

var result2 = await conn.Send<Hello2>(new Hello2()
{
    FirstName="Bob",
    LastName="Loblaw",
    Salutation="Mr."
});

Console.WriteLine($"Result 1 is Error: {result1.IsError}");
Console.WriteLine($"Result 2 is Error: {result2.IsError}");

conn.Unsubscribe(listener);

//Create subscriber to listen to HelloProto (protobuf encoded message) channel and convert to Hello2 for logging
listener = conn.Subscribe<Hello2>(message =>
    {
        Console.WriteLine($"Greetings {message.Data.Salutation} {message.Data.FirstName} {message.Data.LastName}");
    },
    error =>
    {
        Console.WriteLine(error.Message);
    },
    channel: "Greeting.Proto"
);

var result3 = await conn.Send<HelloProto>(new HelloProto()
{
    FirstName="Bob",
    LastName="Loblaw"
});

var result4 = await conn.Send<Hello2>(new Hello2()
{
    FirstName="Bob",
    LastName="Loblaw",
    Salutation="Mr."
},channel:"Greeting.Proto");

Console.WriteLine($"Result 3 is Error: {result3.IsError}");
Console.WriteLine($"Result 4 is Error: {result4.IsError}");

Console.ReadLine();
conn.Unsubscribe(listener);