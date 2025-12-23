using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using idox.eim.fusionp8;

namespace FfcTestApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: FfcTestApp.exe <jobTicketPath> [queueName]");
                return;
            }

            idox.eim.fusionp8.FfcApiHelper.ApplyLicense();

            string jobTicketPath = args[0];
            string queueName = args.Length > 1 ? args[1] : null;

            Console.WriteLine($"Job Ticket: {jobTicketPath}");

            string ffcApiUrl = "http://localhost:9000";
            string username = "sysadmin";
            string password = "nimbus";
            int maxParallelJobs = 5;

            try
            {
                
                List<FfcClientWrapper> ffcClients = new List<FfcClientWrapper>();

                ffcClients.Add(new FfcClientWrapper() { FfcUri = "http://dev-fusp8-01.idoxgroup.local:9000/", FfcUsername = "sysadmin", FfcPassword = "nimbus", });
                //ffcClients.Add(new FfcClientWrapper() { FfcUri = "http://sup-fusp8-03.idoxgroup.local:9000/", FfcUsername = "sysadmin", FfcPassword = "nimbus", });
                //ffcClients.Add(new FfcClientWrapper() { FfcUri = "http://sup-fusp8-04.idoxgroup.local:9000/", FfcUsername = "sysadmin", FfcPassword = "nimbus", });
                //ffcClients.Add(new FfcClientWrapper() { FfcUri = "http://qa-fusp8-05.idoxgroup.local:9000/", FfcUsername = "sysadmin", FfcPassword = "nimbus", });

                var httpClient = new HttpClient 
                { BaseAddress = new Uri(ffcApiUrl) };
                var orchestrator = new LargePdfOrchestrator(ffcClients, queueName, maxParallelJobs, 300);

                string result = await orchestrator.ProcessLargePdfAsync(jobTicketPath,new System.Threading.CancellationToken());


                Console.WriteLine($"Processing completed successfully!");
                Console.WriteLine($"Output file: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing PDF: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
