#r "nuget: SmtpServer, 11.0.0"
#r "nuget: MimeKit, 4.12.0"
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#nullable enable
using System.Buffers;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Lestaly;
using Kokuban;
using SmtpServer;
using SmtpServer.Protocol;
using SmtpServer.Storage;
using SmtpServerServiceProvider = SmtpServer.ComponentModel.ServiceProvider;

// This script is meant to run with dotnet-script.
// Install .NET8 and run `dotnet tool install -g dotnet-script`

// Receive and dump mail.

await Paved.RunAsync(async () =>
{
    using var outenc = ConsoleWig.OutputEncodingPeriod(Encoding.UTF8);
    using var signal = new SignalCancellationPeriod();

    // Service info
    var hostName = Environment.GetEnvironmentVariable("MAIL_HOST") ?? "localhost";
    var portNumber = Environment.GetEnvironmentVariable("MAIL_PORT")?.TryParseNumber<ushort>() ?? 25;
    var dumpDir = ThisSource.RelativeDirectory(Environment.GetEnvironmentVariable("MAIL_DUMP_DIR") ?? "/var/maildump");

    // Display server information.
    // This has already been configured in the included container, so no additional configuration should be required.
    WriteLine($"Server name : {hostName}");
    WriteLine($"Server port : {portNumber}");
    WriteLine($"Dump dir    : {dumpDir.FullName}");
    WriteLine();

    // Configure server options.
    var options = new SmtpServerOptionsBuilder()
        .ServerName(hostName)
        .Endpoint(builder => builder.Endpoint(new IPEndPoint(IPAddress.Any, portNumber)))
        .Build();

    // Prepare service providers.
    var provider = new SmtpServerServiceProvider();
    provider.Add(new FileMessageStore(dumpDir));

    // Start HTTP Server
    WriteLine($"Start mail receiver.");
    var server = new SmtpServer.SmtpServer(options, provider);
    await server.StartAsync(signal.Token);
});

class FileMessageStore(DirectoryInfo dumpDir) : MessageStore
{
    public override async Task<SmtpResponse> SaveAsync(ISessionContext context, IMessageTransaction transaction, ReadOnlySequence<byte> buffer, CancellationToken cancellationToken)
    {
        var timestamp = DateTime.Now;
        WriteLine($"  {timestamp:yyyyMMdd_HHmmss.fff}: To={transaction.To.Select(t => $"{t.User}@{t.Host}").JoinString(", ")}");
        try
        {
            // Copy the entire message into the memory stream.
            // This is to match the MimeKit I/F used for decoding.
            using var rawMsg = new MemoryStream();

            // Save the entire message to a file.
            var rawFile = dumpDir.RelativeFile($"{timestamp:yyyyMMdd_HHmmss.fff}.txt");
            using var rawWriter = rawFile.OpenWrite();

            // Write to memory&file stream
            foreach (var memory in buffer)
            {
                await Task.WhenAll(
                    rawMsg.WriteAsync(memory, cancellationToken).AsTask(),
                    rawWriter.WriteAsync(memory, cancellationToken).AsTask()
                );
            }

            // Decode the message and save the text to a file.
            rawMsg.Position = 0;
            var decodedMsg = await MimeKit.MimeMessage.LoadAsync(rawMsg);
            var decodedText = decodedMsg.TextBody;
            if (decodedText.IsNotWhite())
            {
                var textFile = dumpDir.RelativeFile($"{timestamp:yyyyMMdd_HHmmss.fff}-text.txt");
                await textFile.WriteAllTextAsync(decodedText, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$"    Failed to store. Err={ex.Message}"]);
        }

        return SmtpResponse.Ok;
    }
}
