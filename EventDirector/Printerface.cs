using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace EventDirector
{
    class Printerface
    {
        public static void PrintDayOf(DayOfParticipant part, Division div)
        {
            PrintDialog dialog = new PrintDialog();
            PrintDayOf(part, div, dialog);
        }

        public static void PrintDayOfShowDialog(DayOfParticipant part, Division div)
        {
            PrintDialog dialog = new PrintDialog();
            if (dialog.ShowDialog() != true)
            {
                return;
            }
            PrintDayOf(part, div, dialog);
        }


        public static void PrintDayOf(DayOfParticipant part, Division div, PrintDialog dialog)
        {
            StackPanel printPanel = new StackPanel
            {
                Margin = new System.Windows.Thickness(15)
            };
            printPanel.Children.Add(new TextBlock
            {
                Text = "Distance: " + div.Name,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "First Name: " + part.First,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Last Name: " + part.Last,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Gender: " + part.Gender,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Birthdate: " + part.Birthday,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Phone: " + part.Phone,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Mobile: " + part.Mobile,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Email: " + part.Email,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Parent: " + part.Parent,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Street: " + part.Street,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Apartment: " + part.Street2,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "City: " + part.City,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "State: " + part.State,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Zip: " + part.Zip,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Country: " + part.Country,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Comments: " + part.Comments,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Other: " + part.Other,
                TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
                {
                    Text = "Emergency Contact Name: " + part.EmergencyName,
                    TextAlignment = System.Windows.TextAlignment.Left
            });
            printPanel.Children.Add(new TextBlock
            {
                Text = "Emergency Contact Phone: " + part.EmergencyPhone,
                TextAlignment = System.Windows.TextAlignment.Left
            });

            printPanel.Measure(new System.Windows.Size(dialog.PrintableAreaWidth, dialog.PrintableAreaHeight));
            printPanel.Arrange(new System.Windows.Rect(new System.Windows.Point(0, 0), printPanel.DesiredSize));

            dialog.PrintVisual(printPanel, String.Format("{0} {1}", part.First, part.Last));
        }
    }
}
