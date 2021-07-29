using MigraDoc.DocumentObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChronoKeep.IO
{
    class PrintingInterface
    {
        public static Document CreateDocument(string year, string eventName, string companyName)
        {
            Document document = new Document();
            document.Info.Title = string.Format("{0} {1}", year, eventName);
            document.Info.Subject = "Results";
            document.Info.Author = companyName;

            // Declare styles
            MigraDoc.DocumentObjectModel.Style style = document.Styles["Normal"];
            style.Font.Color = Colors.Black;

            style = document.Styles["Heading1"];
            style.Font.Size = 18;
            style.ParagraphFormat.OutlineLevel = OutlineLevel.BodyText;
            style.ParagraphFormat.Alignment = ParagraphAlignment.Center;
            style.ParagraphFormat.SpaceAfter = 1;

            style = document.Styles["Heading2"];
            style.Font.Size = 14;
            style.ParagraphFormat.OutlineLevel = OutlineLevel.BodyText;
            style.ParagraphFormat.Alignment = ParagraphAlignment.Center;
            style.ParagraphFormat.SpaceBefore = 1;
            style.ParagraphFormat.SpaceAfter = 1;

            style = document.Styles["Heading3"];
            style.Font.Size = 10;
            style.ParagraphFormat.OutlineLevel = OutlineLevel.BodyText;
            style.ParagraphFormat.Alignment = ParagraphAlignment.Center;
            style.ParagraphFormat.SpaceBefore = 1;

            style = document.Styles.AddStyle("DistanceName", "Heading2");
            style.ParagraphFormat.OutlineLevel = OutlineLevel.Level1;
            style.ParagraphFormat.SpaceBefore = Unit.FromMillimeter(2.5);

            style = document.Styles.AddStyle("SubHeading", "Heading2");
            style.ParagraphFormat.OutlineLevel = OutlineLevel.Level2;
            style.ParagraphFormat.SpaceBefore = Unit.FromMillimeter(0);
            style.ParagraphFormat.SpaceAfter = Unit.FromMillimeter(5);

            style = document.Styles.AddStyle("ResultsRow", "Normal");
            style.Font.Size = 9;
            style.ParagraphFormat.SpaceAfter = Unit.FromMillimeter(0.5);
            style.ParagraphFormat.Alignment = ParagraphAlignment.Center;

            style = document.Styles.AddStyle("ResultsRowName", "ResultsRow");
            style.ParagraphFormat.Alignment = ParagraphAlignment.Left;

            style = document.Styles.AddStyle("ResultsHeader", "ResultsRow");
            style.ParagraphFormat.SpaceAfter = Unit.FromMillimeter(1);
            style.Font.Underline = Underline.Single;

            style = document.Styles.AddStyle("ResultsHeaderName", "ResultsHeader");
            style.ParagraphFormat.Alignment = ParagraphAlignment.Left;


            return document;
        }
        public static Section SetupMargins(Section section)
        {
            Section output = section;
            output.PageSetup.TopMargin = Unit.FromInch(1.7);
            output.PageSetup.LeftMargin = Unit.FromInch(0.3);
            output.PageSetup.RightMargin = Unit.FromInch(0.3);
            output.PageSetup.BottomMargin = Unit.FromInch(1.0);
            return output;
        }
    }
}
