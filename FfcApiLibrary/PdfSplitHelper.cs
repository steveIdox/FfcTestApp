using Aspose.Pdf;
using System;
using System.Collections.Generic;
using System.IO;

public static class PdfSplitHelper
{
    public static List<string> SplitIntoPages(string inputPdfPath, string outputFolder)
    {
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        using (var doc = new Aspose.Pdf.Document(inputPdfPath))
        {
            var outputFiles = new List<string>(doc.Pages.Count);

            for (int page = 1; page <= doc.Pages.Count; page++)
            {
                using (var singlePageDoc = new Document())
                {
                    singlePageDoc.Pages.Add(doc.Pages[page]);

                    var outputPath = Path.Combine(outputFolder, $"page-{page}.pdf");
                    Console.WriteLine($"Saving page {page} to {outputPath}");
                    singlePageDoc.Save(outputPath);
                    outputFiles.Add(outputPath);
                }
            }

            return outputFiles;
        }
    }
}
