using System;

public class PageTicket
{
    public int PageIndex { get; set; }

    // Tracks whether a job is actively being processed
    public bool InFlight { get; set; } = false;

    // Whether the job completed successfully
    public bool Completed => IsSuccess;

    // The path to the per-page job ticket JSON
    public string TicketPath { get; set; }

    // SUCCESS FLAG set only when output is returned
    public bool IsSuccess { get; set; } = false;

    // Where the *successful* rendered page actually is
    public string OutputPath { get; set; }

    // Where the page *should* go when created (target path)
    public string ExpectedOutputPath { get; set; }

    // FFC job ID (returned by first submit)
    public string JobId { get; set; }

    // Number of times the page has been sent to FFC
    public int RetryCount { get; set; } = 0;

    // Last time the page was submitted
    public DateTime LastSubmitUtc { get; set; }
}
