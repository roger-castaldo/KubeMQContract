﻿using KubeMQ.Contract;
using Messages;

var sourceCancel = new CancellationTokenSource();

var opts = new ConnectionOptions()
{};

var conn = opts.EstablishRPCQueryConnection();

//Add listener
var listener = conn.SubscribeRPCQuery<Hello2, Greeting>(
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

var response1 = await conn.SendRPCQuery<Hello,Greeting>(new Hello()
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

conn.Unsubscribe(listener);

var commandConn = opts.EstablishRPCCommandConnection();

commandConn.SubscribeRPCCommand<Hello2>(
    message =>
    {
        Console.WriteLine($"Greetings {message.Data.Salutation} {message.Data.FirstName} {message.Data.LastName}");
        return new TaggedResponse<bool>()
        {
            Response=true,
            Tags = new Dictionary<string, string>()
            {
                {"Salutation",message.Data.Salutation }
            }
        };
    },
    error =>
    {
        Console.WriteLine(error.Message);
    }
);

var response3 = await commandConn.SendRPCCommand<Hello>(new Hello()
{
    FirstName="Bob",
    LastName="Loblaw"
});

var response4 = await commandConn.SendRPCCommand<Hello2>(new Hello2()
{
    Salutation="Mr.",
    FirstName="Bob",
    LastName="Loblaw"
});

Console.WriteLine($"Response 3 : {response3.Response}");
Console.WriteLine("Tags:");
foreach (var key in response3.Keys)
    Console.WriteLine($"{key}={response3[key]}");
Console.WriteLine($"Response 4 : {response4.Response}");
Console.WriteLine("Tags:");
foreach (var key in response4.Keys)
    Console.WriteLine($"{key}={response4[key]}");