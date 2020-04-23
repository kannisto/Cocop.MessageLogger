//
// Please make sure to read and understand README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Author: Petri Kannisto, Tampere University, Finland
// File created: 11/2019
// Last modified: 3/2020

using System;
using System.Windows;

namespace CocopMessageLogger
{
    /// <summary>
    /// Interaction logic for SelectExchange.xaml
    /// </summary>
    public partial class SelectExchangeDialog : Window
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="host">The original host value.</param>
        /// <param name="exchange">The exchange host value.</param>
        public SelectExchangeDialog(string host, string exchange)
        {
            InitializeComponent();

            HostTextBox.Text = host;
            ExchangeTextBox.Text = exchange;
        }
        
        /// <summary>
        /// Host.
        /// </summary>
        public string Host
        {
            get;
            private set;
        }

        /// <summary>
        /// Exchange.
        /// </summary>
        public string Exchange
        {
            get;
            private set;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Host = HostTextBox.Text;
            Exchange = ExchangeTextBox.Text;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
