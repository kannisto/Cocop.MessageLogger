//
// Please make sure to read and understand README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Authors: Petri Kannisto and Tapio Vaaranmaa, Tampere University, Finland
// File created: 10/2019
// Last modified: 3/2020

using System;
using System.Windows;
using System.Windows.Controls;
using SysColl = System.Collections.Generic;
using SysDoc = System.Windows.Documents;
using MsgMeas = Cocop.MessageSerialiser.Meas;
using MsgBiz = Cocop.MessageSerialiser.Biz;

namespace CocopMessageLogger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private AppLogic m_appLogic = null;

        // This is used in lock statements to synchronise data between threads
        private object m_lockObject = new object();
        
        // Host and exchange
        private string m_host = "localhost";
        private string m_exchange = "my-exchange";


        /// <summary>
        /// Constructor.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // Do nothing else here. Objects are initialised in Window_Loaded().
        }


        #region UI event handlers

        private void Window_Loaded(object sender, RoutedEventArgs args)
        {
            // Initialising object only here, because the creation of AppLogic it takes long.
            // However, it is considered good that the window is visible while initialising.
            try
            {
                Title = string.Format("{0} v.{1}", Globals.ProductName, Globals.Version);
                
                // Using the "wait" cursor during startup
                var previousCursor = Cursor;
                Cursor = System.Windows.Input.Cursors.Wait;

                m_appLogic = new AppLogic(ConnectionEventCallback, MessageReceivedCallback);

                // Creating a persistor object - If anything has been saved in the disk, loading it
                UserInputPersistor persistor = UserInputPersistor.Load(); // Does not leak exceptions
                m_host = persistor.Host;
                m_exchange = persistor.Exchange;
                TopicTextBox.Text = persistor.TopicPattern;
                UsernameTextBox.Text = persistor.Username;
                SecureConnectionCheckBox.IsChecked = persistor.IsSecureConnection;

                // Set up UI state
                ChangeUiState(canConnect: true, canTerminate: false);
                SetConnectionStatusText(text: "Not connected", isProblem: false);

                // Setting to the initial state, mostly applies to filters
                SetToInitialState();

                // Restoring the default cursor
                Cursor = previousCursor;

                args.Handled = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(this, "Cannot start application: " + e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Trying to add to log
                try
                {
                    HandleBug(e);
                }
                catch { }

                throw new Exception("Failed to start application", e);
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                var dialog = new AboutDialog()
                {
                    Owner = this
                };
                dialog.ShowDialog();

                args.Handled = true;
            }
            catch { } // No error logging needed here, as this method has a low importance
        }

        private void SelectExchangeButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                var dialog = new SelectExchangeDialog(host: m_host, exchange: m_exchange)
                {
                    Owner = this
                };

                // Get user input
                bool? result = dialog.ShowDialog();

                // Did the user confirm the values?
                if (result.HasValue && result.Value)
                {
                    m_host = dialog.Host;
                    m_exchange = dialog.Exchange;
                    
                    // Setting to initial state. This mostly affects filters.
                    SetToInitialState();
                }

                args.Handled = true;
            }
            catch (Exception e)
            {
                HandleBug(e);
            }
        }
        
        private void ConnectButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                // Clear any previous connection error
                ClearConnectionStatusLabel();

                bool isSecure = SecureConnectionCheckBox.IsChecked.HasValue && SecureConnectionCheckBox.IsChecked.Value;

                // Saving user input to disk
                var persistor = new UserInputPersistor
                {
                    Host = m_host,
                    Exchange = m_exchange,
                    TopicPattern = TopicTextBox.Text,
                    Username = UsernameTextBox.Text,
                    IsSecureConnection = isSecure
                };
                UserInputPersistor.Save(persistor);
                
                // Updating UI state
                ChangeUiState(canConnect: false, canTerminate: true);

                try
                {
                    // Connecting
                    var connRequest = new ConnectionRequest(host: m_host, exc: m_exchange, secure: isSecure,
                        user: UsernameTextBox.Text, pwd: PasswordTextBox.Password, topic: TopicTextBox.Text);
                    m_appLogic.Connect(connRequest);
                }
                catch (InvalidOperationException e)
                {
                    // Showing the error in UI
                    SetConnectionStatusText(text: e.Message, isProblem: true);
                    return;
                }

                args.Handled = true;
            }
            catch (Exception e)
            {
                HandleBug(e);
            }
        }

        private void TerminateButton_Click(object sender, RoutedEventArgs args)
        {
            // Clear any previous connection error
            ClearConnectionStatusLabel();

            try
            {
                // The user cannot do anything connection related until termination has finished
                SetConnectionStatusText(text: "Awaiting termination...", isProblem: false);
                ChangeUiState(canConnect: false, canTerminate: false);

                m_appLogic.TerminateConnection();

                args.Handled = true;
            }
            catch (Exception e)
            {
                // Showing the error in UI
                SetConnectionStatusText(text: e.Message, isProblem: true);
                return;
            }
        }

        private void ViewLogsButton_Click(object sender, RoutedEventArgs args)
        {
            ViewLogsFolder();
            args.Handled = true;
        }

        private void ViewMessageFilesButton_Click(object sender, RoutedEventArgs args)
        {
            ViewLogsFolder();
            args.Handled = true;
        }

        private void ViewUiErrorLogsButton_Click(object sender, RoutedEventArgs args)
        {
            ViewLogsFolder();
            args.Handled = true;
        }

        private void ViewLogsFolder()
        {
            try
            {
                // Opening the folder in file explorer
                var path = m_appLogic.LogsFolder;
                System.Diagnostics.Process.Start(path);
            }
            catch (Exception e)
            {
                HandleBug(e);
            }
        }

        private void Window_Closed(object sender, EventArgs args)
        {
            try
            {
                if (m_appLogic != null)
                {
                    m_appLogic.Dispose();
                    m_appLogic = null;
                }
            }
            catch { } // No can do
        }
        
        private void FilterTimeWindowStartDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs args)
        {
            try
            {
                // When there is no start date, cannot apply start time or time window length
                bool startDateDefined = FilterTimeWindowStartDatePicker.SelectedDate.HasValue;
                FilterTimeWindowStartTimeTextBox.IsEnabled = startDateDefined;
                FilterTimeWindowLengthMinutes.IsEnabled = startDateDefined;

                // Set defaults to others filter controls
                ResetFilterTimes();

                args.Handled = true;
            }
            catch (Exception e)
            {
                HandleBug(e);
            }
        }
        
        private void ApplyFiltersButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                // Refreshing received messages and setting filters to the applied state
                ReceivedMessagesDataGrid.Items.Clear();
                RefreshMetadataAndTopics(onlyAddNewMessages: false);

                args.Handled = true;
            }
            catch (Exception e)
            {
                HandleBug(e);
            }
        }

        private void ReceivedMessagesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs args)
        {
            var originalCursor = Cursor;

            try
            {
                var selectedItem = (MessageDataRow)ReceivedMessagesDataGrid.SelectedItem;

                if (selectedItem == null)
                {
                    // Nothing selected
                    MessageTreeView.Items.Clear();
                    OpenInEditorButton.IsEnabled = false;
                    return;
                }

                OpenInEditorButton.IsEnabled = true;

                // Visualising the message. Using the wait cursor in case of a delay.
                Cursor = System.Windows.Input.Cursors.Wait;

                switch (selectedItem.PayloadType) // TODO-later: add support for more message types
                {
                    case PayloadTypeType.ObservationXml:
                        VisualiseObservation(selectedItem.Filepath);
                        break;
                    case PayloadTypeType.ProcessProductionScheduleXml:
                        VisualiseProductionSchedule(selectedItem.Filepath);
                        break;
                    default:
                        AddMessageToTreeview("(Unknown payload type)");
                        break;
                }

                args.Handled = true;
            }
            catch (Exception e)
            {
                HandleBug(e);
            }

            Cursor = originalCursor;
        }

        private void OpenInEditorButton_Click(object sender, RoutedEventArgs args)
        {
            try
            {
                var selectedItem = (MessageDataRow)ReceivedMessagesDataGrid.SelectedItem;

                if (selectedItem != null)
                {
                    // Opening the file in the default editor
                    System.Diagnostics.Process.Start(selectedItem.Filepath);
                }
            }
            catch (Exception e)
            {
                HandleBug(e);
            }
        }

        #endregion UI event handlers


        #region AppLogic callbacks

        private void ConnectionEventCallback(ConnectionEvent ev)
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    // Handing this task to the UI thread
                    var action = (Action)(() => ConnectionEventCallback(ev));
                    Dispatcher.BeginInvoke(action);

                    // Return to the calling class, a this is an asynchronous invoke
                    return;
                }
            
                // Changing UI state.
                // Can only connect if no connection is being maintained.
                // For termination, it is the negation of this.
                ChangeUiState(canConnect: !ev.IsConnectionMaintained, canTerminate: ev.IsConnectionMaintained);
            
                ShowConnectionStatus(ev);
            }
            catch (Exception e)
            {
                HandleBug(e);
            }
        }

        private void MessageReceivedCallback()
        {
            try
            {
                if (!Dispatcher.CheckAccess())
                {
                    // Handing this task to the UI thread
                    var action = (Action)(() => MessageReceivedCallback());
                    Dispatcher.BeginInvoke(action);

                    // Return to the calling class, a this is an asynchronous invoke
                    return;
                }

                // Get messages with the current filter
                var metadataState = GetMetadataUsingFilters();

                if (metadataState == null)
                {
                    // Failed to get metadata. Assuming any error messages have already been set.
                    return;
                }

                // Add any messages if new; refresh topics
                RefreshMetadataAndTopics(onlyAddNewMessages: true);
            }
            catch (Exception e)
            {
                HandleBug(e);
            }
        }

        #endregion AppLogic callbacks


        #region Filtering related
        
        private void RefreshMetadataAndTopics(bool onlyAddNewMessages)
        {
            var metadataState = GetMetadataUsingFilters();

            if (metadataState == null)
            {
                // Failed to get metadata
                return;
            }

            if (onlyAddNewMessages)
            {
                // Resolving the IDs of already added messages
                var alreadyAdded = GetNrsOfAlreadyAddedMessages();

                // Adding a row for each metadata item if new
                foreach (var i in metadataState.Items)
                {
                    if (!alreadyAdded.Contains(i.RunningId))
                    {
                        var row = new MessageDataRow(i);
                        ReceivedMessagesDataGrid.Items.Add(row);
                    }
                }
            }
            else
            {
                // Adding a row for each metadata item
                foreach (var i in metadataState.Items)
                {
                    var row = new MessageDataRow(i);
                    ReceivedMessagesDataGrid.Items.Add(row);
                }
            }
            
            // Refreshing topics
            RefreshTopics(metadataState);
        }

        private System.Collections.Generic.HashSet<int> GetNrsOfAlreadyAddedMessages()
        {
            var retval = new System.Collections.Generic.HashSet<int>();

            // Take the ID of each row
            foreach (var i in ReceivedMessagesDataGrid.Items)
            {
                var row = (MessageDataRow)i;
                retval.Add(int.Parse(row.RunningId));
            }
            
            return retval;
        }
        
        private AppLogic.MetadataState GetMetadataUsingFilters()
        {
            // Clear any previous filter status and messages
            FiltersExpander.Header = "Filters";
            SetStatusText(FiltersHintTextBlock, "", isProblem: false);

            TimeWindowBuilder timeWindow;

            // Trying to build time window for the query
            try
            {
                timeWindow = GetTimeWindowForFilter();
            }
            catch (ArgumentException e)
            {
                SetStatusText(FiltersHintTextBlock, e.Message, isProblem: true);
                return null;
            }

            // Create query object
            var query = new MetadataQuery(m_host, m_exchange)
            {
                TimeWindowStart = timeWindow.TimeWindowStart,
                TimeWindowEnd = timeWindow.TimeWindowEnd
            };

            // Adding topic for filtering if selected
            if (FilterTopicComboBox.SelectedValue != null)
            {
                var selectedValue = (string)FilterTopicComboBox.SelectedValue;
                query.Topic = selectedValue;
            }

            // Getting metadata and topics
            var metadataState = m_appLogic.GetMetadataState(query);

            // Refreshing message count
            FiltersExpander.Header = string.Format("Filters (showing {0} messages out of {1})",
                metadataState.Items.Count, metadataState.TotalItemCount);

            // Is the item limit applied? Set hint if yes.
            if (metadataState.IsLimitApplied)
            {
                SetStatusText(FiltersHintTextBlock, "Too many items found. Not showing all.", isProblem: false);
            }

            return metadataState;
        }

        private TimeWindowBuilder GetTimeWindowForFilter() // throws ArgumentException
        {
            // Start date specified?
            DateTime? startDate = FilterTimeWindowStartDatePicker.SelectedDate.HasValue ?
                DateTime.SpecifyKind(FilterTimeWindowStartDatePicker.SelectedDate.Value, DateTimeKind.Local) :
                (DateTime?)null;

            // throws ArgumentException
            return new TimeWindowBuilder(startDate,
                FilterTimeWindowStartTimeTextBox.Text, FilterTimeWindowLengthMinutes.Text);
        }

        private void RefreshTopics(AppLogic.MetadataState metadataState)
        {
            // Retrieving currently known topics from the host and exchange, still remembering
            // the previous topic selection

            var previousTopicSelection = (string)FilterTopicComboBox.SelectedValue;

            FilterTopicComboBox.Items.Clear();

            // Add an empty item as first
            FilterTopicComboBox.Items.Add("");

            // Adding actual topics
            foreach (var t in metadataState.Topics)
            {
                FilterTopicComboBox.Items.Add(t);
            }

            // Restoring previous selection
            FilterTopicComboBox.SelectedValue = previousTopicSelection;
        }

        private void ResetFilterTimes()
        {
            // Resetting all time-related filter controls but not start date
            FilterTimeWindowStartTimeTextBox.Text = "0:00";
            FilterTimeWindowLengthMinutes.Text = "";
        }

        #endregion Filtering related


        #region Message treeview logic

        private void AddMessageToTreeview(string message)
        {
            // Only show one node with the given message
            MessageTreeView.Items.Clear();
            AddNodeToTreeview(MessageTreeView, message);
        }

        private void VisualiseObservation(string filepath)
        {
            MessageTreeView.Items.Clear();
            
            try
            {
                // Deserialising the document
                var msg = System.IO.File.ReadAllBytes(filepath);
                var observation = new MsgMeas.Observation(msg);

                // Adding metadata
                AddNodeToTreeview(MessageTreeView, "Type", "Observation");
                AddNodeToTreeview(MessageTreeView, "Description", observation.Description);
                AddNodeToTreeview(MessageTreeView, "Name", observation.Name);
                AddNodeToTreeview(MessageTreeView, "Phenomenon time", FormatDateTimeForTreeview(observation.PhenomenonTime));
                AddNodeToTreeview(MessageTreeView, "Result time", FormatDateTimeForTreeview(observation.ResultTime));
                AddNodeToTreeview(MessageTreeView, "Procedure", observation.Procedure);
                AddNodeToTreeview(MessageTreeView, "Observed property", observation.ObservedProperty);
                AddNodeToTreeview(MessageTreeView, "Feature of interest", observation.FeatureOfInterest);
                AddNodeToTreeview(MessageTreeView, "Result quality", observation.ResultQuality.Value);

                // Adding payload
                var resultNode = AddNodeToTreeview(MessageTreeView, "Result", observation.Result);
            }
            catch (Exception e)
            {
                AddMessageToTreeview("Error: " + e.Message);
                m_appLogic.AddUnexpectedErrorToLog(e);
            }
        }

        private void VisualiseProductionSchedule(string filepath)
        {
            MessageTreeView.Items.Clear();

            try
            {
                // Deserialising the document
                var msg = System.IO.File.ReadAllBytes(filepath);
                var requestMsg = new MsgBiz.ProcessProductionSchedule(msg);

                // Adding metadata
                var root = AddNodeToTreeview(MessageTreeView, "ProcessProductionSchedule");
                AddNodeToTreeview(root, "CreationDateTime", FormatDateTimeForTreeview(requestMsg.CreationDateTime));

                // Adding production schedules
                AddProductionSchedules(root, requestMsg.ProductionSchedules);
            }
            catch (Exception e)
            {
                AddMessageToTreeview("Error: " + e.Message);
                m_appLogic.AddUnexpectedErrorToLog(e);
            }
        }

        private void AddProductionSchedules(ItemsControl parent, SysColl.List<MsgBiz.ProductionSchedule> schedules)
        {
            // Add each schedule
            foreach (var sched in schedules)
            {
                // Add schedule node
                var scheduleNode = AddNodeToTreeview(parent, "ProductionSchedule");
                
                // Adding production requests
                for (int a = 0; a < sched.ProductionRequests.Count; ++a)
                {
                    AddOneProductionRequest(scheduleNode, sched.ProductionRequests[a], a+1);
                }
            }
        }

        private void AddOneProductionRequest(ItemsControl parent, MsgBiz.ProductionRequest req, int nr)
        {
            var prodReqNode = AddNodeToTreeview(parent, "ProductionRequest " + nr);

            // Add hierarchy scope and identifier
            var hierarchyScope = req.HierarchyScopeObj ?? null;
            AddHierarchyScopeIfNotNull(prodReqNode, req.HierarchyScopeObj);
            AddIdentifierIfNotNull(prodReqNode, "ID", req.Identifier);

            // Add segment requirements
            for (int a = 0; a < req.SegmentRequirements.Count; ++a)
            {
                AddSegmentRequirement(prodReqNode, req.SegmentRequirements[a], a+1);
            }

            // Add scheduling params
            if (req.SchedulingParameters != null)
            {
                try
                {
                    // Expecting a data record
                    var paramsDataRec = new MsgMeas.Item_DataRecord((System.Xml.XmlNode[])req.SchedulingParameters);
                    AddNodeToTreeview(prodReqNode, "SchedulingParameters", paramsDataRec);
                }
                catch (Exception e)
                {
                    AddNodeToTreeview(prodReqNode, "SchedulingParameters failed to show: " + e.Message);
                    HandleBug(e);
                }
            }
        }
        
        private void AddSegmentRequirement(ItemsControl parent, MsgBiz.SegmentRequirement segm, int nr)
        {
            // Add segment requirement and its children

            var segmNode = AddNodeToTreeview(parent, "SegmentRequirement " + nr);

            AddIdentifierIfNotNull(segmNode, "ProcessSegmentID", segm.ProcessSegmentIdentifier);
            AddDateTimeIfNotNull(segmNode, "EarliestStartTime", segm.EarliestStartTime);
            AddDateTimeIfNotNull(segmNode, "LatestEndTime", segm.LatestEndTime);

            if (segm.EquipmentRequirements != null)
            {
                for (int a = 0; a < segm.EquipmentRequirements.Count; ++a)
                {
                    AddEquipmentRequirement(segmNode, segm.EquipmentRequirements[a], a + 1);
                }
            }
            if (segm.MaterialRequirements != null)
            {
                for (int a = 0; a < segm.MaterialRequirements.Count; ++a)
                {
                    AddMaterialRequirement(segmNode, "MaterialRequirement", segm.MaterialRequirements[a], a+1);
                }
            }
            // Any nested segment requirements?
            if (segm.SegmentRequirements != null)
            {
                for (int a = 0; a < segm.SegmentRequirements.Count; ++a)
                {
                    AddSegmentRequirement(segmNode, segm.SegmentRequirements[a], a+1);
                }
            }
        }

        private void AddEquipmentRequirement(ItemsControl parent, MsgBiz.EquipmentRequirement eqReq, int nr)
        {
            // Add equipment requirement

            var eqReqNode = AddNodeToTreeview(parent, "EquipmentRequirement " + nr);

            for (int a = 0; a < eqReq.Quantities.Count; ++a)
            {
                AddQuantity(eqReqNode, eqReq.Quantities[a], a + 1);
            }
        }

        private void AddMaterialRequirement(ItemsControl parent, string name, MsgBiz.MaterialRequirement matReq, int nr)
        {
            // Add material requirement

            var matReqNode = AddNodeToTreeview(parent, name + " " + nr);

            // Material definition IDs
            for (int a = 0; a < matReq.MaterialDefinitionIdentifiers.Count; ++a)
            {
                AddIdentifier(matReqNode, "MaterialDefinitionID", matReq.MaterialDefinitionIdentifiers[a], a + 1);
            }

            // Material lot IDs
            for (int a = 0; a < matReq.MaterialLotIdentifiers.Count; ++a)
            {
                AddIdentifier(matReqNode, "MaterialLotID", matReq.MaterialLotIdentifiers[a], a + 1);
            }

            // Material use
            if (matReq.MaterialUse != null)
            {
                AddNodeToTreeview(matReqNode, "MaterialUse", matReq.MaterialUse.Value.ToString());
            }

            // Add quantities
            for (int a = 0; a < matReq.Quantities.Count; ++a)
            {
                AddQuantity(matReqNode, matReq.Quantities[a], a + 1);
            }

            // Assembly requirements
            for (int a = 0; a < matReq.AssemblyRequirements.Count; ++a)
            {
                AddMaterialRequirement(matReqNode, "AssemblyRequirement", matReq.AssemblyRequirements[a], a + 1);
            }
        }
        
        private void AddQuantity(ItemsControl parent, MsgBiz.QuantityValue quant, int nr)
        {
            // Adds a quantity element and its children

            var quantityNode = AddNodeToTreeview(parent, "Quantity " + nr);

            AddNodeToTreeview(quantityNode, "QuantityString", quant.RawQuantityString ?? "");
            AddNodeToTreeview(quantityNode, "DataType", quant.DataType.Type.ToString());

            if (quant.UnitOfMeasure != null)
            {
                AddNodeToTreeview(quantityNode, "UnitOfMeasure", quant.UnitOfMeasure);
            }

            AddIdentifierIfNotNull(quantityNode, "Key", quant.Key);
        }

        private void AddDateTimeIfNotNull(ItemsControl parent, string fieldName, DateTime? dt)
        {
            if (!dt.HasValue) return;

            // Add to treeview
            AddNodeToTreeview(parent, fieldName, FormatDateTimeForTreeview(dt.Value));
        }

        private void AddHierarchyScopeIfNotNull(ItemsControl parent, MsgBiz.HierarchyScope hierScope)
        {
            if (hierScope == null) return;

            var hierScopeNode = AddNodeToTreeview(parent, "HierarchyScope");

            // Equipment ID specified?
            if (hierScope.EquipmentIdentifier != null)
            {
                AddIdentifierIfNotNull(hierScopeNode, "EquipmentID", hierScope.EquipmentIdentifier);
            }
            AddNodeToTreeview(hierScopeNode, "EquipmentElementLevel", hierScope.EquipmentElementLevel.ToString());
        }

        private void AddIdentifier(ItemsControl parent, string name, MsgBiz.IdentifierType identifier, int nr)
        {
            var value = identifier.Value ?? "";
            AddNodeToTreeview(parent, name + " " + nr, value);
        }

        private void AddIdentifierIfNotNull(ItemsControl parent, string name, MsgBiz.IdentifierType identifier)
        {
            if (identifier == null) return;

            // Add "" if null
            var value = identifier.Value ?? "";
            AddNodeToTreeview(parent, name, value);
        }

        private TreeViewItem AddNodeToTreeview(ItemsControl parent, string fieldName, string fieldValue)
        {
            // Is the value null?
            fieldValue = fieldValue ?? "";

            // Create a span and add content to it
            var span = new SysDoc.Span();
            var nameBold = new SysDoc.Bold();
            nameBold.Inlines.Add(new SysDoc.Run(fieldName + ": "));
            span.Inlines.Add(nameBold);
            span.Inlines.Add(fieldValue);
            
            return AddNodeToTreeview(parent, span);
        }

        private TreeViewItem AddNodeToTreeview(ItemsControl parent, string header)
        {
            // Create a span and add it
            var span = new SysDoc.Span();
            span.Inlines.Add(new SysDoc.Run(header));
            return AddNodeToTreeview(parent, span);
        }

        private TreeViewItem AddNodeToTreeview(ItemsControl parent, SysDoc.TextElement header)
        {
            var item = new TreeViewItem()
            {
                Header = header
            };
            parent.Items.Add(item);
            item.IsExpanded = true;
            return item;
        }

        private TreeViewItem AddNodeToTreeview(ItemsControl parent, string fieldName, MsgMeas.Item item)
        {
            string fieldValueString = "";

            if (item is MsgMeas.Item_TimeInstant timeInstant)
            {
                // DateTime value
                fieldValueString = FormatDateTimeForTreeview(timeInstant.Value);
            }
            else
            {
                // Default formatting
                fieldValueString = item.ToDisplayString();
            }

            // Adding header
            var node = AddNodeToTreeview(parent, fieldName, fieldValueString);

            if (item is MsgMeas.Item_DataRecord dataRecord)
            {
                // Add fields recursively
                foreach (var field in dataRecord)
                {
                    AddNodeToTreeview(node, field.Name, field.ItemObj);
                }
            }

            // TODO-later: show fields of time series

            return node;
        }
        
        private string FormatDateTimeForTreeview(DateTime dt)
        {
            if (dt.Kind == DateTimeKind.Unspecified)
            {
                return dt.ToString() + " (time zone?)";
            }
            else
            {
                return dt.ToLocalTime().ToString() + " (converted to local)";
            }
        }

        #endregion Message treeview logic


        #region Other members

        private void RefreshHostAndExchangeLabel()
        {
            HostAndExchangeLabel.Content = string.Format("{0} | {1}", m_host, m_exchange);
        }
        
        private void ChangeUiState(bool canConnect, bool canTerminate)
        {
            // Enabled when not connected:
            SelectExchangeButton.IsEnabled = canConnect;
            ConnectButton.IsEnabled = canConnect;
            SecureConnectionCheckBox.IsEnabled = canConnect;
            UsernameTextBox.IsEnabled = canConnect;
            PasswordTextBox.IsEnabled = canConnect;
            TopicTextBox.IsEnabled = canConnect;

            // Enable when connected or connecting:
            TerminateButton.IsEnabled = canTerminate;
        }

        private void ShowConnectionStatus(ConnectionEvent connEv)
        {
            // Category to string
            var msg = connEv.Description;

            // 1) Update connection expander header
            ConnectionExpander.Header = connEv.EventTypeString;

            // 2) Update the status label
            SetConnectionStatusText(text: msg, isProblem: connEv.IsError);

            // 3) In status log, show the message with a timestamp and the numeric status ID
            var fullMsg = string.Format("{0} {1}{2}",
                DateTime.Now.ToString("d.M. H.mm.ss"),
                msg,
                Environment.NewLine
                );
            ConnectionLogTextBox.AppendText(fullMsg);

            // Scrolling the status log to the bottom
            ConnectionLogTextBox.ScrollToEnd();
        }
        
        private void ClearConnectionStatusLabel()
        {
            SetConnectionStatusText(text: "", isProblem: false);
        }

        private void SetConnectionStatusText(string text, bool isProblem)
        {
            SetStatusText(textBlock: ConnectionErrorTextBlock, text: text, isProblem: isProblem);
        }

        private void SetStatusText(TextBlock textBlock, string text, bool isProblem)
        {
            // Method to set status information in a TextBlock

            var colorHex = "";

            if (isProblem)
            {
                text = "!!! " + text;
                colorHex = "#f00000";
            }
            else
            {
                colorHex = "#000000";
            }

            // Set text color and text for the text block
            var converter = new System.Windows.Media.BrushConverter();
            textBlock.Foreground = (System.Windows.Media.Brush)converter.ConvertFromString(colorHex);
            textBlock.Text = text;
        }
        
        private void HandleBug(Exception e)
        {
            // This method should only be called if there is a bug.
            try
            {
                // Showing an error message to the user
                var msg = "Unexpected error in app. See logs. Message: " + e.Message;
                UiErrorLabel.Content = msg;
                UiErrorGridRow.Height = new GridLength(35); // Set height to non-zero to make the error visible

                // Adding the error to log
                m_appLogic.AddUnexpectedErrorToLog(e);
            }
            catch { }
        }

        private void SetToInitialState()
        {
            // Show current host and exchange
            RefreshHostAndExchangeLabel();
            
            // Clearing all filters
            FilterTimeWindowStartDatePicker.SelectedDate = null;
            ResetFilterTimes();
            FilterTopicComboBox.Items.Clear();

            // Setting the default value for the start of the time window filter
            FilterTimeWindowStartDatePicker.DisplayDate = DateTime.Now;

            // Refreshing messages and topics
            ReceivedMessagesDataGrid.Items.Clear();
            RefreshMetadataAndTopics(onlyAddNewMessages: false);
        }
        
        #endregion Other members


        #region Nested types

        /// <summary>
        /// Maps data to be shown in the messages datagrid.
        /// </summary>
        private class MessageDataRow
        {
            public MessageDataRow()
            {
                // Empty ctor body
            }

            public MessageDataRow(Metadata metadata)
            {
                RunningId = metadata.RunningId.ToString();
                Time = metadata.ReceivedAt.ToLocalTime().ToString("yyyy-MM-dd HH\\:mm\\:ss,f"); // "\\" is for escape
                Topic = metadata.Topic;
                Name = metadata.Name;
                Payload = metadata.PayloadSummary;
                PayloadType = metadata.PayloadType;
                Filepath = metadata.Filepath;
            }

            // Read only properties do not work in datagrid? Thus, specifying setters
            // although not needed for anything in the source code.
            public string RunningId { get; set; }
            public string Time { get; set; }
            public string Topic { get; set; }
            public string Name { get; set; }
            public string Payload { get; set; }
            public PayloadTypeType PayloadType { get; set; }
            public string Filepath { get; set; }
        }
        
        #endregion Nested types
    }
}
