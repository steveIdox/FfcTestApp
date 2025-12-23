# Complex PDF Processor

This application demonstrates how to handle complex PDFs that may timeout during FFC processing by implementing a split-and-merge strategy.

## How It Works

1. **Normal Processing**: First attempts to process the PDF normally through FFC
2. **Timeout Detection**: If the job times out (default: 10 minutes), it triggers the fallback strategy
3. **Split Strategy**: Splits the complex PDF into individual pages
4. **Individual Processing**: Processes each page separately with shorter timeouts (5 minutes per page)
5. **Merge Strategy**: Combines all processed pages back into a single PDF

## Usage

```bash
FfcTestApp.exe <jobTicketPath> [queueName]
```

### Parameters
- `jobTicketPath`: Path to the FFC job ticket JSON file (contains input PDF path)
- `queueName`: (Optional) FFC queue name to submit jobs to

### Example
```bash
FfcTestApp.exe "sample-job-ticket.json" "default"
```

## Configuration

The processor can be configured with:
- FFC API URL (default: http://localhost:9000)
- Username and password for FFC authentication
- Timeout duration for normal processing (default: 10 minutes)
- Timeout duration for individual page processing (default: 5 minutes)

## Dependencies

- iText7: For PDF splitting and merging operations
- Newtonsoft.Json: For job ticket manipulation
- FfcApiLibrary: For FFC API interactions

## Error Handling

The processor handles various error scenarios:
- Authentication failures
- Job submission failures
- Processing timeouts
- Individual page processing failures
- PDF manipulation errors

All errors are logged and propagated with detailed error messages.