//
// Please make sure to read and understand README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Author: Petri Kannisto, Tampere University, Finland
// File created: 12/2019
// Last modified: 3/2020

using System;
using System.Windows;

namespace CocopMessageLogger
{
    /// <summary>
    /// A dialog to show application information.
    /// </summary>
    public partial class AboutDialog : Window
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public AboutDialog()
        {
            InitializeComponent();

            try
            {
                // Assigning product name
                ProductTextBlock.Text = Globals.ProductName;

                // Assigning assembly version
                VersionTextBlock.Text = "Version " + Globals.Version;
            }
            catch { }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs args)
        {
            try
            {
                // Open the web page in browser
                var startInfo = new System.Diagnostics.ProcessStartInfo(args.Uri.AbsoluteUri);
                System.Diagnostics.Process.Start(startInfo);

                args.Handled = true;
            }
            catch { }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the window
            Close();
        }

        private void ReadmeHyperlink_Click(object sender, RoutedEventArgs e)
        {
            // Show readme file
            System.Diagnostics.Process.Start(Globals.ReadmeFilepath);
        }

        private void LicenseHyperlink_Click(object sender, RoutedEventArgs e)
        {
            // Show license file
            System.Diagnostics.Process.Start(Globals.LicenseFilepath);
        }
    }
}
