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

namespace CocopMessageLogger
{
    // Stubs classes to facilitate testing

    public class MetadataExtractor
    {
        public MetadataExtractor(object a)
        { }

        public string Name
        {
            get { return "Name for test"; }
        }

        public string PayloadSummary
        {
            get { return "Payload summary for test"; }
        }
    }
}
