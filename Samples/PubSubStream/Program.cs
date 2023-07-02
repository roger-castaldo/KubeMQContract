using KubeMQ.Contract;
using Messages;


var sourceCancel = new CancellationTokenSource();

var opts = new ConnectionOptions()
{ };

var connStream = opts.EstablishPubSubStreamConnection();

var readStream = connStream.SubscribeToStream<Hello2>(error =>
{
    Console.WriteLine(error.Message);
});

var writeStream = connStream.CreateStream<Hello>();

var result5 = await writeStream.Write(new Hello()
{
    FirstName="Bob",
    LastName="Loblaw"
});

var result6 = await writeStream.Write(new Hello()
{
    FirstName="Fred",
    LastName="Flinestone"
});

Console.WriteLine($"Result 5 is Error: {result5.IsError}");
Console.WriteLine($"Result 6 is Error: {result6.IsError}");
Console.WriteLine($"Write Stream Stats: Success: {writeStream.Stats.Success}, Errors: {writeStream.Stats.Errors}, Length: {writeStream.Length}");

var cnt = 0;

await foreach (var message in readStream)
{
    Console.WriteLine($"Greetings {message.Data.Salutation} {message.Data.FirstName} {message.Data.LastName}");
    cnt++;
    if (cnt==2)
        break;
}

Console.WriteLine($"Read Stream Stats: Success: {readStream.Stats.Success}, Errors: {readStream.Stats.Errors}, Length: {readStream.Length}");

