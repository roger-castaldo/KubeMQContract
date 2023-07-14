using KubeMQ.Contract;

var opts = new ConnectionOptions()
{
    Logger=new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider()
};

var conn = opts.EstablishConnection();

var ping = conn.Ping();

Console.WriteLine(@$"Ping Results: 
Host: {ping?.Host}
Version: {ping?.Version}
Server Start Time: {ping?.ServerStartTime}
Server Up Time: {ping?.ServerUpTime}
");