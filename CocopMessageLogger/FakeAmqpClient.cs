//
// Please make sure to read and understand README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Authors: Petri Kannisto and Tapio Vaaranmaa, Tampere University, Finland
// File created: 11/2019
// Last modified: 3/2020

using System;
using MsgMeas = Cocop.MessageSerialiser.Meas;
using MsgBiz = Cocop.MessageSerialiser.Biz;

namespace CocopMessageLogger
{
    /// <summary>
    /// Implements a fake AMQP client to test the application.
    /// </summary>
    class FakeAmqpClient : AmqpClientBase
    {
        private static bool s_connectionFailedLastTime = false; // This flag will be used to make the fake connection fail every second time

        private System.Timers.Timer m_timer = null;

        private readonly object m_lockObject = new object();
        private bool m_disposed = false;
        
        private int m_taskId = 4120; // This is incremented to fabricate changing IDs in messages

        /// <summary>
        /// Constructor.
        /// </summary>
        public FakeAmqpClient(ConnectionEventCallback connCb, MessageReceivedCallback msgCb)
            : base(connCb, msgCb)
        {
            // Create timer to send messages periodically
            m_timer = new System.Timers.Timer();
            m_timer.Elapsed += M_timer_Elapsed;
            m_timer.Interval = 4000; // unit: ms
            m_timer.AutoReset = true;
        }

        private void M_timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                int taskId;

                lock (m_lockObject)
                {
                    // Get task ID and increment it for next time
                    taskId = m_taskId;
                    ++m_taskId;
                }

                // This determines what to send
                var taskModulo = taskId % 4;
                byte[] messageToSend;
                
                if (taskModulo == 0)
                {
                    // Send a schedule
                    messageToSend = GetProcessProductionSchedule();
                }
                else
                {
                    // Send an observation
                    messageToSend = GetObservation(taskId);
                }
                
                // Choosing the topic by ID; topic-1..topic-4
                var topic = "topic-" + (taskModulo + 1);
                
                // Invoke the callback if not null
                b_messageReceivedCallback?.Invoke(host: ConnectionRequestObj.Host, exc: ConnectionRequestObj.Exchange,
                    topic: topic, msg: messageToSend);
            }
            catch { }
        }
        

        #region AmqpClientBase

        protected override void DisposeImpl()
        {
            lock (m_lockObject)
            {
                m_disposed = true;
            }

            TerminateImpl(false);
        }

        protected override void ConnectImpl()
        {
            ExpectNotDisposed(); // throws ObjectDisposedException
            
            if (m_timer.Enabled)
            {
                // Already "connected"
                throw new InvalidOperationException("Already connected");
            }
            
            bool failNow = false;

            // Simulate connecting failing every second time
            lock (m_lockObject)
            {
                failNow = !s_connectionFailedLastTime;
                s_connectionFailedLastTime = !s_connectionFailedLastTime;
            }

            InvokeConnectionEvent(ConnectionEventType.Connecting, true);

            if (failNow)
            {
                try
                {
                    var exception = new Exception("Fabricated error for testing");
                    SendConnectionEvent(connMaint: false, evType: ConnectionEventType.ConnectingFailed, exc: exception);
                }
                catch { }
            }
            else
            {
                m_timer.Enabled = true;
                InvokeConnectionEvent(ConnectionEventType.Connected, true);
            }
        }

        public override void Terminate()
        {
            TerminateImpl(true);
        }

        #endregion IAmqpClient


        #region Private methods

        private byte[] GetObservation(int taskId)
        {
            // Fabricating a message
            var dataRecord = new MsgMeas.Item_DataRecord()
                {
                    { "TaskId", new MsgMeas.Item_Text(taskId.ToString()) },
                    { "SomeField", new MsgMeas.Item_Category("my-category") }
                };
            var observation = new MsgMeas.Observation(dataRecord)
            {
                Name = "Task " + taskId
            };

            return observation.ToXmlBytes();
        }

        private byte[] GetProcessProductionSchedule()
        {
            try
            {
                // Fabricating a message
                var baseDateTime = DateTime.Now.ToUniversalTime().AddMinutes(30);

                var materialReq = new MsgBiz.MaterialRequirement()
                {
                    MaterialDefinitionIdentifiers = new System.Collections.Generic.List<MsgBiz.IdentifierType>
                    {
                        new MsgBiz.IdentifierType("material_x")
                    },
                    MaterialLotIdentifiers = new System.Collections.Generic.List<MsgBiz.IdentifierType>
                    {
                        new MsgBiz.IdentifierType("batch-119")
                    },
                    MaterialUse = new MsgBiz.MaterialUse(MsgBiz.MaterialUseType.Produced),
                    Quantities = new System.Collections.Generic.List<MsgBiz.QuantityValue>
                    {
                        new MsgBiz.QuantityValue(41.9) { UnitOfMeasure = "t/h" }
                    }
                };

                var messageObj = new MsgBiz.ProcessProductionSchedule()
                {
                    ProductionSchedules = new System.Collections.Generic.List<MsgBiz.ProductionSchedule>
                    {
                        new MsgBiz.ProductionSchedule()
                        {
                            ProductionRequests = new System.Collections.Generic.List<MsgBiz.ProductionRequest>
                            {
                                new MsgBiz.ProductionRequest()
                                {
                                    Identifier = new MsgBiz.IdentifierType("my-identifier-1"),
                                    HierarchyScopeObj = new MsgBiz.HierarchyScope(
                                        new MsgBiz.IdentifierType("process_a"),
                                        MsgBiz.EquipmentElementLevelType.ProcessCell
                                        ),
                                    SegmentRequirements = new System.Collections.Generic.List<MsgBiz.SegmentRequirement>
                                    {
                                        new MsgBiz.SegmentRequirement()
                                        {
                                            EarliestStartTime = baseDateTime.AddMinutes(1),
                                            LatestEndTime = baseDateTime.AddMinutes(30),
                                            MaterialRequirements = new System.Collections.Generic.List<MsgBiz.MaterialRequirement>
                                            { materialReq }
                                        },
                                        new MsgBiz.SegmentRequirement()
                                        {
                                            EarliestStartTime = baseDateTime.AddMinutes(31),
                                            LatestEndTime = baseDateTime.AddMinutes(60)
                                        }
                                    }
                                },
                                new MsgBiz.ProductionRequest()
                                {
                                    Identifier = new MsgBiz.IdentifierType("my-identifier-2")
                                }
                            }
                        }
                    }
                };

                // String to bytes
                return messageObj.ToXmlBytes();
            }
            catch
            {
                // Fallback: use empty message
                return new byte[0];
            }
        }

        private void TerminateImpl(bool calledByUser)
        {
            if (m_timer != null)
            {
                try
                {
                    if (calledByUser)
                    {
                        InvokeConnectionEvent(ConnectionEventType.TerminatingConnectionByUser, true);
                    }

                    if (m_timer.Enabled)
                    {
                        m_timer.Enabled = false;
                    }

                    m_timer.Dispose();
                    m_timer = null;
                    
                    if (calledByUser)
                    {
                        InvokeConnectionEvent(ConnectionEventType.ConnectionTerminatedByUser, false);
                    }
                }
                catch { }
            }
        }

        private void ExpectNotDisposed()
        {
            lock (m_lockObject)
            {
                if (m_disposed)
                {
                    throw new ObjectDisposedException("FakeAmqpClient");
                }
            }
        }

        private void InvokeConnectionEvent(ConnectionEventType evCtg, bool connectionMaintained)
        {
            SendConnectionEvent(connMaint: connectionMaintained, evType: evCtg);
        }

        #endregion Private methods
    }
}
