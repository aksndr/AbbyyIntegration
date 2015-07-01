using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using WindowsService.RSSoapService;

namespace WindowsService.Models
{
    [DataContract]
    public class ExportSettings
    {
        public ExportSettings(){}
                
        [DataMember]
        public int ID { get; set; }
        [DataMember]
        public int workTypeId { get; set; }
        [DataMember]
        public int order { get; set; }
        [DataMember]
        public string workFlowName { get; set; }
        [DataMember]
        public string formatName { get; set; }

        public OutputFormatSettings format;        
        private bool applyFormat()
        {
            switch (this.formatName)
            {
                case "pdf":                     
                    format = new PDFExportSettings();
                    applyPDFFormatSettings((PDFExportSettings)format);
                    return true;

                case "tiff": 
                    format = new TiffExportSettings();
                    applyTIFFFormatSettings((TiffExportSettings)format);
                    return true;
            }
            return false; 
        }

        public OutputFormatSettings getFormat()
        {
            applyFormat();  
            return format;
        }

        private void applyPDFFormatSettings(PDFExportSettings pFormat)
        {            
            pFormat.PDFExportMode = PDFExportModeEnum.PEM_ImageOnText;
            pFormat.PictureResolution = 120;
            pFormat.Quality = 70;
            pFormat.UseOriginalPaperSize = true;
        }

        private void applyTIFFFormatSettings(TiffExportSettings pFormat)
        {
            pFormat.ColorMode = ImageColorModeEnum.ICM_AsIs;
            pFormat.Compression = ImageCompressionTypeEnum.ICT_Zip;
            pFormat.Resolution = 300;
        }

    }
}
