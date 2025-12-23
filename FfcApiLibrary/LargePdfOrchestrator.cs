using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Aspose.Pdf.PageNumber;

namespace idox.eim.fusionp8
{
    public interface ILargePdfOrchestrator
    {
        /// <summary>
        /// Executes the fallback split/convert/merge pipeline for extremely large or complex PDFs.
        /// Returns the final merged PDF output path.
        /// </summary>
        Task<string> ProcessLargePdfAsync(
            string originalJobTicketPath,
            CancellationToken cancellationToken = default
        );
    }
    public class LargePdfOrchestrator : ILargePdfOrchestrator
    {
        private readonly List<FfcClientWrapper> ffcClients = new List<FfcClientWrapper>();
        private readonly string defaultQueueName;
        private readonly int maxParallelFfcJobs;
        private readonly int timeout;
        private readonly string[] ffcUris;
        private readonly SemaphoreSlim rateLimiter = new SemaphoreSlim(1, 1);
        private readonly Dictionary<HttpClient, string> accessTokens =
    new Dictionary<HttpClient, string>();
        private int nextClientIndex = -1;
        // Tune this later; start with 200–300 ms.
        private const int SubmitDelayMs = 200;
        const int MaxRetries = 3;
        const int InFlightTimeoutMinutes = 10;

        public LargePdfOrchestrator(List<FfcClientWrapper> clientWrappers, string defaultQueueName, int maxParallelFfcJobs, int timeout)
        {
            foreach (FfcClientWrapper wrapper in clientWrappers)
            {
                wrapper.HttpClient = new HttpClient() { BaseAddress = new Uri(wrapper.FfcUri) };
                wrapper.AccessToken = FfcApiOperations.RequestAccessToken(wrapper.FfcUri, wrapper.FfcUsername, wrapper.FfcPassword);
                ffcClients.Add(wrapper);
            }
            this.defaultQueueName = defaultQueueName;
            this.maxParallelFfcJobs = maxParallelFfcJobs;
            this.timeout = timeout;
        }
        private FfcClientWrapper GetNextClient()
        {
            int index = Interlocked.Increment(ref nextClientIndex);
            if (index > 1_000_000_000)
            {
                // Reset to zero so increment continues smoothly
                Interlocked.Exchange(ref nextClientIndex, 0);
                index = 0;
            }
            if (index >= ffcClients.Count) index = 0;
            Console.WriteLine($"Next client is : {ffcClients[index].FfcUri}");
            return ffcClients[index % ffcClients.Count];
        }
        private bool PageIsStalled(PageTicket p)
        {
            return p.InFlight &&
                   (DateTime.UtcNow - p.LastSubmitUtc) >
                   TimeSpan.FromMinutes(InFlightTimeoutMinutes);
        }
        public async Task<string> ProcessLargePdfAsync(string originalJobTicketPath, CancellationToken ct)
        {
            // 1. Load original ticket and get input PDF path
            var originalJson = File.ReadAllText(originalJobTicketPath);
            Console.WriteLine($"Original ticket: {originalJson}");
            var originalTicket = JsonConvert.DeserializeObject<JobTicket>(originalJson);

            var inputPdf = originalTicket.InputFiles.Single(); // assuming one input
            var workingFolder = Path.Combine(Path.GetDirectoryName(inputPdf), "split");
            var ticketsFolder = Path.Combine(workingFolder, "tickets");
            var outputsFolder = Path.Combine(workingFolder, "outputs");

            Directory.CreateDirectory(ticketsFolder);
            Directory.CreateDirectory(outputsFolder);

            // 2. Split into per-page PDFs
            var perPageInputFiles = PdfSplitHelper.SplitIntoPages(inputPdf, workingFolder);

            // 3. Build per-page tickets
            List<PageTicket> results = new List<PageTicket>();
            int iPageCount = 1;
            foreach (var pagePath in perPageInputFiles)
            {
                var perPageOutput = Path.Combine(outputsFolder,
                    Path.GetFileNameWithoutExtension(pagePath) + "-out.pdf");

                var ticketPath = JobTicketFactory.CreatePerPageTicket(
                    originalJobTicketPath,
                    pagePath,
                    perPageOutput,
                    ticketsFolder);

                results.Add(new PageTicket { PageIndex = iPageCount, TicketPath = ticketPath, OutputPath = perPageOutput });
                iPageCount++;
            }

            //---------------------------------------------------------------------
            // 4. Submit per-page jobs to FFC with concurrency limit and retries
            //---------------------------------------------------------------------
            Console.WriteLine("Beginning distributed render...");

            var throttler = new SemaphoreSlim(maxParallelFfcJobs, maxParallelFfcJobs);
            const int MaxRetries = 3;
            const int StallMinutes = 10;

            bool PageIsStalled(PageTicket p) =>
                p.InFlight &&
                (DateTime.UtcNow - p.LastSubmitUtc) > TimeSpan.FromMinutes(StallMinutes);

            while (true)
            {
                // Pages that need work
                var pending = results.Where(p =>
                    !p.Completed &&
                    (!p.InFlight || PageIsStalled(p)) &&
                    p.RetryCount < MaxRetries).ToList();

                // Are any pages still running?
                bool anyInFlight = results.Any(p => p.InFlight);

                // 1. All done? (no pending AND nothing running)
                if (!pending.Any() && !anyInFlight)
                {
                    Console.WriteLine("All page jobs completed.");
                    break;   // exit the work queue
                }

                // 2. Nothing to start but jobs still running → wait for them
                if (!pending.Any() && anyInFlight)
                {
                    await Task.Delay(500, ct);
                    continue;
                }

                // 3. Submit all pending pages
                foreach (var page in pending)
                {
                    page.InFlight = true;
                    page.RetryCount++;
                    page.LastSubmitUtc = DateTime.UtcNow;

                    _ = Task.Run(async () =>
                    {
                        await throttler.WaitAsync(ct);
                        try
                        {
                            var client = GetNextClient();
                            Console.WriteLine($"Submitting p{page.PageIndex} to {client.FfcUri}");

                            var outputPath = await SubmitPageToFfcAsync(
                                page.PageIndex,
                                client,
                                page.TicketPath,
                                ct);

                            page.OutputPath = outputPath;
                            page.IsSuccess = true;
                            page.InFlight = false;

                            Console.WriteLine($"SUCCESS p{page.PageIndex}: {outputPath}");
                        }
                        catch (Exception ex)
                        {
                            page.InFlight = false;
                            Console.WriteLine($"FAIL p{page.PageIndex}: {ex.Message}");
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    });
                }

                await Task.Delay(500, ct);
            }


            // 5. At this point, all per-page outputs should be present in outputsFolder
            //    Next step would be to merge them using Aspose.PDF.
            Console.WriteLine("Merging outputs...");
            var finalOutputPath = originalTicket.OutputFilePath;
            MergePerPagePdfs(results, finalOutputPath);

            return finalOutputPath;
        }

        private async Task<string> SubmitPageToFfcAsync(
             int iSubmitCount,
             FfcClientWrapper ffcClient,
             string pageTicketPath,
             CancellationToken ct)
        {
            //
            // 🔵 RATE LIMITER — the critical fix
            //
            await rateLimiter.WaitAsync(ct);
            try
            {
                // This enforces a minimum delay between submissions
                await Task.Delay(SubmitDelayMs, ct);
            }
            finally
            {
                rateLimiter.Release();
            }


            // Build FFC job submission URI
            var encodedPath = Uri.EscapeDataString(pageTicketPath ?? "");
            var uri = $"api/ffc-jobs?jobTicketPath={encodedPath}";

            if (!string.IsNullOrEmpty(defaultQueueName))
            {
                uri += $"&queueName={Uri.EscapeDataString(defaultQueueName)}";
            }

            string ffcBase = ffcClient.FfcUri;
            string fullUri = ffcBase + uri;

            Console.WriteLine($"Submitting {pageTicketPath} to FFC at {fullUri}");


            //
            // 1️⃣ SUBMIT THE JOB
            //
            string submitResponseJson = idox.eim.fusionp8.FfcApiOperations
                .SubmitJobTicket(fullUri, ffcClient.AccessToken, pageTicketPath, defaultQueueName);

            if (string.IsNullOrWhiteSpace(submitResponseJson))
                throw new Exception($"FFC returned empty response when submitting: {pageTicketPath}");

            var submitReport = JsonConvert.DeserializeObject<FileConverterJobReport>(submitResponseJson)
                ?? throw new Exception($"Could not parse FileConverterJobReport: {submitResponseJson}");

            Console.WriteLine($"{iSubmitCount} : Submitted page job {submitReport.Id} for {pageTicketPath}");


            //
            // 2️⃣ WAIT FOR JOB COMPLETION
            //
            string finalReportJson = await idox.eim.fusionp8.FfcApiOperations.WaitForConvertorJobAsync(
                iSubmitCount, ffcBase, ffcClient.AccessToken, submitReport.Id, timeout, ct);
            
            if (string.IsNullOrWhiteSpace(finalReportJson))
                throw new Exception($"FFC returned null final report for job {submitReport.Id}");

            var finalReport = JsonConvert.DeserializeObject<FileConverterJobReport>(finalReportJson)
                ?? throw new Exception($"Could not parse final FileConverterJobReport: {finalReportJson}");
            if (finalReport.ConversionStatus.Equals("FAILED", StringComparison.OrdinalIgnoreCase))
            {
                // Log the error and decide how to handle
                Console.WriteLine($"Page FAILED: JobId={finalReport.Id}, Error={finalReport.ErrorDescription}");

                // Option 1 — throw (stop entire pipeline)
                throw new Exception($"Page conversion failed: {finalReport.ErrorDescription}");

                // Option 2 — mark output file as a blank page
                // Option 3 — retry conversion in image-mode
            }

            //
            // 3️⃣ FETCH OUTPUT FILE(S)
            //
            JobFilesPaths files = idox.eim.fusionp8.FfcApiOperations
                .GetFileConverterJobFiles(iSubmitCount, ffcBase, ffcClient.AccessToken, finalReport.Id);

            if (files == null)
                throw new Exception($"GetFileConverterJobFiles returned null for job {finalReport.Id}");

            Console.WriteLine($"Job {finalReport.Id} completed. Output: {files.OutputFilePath}");

            //
            // 4️⃣ RETURN final rendition path
            //
            return files.OutputFilePath;
        }


        private static void MergePerPagePdfs(List<PageTicket> results, string finalOutputPath)
        {
            // Sort using PageIndex (1..N)
            var orderedFiles = results
                .OrderBy(r => r.PageIndex)
                .Select(r => r.OutputPath)
                .ToList();

            using (var finalDoc = new Aspose.Pdf.Document())
            {
                foreach (var file in orderedFiles)
                {
                    using (var pageDoc = new Aspose.Pdf.Document(file))
                    {
                        finalDoc.Pages.Add(pageDoc.Pages);
                    }
                }

                finalDoc.Save(finalOutputPath);
            }
        }
    }

}
