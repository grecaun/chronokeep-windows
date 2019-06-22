using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ChronoKeep
{
    class Printerface
    {
        public static void PrintDayOf(DayOfParticipant part, Division div)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(()=>
            {
                PrintDialog dialog = new PrintDialog();
                Log.D("Printing without showing a dialog.");
                PrintDayOf(part, div, dialog);
            }));
        }

        public static void PrintDayOfShowDialog(DayOfParticipant part, Division div)
        {
            PrintDialog dialog = new PrintDialog();
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            Log.D("User requests print.");
            PrintDayOf(part, div, dialog);
        }


        public static void PrintDayOf(DayOfParticipant part, Division div, PrintDialog dialog)
        {
            StackPanel printPanel = new StackPanel
            {
                Margin = new Thickness(40)
            };
            printPanel.Children.Add(new TextBlock
            {
                Text = "Distance: " + div.Name,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "First Name: " + part.First,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Last Name: " + part.Last,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Gender: " + part.Gender,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Birthdate: " + part.Birthday,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Mobile: " + part.Mobile,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Email: " + part.Email,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Parent: " + part.Parent,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Street: " + part.Street,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Apartment: " + part.Street2,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "City: " + part.City,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "State: " + part.State,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Zip: " + part.Zip,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Country: " + part.Country,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Comments: " + part.Comments,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Other: " + part.Other,
                TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
                {
                    Text = "Emergency Contact Name: " + part.EmergencyName,
                    TextAlignment = TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Emergency Contact Phone: " + part.EmergencyPhone,
                TextAlignment = TextAlignment.Left
            });

            printPanel.Measure(new Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight));
            Size desiredSize = printPanel.DesiredSize;
            desiredSize.Height = desiredSize.Height * 2;
            desiredSize.Width = desiredSize.Width * 2;
            printPanel.Arrange(new Rect(new Point(0, 0), desiredSize));

            dialog.PrintVisual(printPanel, String.Format("{0} {1}", part.First, part.Last));
        }
    }
}
