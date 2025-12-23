using idox.eim.fusionp8;
using Newtonsoft.Json;
using System.Collections.Generic;

public static class JobTicketFactory
{
    public static string CreatePerPageTicket(
        string originalJobTicketPath,
        string singlePageInputPath,
        string expectedOutputPath,
        string ticketsFolder)
    {
        // Load original ticket template
        var json = System.IO.File.ReadAllText(originalJobTicketPath);
        var ticket = JsonConvert.DeserializeObject<JobTicket>(json);

        // Replace input with the single page input
        ticket.InputFiles = new List<string> { singlePageInputPath };

        // The OUTPUT FILE PATH MUST USE ExpectedOutputPath
        ticket.OutputFilePath = expectedOutputPath;

        var newTicketJson = JsonConvert.SerializeObject(ticket, Formatting.Indented);

        var ticketFileName = System.IO.Path.Combine(
            ticketsFolder,
            System.IO.Path.GetFileNameWithoutExtension(singlePageInputPath) + "-ticket.json");

        System.IO.File.WriteAllText(ticketFileName, newTicketJson);

        return ticketFileName;
    }
}
