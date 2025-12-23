using System.Collections.Generic;

namespace idox.eim.fusionp8
{
    public class JobDefinition
    {
        public string Type { get; set; }
        public JobSettings JobSettings { get; set; }
    }

    public class JobSettings
    {
        public RenderingSettings RenderingSettings { get; set; }
    }

    public class RenderingSettings
    {
        public CadSettings CadSettings { get; set; }
        public PdfSettings PdfSettings { get; set; }
    }

    public class CadSettings
    {
        public bool UseActualPageSize { get; set; }
        public CadPaperSize CadPaperSize { get; set; }
        public PlotSettings PlotSettings { get; set; }
        public string SupportPath { get; set; }
        public bool TextAsGeometryTTF { get; set; }
        public bool TextAsGeometrySHX { get; set; }
        public bool CheckIs3dDrawing { get; set; }
    }

    public class CadPaperSize
    {
        public string PageOrientation { get; set; }
    }

    public class PlotSettings
    {
        public string PageLoadOrder { get; set; }
    }

    public class PdfSettings
    {
        public List<HeaderFooterSetting> HeaderSettings { get; set; }
        public List<HeaderFooterSetting> FooterSettings { get; set; }
        public List<WatermarkSetting> Watermarks { get; set; }
    }

    public class HeaderFooterSetting
    {
        public string Text { get; set; }
        public double LeftMargin { get; set; }
        public double RightMargin { get; set; }
        public string Alignment { get; set; }
        public TextConversion TextConversion { get; set; }
    }

    public class WatermarkSetting
    {
        public string Text { get; set; }
        public string OrientationStyle { get; set; }
        public string Pages { get; set; }
        public TextConversion TextConversion { get; set; }
        public bool AutoScale { get; set; }
        public int ScalePercent { get; set; }
        public double SymbolHeight { get; set; }
        public double SymbolWidth { get; set; }
        public bool WordWrapping { get; set; }
    }

    public class TextConversion
    {
        public string FontFamily { get; set; }
        public int FontSize { get; set; }
        public int Opacity { get; set; }
        public string FontColor { get; set; }
    }
}