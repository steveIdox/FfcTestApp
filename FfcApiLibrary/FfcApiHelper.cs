using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Newtonsoft.Json;

namespace idox.eim.fusionp8
{
    public class FfcApiHelper
    {
        public static void ApplyLicense()
        {
            Aspose.Pdf.License license = new Aspose.Pdf.License();
            license.SetLicense("d:\\test\\2025_Aspose.Total.lic");
        }
        public const string ticket = "{\"InputFiles\":[\"NEWINPUT\"],\"OutputFilePath\":\"NEWOUTPUT\",\"Jobs\":[{\"Type\":\"ConvertToPdf\",\"JobSettings\":{\"PenTableFilePath\":\"NEWCTB\",\"RenderingSettings\":{\"CadSettings\":{\"UseActualPageSize\":\"false\",\"TextAsGeometrySHX\":\"false\",\"TextAsGeometryTTF\":\"false\",\"CheckIs3dDrawing\":\"false\",\"CadPaperSize\":{\"Height\":1188.8,\"Width\":841,\"TopMargin\":3.5,\"BottomMargin\":3.5,\"LeftMargin\":3.5,\"RightMargin\":3.5,\"PageOrientation\":\"Landscape\"},\"PlotSettings\":{\"PageLoadOrder\":\"PaperSpaceFirst\"}},\"PdfSettings\":{\"HeaderSettings\":[{}],\"FooterSettings\":[{}],\"Watermarks\":[{}]}}}}]}";

        public static string BuildJobTicket(string inputFile)
        {
            return ticket;
            //if (System.IO.File.Exists(inputFile))
            //{
            //    string jobTicket = ticket;
            //    string escapedInputFile = inputFile.Replace("\\", "\\\\");

            //    jobTicket = jobTicket.Replace("NEWINPUT", escapedInputFile);
            //    jobTicket = jobTicket.Replace("NEWOUTPUT", escapedInputFile.Replace(".dwg", ".pdf"));
            //    jobTicket = jobTicket.Replace("NEWCTB", "");
            //    return jobTicket;
            //}
            //return String.Empty;
        }



    }
}
