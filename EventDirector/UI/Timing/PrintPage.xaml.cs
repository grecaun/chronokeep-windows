using EventDirector.Interfaces;
using EventDirector.UI.MainPages;
using Microsoft.Win32;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using MigraDoc.Rendering.Printing;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace EventDirector.UI.Timing
{
    /// <summary>
    /// Interaction logic for PrintPage.xaml
    /// </summary>
    public partial class PrintPage : ISubPage
    {
        TimingPage parent;
        IDBInterface database;
        Event theEvent;

        public PrintPage(TimingPage parent, IDBInterface database)
        {
            InitializeComponent();
            this.parent = parent;
            this.database = database;
            theEvent = database.GetCurrentEvent();
            List<Division> divisions = database.GetDivisions(theEvent.Identifier);
            FinishOnlyDivisionsBox.Items.Add(new ComboBoxItem()
            {
                Content = "All",
                Uid = "-1"
            });
            StartFinishDivisionsBox.Items.Add(new ComboBoxItem()
            {
                Content = "All",
                Uid = "-1"
            });
            AllDivisionsBox.Items.Add(new ComboBoxItem()
            {
                Content = "All",
                Uid = "-1"
            });
            divisions.Sort((x1, x2) => x1.Name.CompareTo(x2.Name));
            foreach (Division d in divisions)
            {
                FinishOnlyDivisionsBox.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
                StartFinishDivisionsBox.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
                AllDivisionsBox.Items.Add(new ComboBoxItem()
                {
                    Content = d.Name,
                    Uid = d.Identifier.ToString()
                });
            }
            FinishOnlyDivisionsBox.SelectedIndex = 0;
            StartFinishDivisionsBox.SelectedIndex = 0;
            AllDivisionsBox.SelectedIndex = 0;
        }

        public void UpdateView() { }

        private void FinishOnlyPrint_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Finish only times - print clicked.");
            PdfDocumentRenderer renderer = new PdfDocumentRenderer
            {
                Document = GetOverallPrintableDocument()
            };
            renderer.RenderDocument();
            System.Windows.Forms.PrintDialog printDialog = new System.Windows.Forms.PrintDialog();
            printDialog.AllowSomePages = true;
            printDialog.UseEXDialog = true;
            if (printDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MigraDocPrintDocument printDocument = new MigraDocPrintDocument();
                printDocument.Renderer = renderer.DocumentRenderer;
                printDocument.PrinterSettings = printDialog.PrinterSettings;
                printDocument.Print();
            }
        }

        private void FinishOnlySave_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Finish only times - save clicked.");
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Filter = "PDF (*.pdf)|*.pdf",
                InitialDirectory = database.GetAppSetting(Constants.Settings.DEFAULT_EXPORT_DIR).value
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                PdfDocumentRenderer renderer = new PdfDocumentRenderer
                {
                    Document = GetOverallPrintableDocument()
                };
                renderer.RenderDocument();
                renderer.PdfDocument.Save(saveFileDialog.FileName);
            }
        }

        public Document GetOverallPrintableDocument()
        {
            // Get all participants for the race and categorize them by their event specific identifier;
            Dictionary<int, Participant> participantDictionary = database.GetParticipants(theEvent.Identifier).ToDictionary(x => x.EventSpecific.Identifier, x => x);
            // Get all finish results for the race
            List<TimeResult> results = database.GetSegmentTimes(theEvent.Identifier, Constants.Timing.SEGMENT_FINISH);
            // REMOVE SOME DEPENDING ON WHO THEY WANT
            // Sort properly.
            results.Sort(TimeResult.CompareByDivisionPlace);
            Dictionary<string, List<TimeResult>> divisionResults = new Dictionary<string, List<TimeResult>>();
            foreach (TimeResult result in results)
            {
                if (!divisionResults.ContainsKey(result.DivisionName))
                {
                    divisionResults[result.DivisionName] = new List<TimeResult>();
                }
                divisionResults[result.DivisionName].Add(result);
            }

            // Create document to output;
            Document document = CreateDocument(theEvent.YearCode, theEvent.Name, database.GetAppSetting(Constants.Settings.COMPANY_NAME).value);

            foreach (string divName in divisionResults.Keys)
            {
                // Set margins to really small
                Section section = document.AddSection();
                section.PageSetup.TopMargin = Unit.FromInch(1.7);
                section.PageSetup.LeftMargin = Unit.FromInch(0.3);
                section.PageSetup.RightMargin = Unit.FromInch(0.3);
                section.PageSetup.BottomMargin = Unit.FromInch(0.3);
                // Create header so we can always see the name of the event on the page.
                HeaderFooter header = section.Headers.Primary;
                Paragraph curPara = header.AddParagraph(theEvent.Name);
                curPara.Style = "Heading1";
                curPara = header.AddParagraph("Overall Results");
                curPara.Style = "Heading2";
                curPara = header.AddParagraph(theEvent.Date);
                curPara.Style = "Heading3";
                curPara = header.AddParagraph(divName);
                curPara.Style = "DivisionName";
                // Create a tabel to display the results.
                Table table = new Table();
                table.Borders.Width = 0.0;
                table.Rows.Alignment = RowAlignment.Center;
                // Create the rows we're displaying
                table.AddColumn(Unit.FromCentimeter(1)); // place
                table.AddColumn(Unit.FromCentimeter(1.2)); // bib
                table.AddColumn(Unit.FromCentimeter(5)); // name
                table.AddColumn(Unit.FromCentimeter(1)); // gender
                table.AddColumn(Unit.FromCentimeter(1)); // age
                table.AddColumn(Unit.FromCentimeter(2.7)); // gun time
                table.AddColumn(Unit.FromCentimeter(2.7)); // chip time
                                                           // add the header row
                Row row = table.AddRow();
                row.Style = "ResultsHeader";
                row.Cells[0].AddParagraph("Place");
                row.Cells[1].AddParagraph("Bib");
                row.Cells[2].AddParagraph("Name");
                row.Cells[2].Style = "ResultsHeaderName";
                row.Cells[3].AddParagraph("G");
                row.Cells[4].AddParagraph("Age");
                row.Cells[5].AddParagraph("Gun Time");
                row.Cells[6].AddParagraph("Chip Time");
                foreach (TimeResult result in divisionResults[divName])
                {
                    row = table.AddRow();
                    row.Style = "ResultsRow";
                    row.Cells[0].AddParagraph(result.Place.ToString());
                    row.Cells[1].AddParagraph(result.Bib.ToString());
                    row.Cells[2].AddParagraph(result.ParticipantName);
                    row.Cells[2].Style = "ResultsRowName";
                    row.Cells[3].AddParagraph(result.Gender);
                    row.Cells[4].AddParagraph(participantDictionary[result.EventSpecificId].Age(theEvent.Date));
                    row.Cells[5].AddParagraph(result.Time);
                    row.Cells[6].AddParagraph(result.ChipTime);
                }
                section.Add(table);
            }
            return document;
        }

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

            style = document.Styles.AddStyle("DivisionName", "Heading2");
            style.ParagraphFormat.OutlineLevel = OutlineLevel.Level1;
            style.ParagraphFormat.SpaceBefore = Unit.FromMillimeter(2.5);

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

        private void StartFinishPrint_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Start and Finish times - print clicked.");
        }

        private void StartFinishSave_Click(object sender, RoutedEventArgs e)
        {
            Log.D("Start and finish times - save clicked.");
        }

        private void AllPrint_Click(object sender, RoutedEventArgs e)
        {
            Log.D("All times - print clicked.");
        }

        private void AllSave_Click(object sender, RoutedEventArgs e)
        {
            Log.D("All times - save clicked.");
        }

        public void Search(string value) { }

        public void Show(PeopleType type) { }

        public void SortBy(SortType type) { }

        public void EditSelected() {}

        public void Closing() { }

        public void Keyboard_Ctrl_A() { }

        public void Keyboard_Ctrl_S() { }

        public void Keyboard_Ctrl_Z() { }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            parent.LoadMainDisplay();
        }
    }
}
