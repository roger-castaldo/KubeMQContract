using KubeMQ.Contract;
using System.Net.NetworkInformation;

var sourceCancel = new CancellationTokenSource();

var opts = new ConnectionOptions()
{ };

var conn = opts.EstablishConnection();

var ping = conn.Ping();

Console.WriteLine(@$"Ping Results: 
Host: {ping.Host}
Version: {ping.Version}
Server Start Time: {ping.ServerStartTime}
Server Up Time: {ping.ServerUpTime}
");