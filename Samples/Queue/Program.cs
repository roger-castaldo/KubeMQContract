using KubeMQ.Contract;
using Messages;

var opts = new ConnectionOptions()
{
    Logger=new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider()
};

var conn = opts.EstablishQueueConnection();

//create listening queue
var queue = conn.SubscribeToQueue<Hello2>();

var result1 = await conn.EnqueueMessage<Hello>(new Hello()
{
    FirstName="Bob",
    LastName="Loblaw"
});

var result2 = await conn.EnqueueMessage<Hello2>(new Hello2()
{
    FirstName="Bob",
    LastName="Loblaw",
    Salutation="Mr."
});

Console.WriteLine($"Result 1 is Error: {result1.IsError}");
Console.WriteLine($"Result 2 is Error: {result2.IsError}");

Console.WriteLine($"Queue has data: {queue.HasMore}");

if (queue.HasMore)
{
    var message = queue.Peek();
    if (message!=null)
        Console.WriteLine($"Greetings {message.Data.Salutation} {message.Data.FirstName} {message.Data.LastName}");
    else
        Console.WriteLine("Peek failed");
}

while (queue.HasMore)
{
    var message = queue.Pop();
    if (message!=null)
        Console.WriteLine($"Greetings {message.Data.Salutation} {message.Data.FirstName} {message.Data.LastName}");
    else
        Console.WriteLine("Pop failed");
}

//enqueue multiple messages
var result3 = await conn.EnqueueMessages<Hello>(new Hello[]
{
    new Hello()
    {
        FirstName="Bob",
        LastName="Loblaw"
    },
    new Hello()
    {
        FirstName="Fred",
        LastName="Flinestone"
    }
});

Console.WriteLine($"Result 3 is Error: {result3.IsError}");

foreach (var result in result3.Results)
    Console.WriteLine($"Result in queue Is Error: {result.IsError}");

while (queue.HasMore)
{
    var message = queue.Pop();
    if (message!=null)
        Console.WriteLine($"Greetings {message.Data.Salutation} {message.Data.FirstName} {message.Data.LastName}");
    else
        Console.WriteLine("Pop failed");
}

conn.Unsubscribe(queue.ID);