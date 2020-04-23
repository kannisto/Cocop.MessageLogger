//
// Please make sure to read and understand README.md and LICENSE.txt.
// 
// This file was prepared in the research project COCOP (Coordinating
// Optimisation of Complex Industrial Processes).
// https://cocop-spire.eu/
//
// Author: Petri Kannisto, Tampere University, Finland
// File created: 3/2020
// Last modified: 3/2020

using System;
using SysColl = System.Collections.Generic;
using Rmq = RabbitMQ.Client;
using MsgMeas = Cocop.MessageSerialiser.Meas;
using MsgBiz = Cocop.MessageSerialiser.Biz;

namespace AmqpDataSourceForTest
{
    /// <summary>
    /// Implements the application workflow.
    /// </summary>
    class Workflow
    {
        private const string ExchangeName = "cocoptest-msgloggertest";
        private const string TopicNameStart = "fabr.";
        private const string TopicNameTankState = TopicNameStart + "t300.state";
        private const string TopicNameSchedules = TopicNameStart + "schedule";

        private readonly Ui m_ui;
        private readonly string m_hostName;
        private readonly bool m_secure;
        private readonly string m_username;
        private readonly string m_password;

        // This generates random values
        private readonly Random m_randomizer;

        // These are the state of the imaginary tank
        private double m_tankTemperature = 23.5;
        private double m_tankLiquidLevel = 45.2;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="ui">User interface.</param>
        /// <param name="host">AMQP host.</param>
        /// <param name="sec">Whether to use a secure connection.</param>
        /// <param name="user">Username.</param>
        /// <param name="pwd">Password.</param>
        public Workflow(Ui ui, string host, bool sec, string user, string pwd)
        {
            m_ui = ui;

            m_hostName = host;
            m_secure = sec;
            m_username = user;
            m_password = pwd;

            m_randomizer = new Random();
        }

        /// <summary>
        /// Runs the workflow.
        /// </summary>
        /// <returns>False if the operation fails, otherwise true.</returns>
        public bool Run()
        {
            // Setting up message bus connection

            var factory = new Rmq.ConnectionFactory()
            {
                HostName = m_hostName,
                UserName = m_username,
                Password = m_password
            };

            if (m_secure)
            {
                // Apply secure communication (TLS 1.2)
                factory.Ssl.Enabled = true;
                factory.Ssl.ServerName = m_hostName;
                factory.Ssl.Version = System.Security.Authentication.SslProtocols.Tls12;
            }

            try
            {
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    // Declaring an exchange for topic-based communication
                    channel.ExchangeDeclare(exchange: ExchangeName, type: "topic", durable: true, autoDelete: false, arguments: null);

                    m_ui.PrintLine("The exchange is " + ExchangeName);

                    // Listening to user commands
                    PerformCommands(channel);
                }

                return true;
            }
            catch (Rmq.Exceptions.OperationInterruptedException e)
            {
                m_ui.PrintLine("Operation interrupted: " + e.Message);
                return false;
            }
            catch (Rmq.Exceptions.BrokerUnreachableException e)
            {
                m_ui.PrintLine("Broker unreachable: " + e.Message);
                return false;
            }
        }
        
        private void PerformCommands(Rmq.IModel channel)
        {
            const string CommandMeas = "M";
            const string CommandSched = "S";
            const string CommandQuit = "Q";
            bool quit = false;
            
            while (!quit)
            {
                m_ui.PrintLine();
                m_ui.PrintLine("Please select an action:");
                m_ui.PrintLine(" M - Send a measurement");
                m_ui.PrintLine(" S - Send a schedule");
                m_ui.PrintLine(" Q - Quit");

                var input = m_ui.ReadLine().ToUpper();

                // Performing the requested action
                switch (input)
                {
                    case CommandMeas:
                        SendMeasurement(channel);
                        break;

                    case CommandSched:
                        SendSchedule(channel);
                        break;

                    case CommandQuit:
                        quit = true;
                        break;

                    default:
                        m_ui.PrintLine("Unknown action. Please try again.");
                        break;
                }
            }

            m_ui.PrintLine("Quitting.");
        }


        #region SendMeasurement

        private void SendMeasurement(Rmq.IModel channel)
        {
            // In this example, sending an imaginary tank state that consists of a few measurements.

            // Changing tank measurements randomly.
            m_tankTemperature = ChangeValueRandomly(m_tankTemperature, 6, 19, 49);
            m_tankLiquidLevel = ChangeValueRandomly(m_tankLiquidLevel, 27, 12, 100);

            // Creating state object.
            var tankStateValues = new MsgMeas.Item_DataRecord()
            {
                // Adding measurements.
                { "TI-300", new MsgMeas.Item_Measurement("Cel", m_tankTemperature) },
                { "LI-300", new MsgMeas.Item_Measurement("cm", m_tankLiquidLevel) },

                // Adding state.
                { "state",  new MsgMeas.Item_Category(GetRandomTankState()) },

                // Adding alarms. These alarms depend on the level value.
                { "LA-300", new MsgMeas.Item_Boolean(m_tankLiquidLevel < 30) },
                { "LA+300", new MsgMeas.Item_Boolean(m_tankLiquidLevel > 70) }
            };

            // Creating an Observation to enable encoding to XML and adding metadata.
            var timestamp = DateTime.Now;

            var observation = new MsgMeas.Observation(tankStateValues)
            {
                Name = "T300",
                Description = "State of T300 at " + timestamp.ToString(),
                FeatureOfInterest = "t300",
                ObservedProperty = "state",
                // By default, PhenomenonTime is always the creation time, but assigning it explicitly
                PhenomenonTime = timestamp.ToUniversalTime()
            };

            // Sending the message to the message bus.
            SendToMessageBus(channel, TopicNameTankState, observation.ToXmlBytes());
        }
        
        private string GetRandomTankState()
        {
            // Mapping a random nr within 0..2 to a state
            switch (m_randomizer.Next(0, 3))
            {
                case 0:
                    return "load";
                case 1:
                    return "unload";
                default:
                    return "idle";
            }
        }
        
        private double ChangeValueRandomly(double current, int maxChange, int min, int max)
        {
            current = current + m_randomizer.Next(-maxChange, maxChange + 1);

            // Make sure the limits are not exceeded
            if (current < min)
            {
                current = min;
            }
            if (current > max)
            {
                current = max;
            }

            return current;
        }

        #endregion SendMeasurement


        #region SendSchedule

        private void SendSchedule(Rmq.IModel channel)
        {
            // Creating an imaginary schedule for two tanks, T300 and T400.
            
            // It helps to create a "base DateTime", because the message API requires all DateTimes in UTC
            var dateTimeBase = DateTime.Now.ToUniversalTime();

            // Creating material requirements for the production requests
            var matReqLoad = CreateMaterialReqForSchedule(MsgBiz.MaterialUseType.Consumed, "liquid_422", 1.3);
            var matReqUnload = CreateMaterialReqForSchedule(MsgBiz.MaterialUseType.Produced, "liquid_429", 1.1);

            // Creating scheduling parameters
            var schedulingParams = new MsgMeas.Item_DataRecord()
            {
                { "scheduling-method", new MsgMeas.Item_Category("default")  },
                { "scheduling-horizon", new MsgMeas.Item_Measurement("min", 120)  }
            };

            // Creating production requests
            var prodReqT300 = new MsgBiz.ProductionRequest()
            {
                HierarchyScopeObj = new MsgBiz.HierarchyScope(
                    new MsgBiz.IdentifierType("T300"),
                    MsgBiz.EquipmentElementLevelType.ProcessCell
                    ),

                SegmentRequirements = new SysColl.List<MsgBiz.SegmentRequirement>()
                {
                    CreateSegmentForSchedule(dateTimeBase.AddMinutes(29), dateTimeBase.AddMinutes(49), "load", matReqLoad),
                    CreateSegmentForSchedule(dateTimeBase.AddMinutes(62), dateTimeBase.AddMinutes(82), "unload", matReqUnload),
                },
                
                SchedulingParameters = schedulingParams.ToDataRecordPropertyProxy()
            };
            var prodReqT400 = new MsgBiz.ProductionRequest()
            {
                HierarchyScopeObj = new MsgBiz.HierarchyScope(
                    new MsgBiz.IdentifierType("T400"),
                    MsgBiz.EquipmentElementLevelType.ProcessCell
                    ),

                SegmentRequirements = new SysColl.List<MsgBiz.SegmentRequirement>()
                {
                    CreateSegmentForSchedule(dateTimeBase.AddMinutes(19), dateTimeBase.AddMinutes(39), "load", matReqLoad),
                    CreateSegmentForSchedule(dateTimeBase.AddMinutes(52), dateTimeBase.AddMinutes(72), "unload", matReqUnload),
                },

                SchedulingParameters = schedulingParams.ToDataRecordPropertyProxy()
            };

            // Creating a request to process the schedule
            var processScheduleRequest = new MsgBiz.ProcessProductionSchedule()
            {
                // This is the current time by default, but setting it explicitly
                CreationDateTime = DateTime.Now.ToUniversalTime(),

                ProductionSchedules = new SysColl.List<MsgBiz.ProductionSchedule>
                {
                    new MsgBiz.ProductionSchedule()
                }
            };

            // Adding the production requests to the message
            processScheduleRequest.ProductionSchedules[0].ProductionRequests.Add(prodReqT300);
            processScheduleRequest.ProductionSchedules[0].ProductionRequests.Add(prodReqT400);

            // Sending the message to the message bus
            SendToMessageBus(channel, TopicNameSchedules, processScheduleRequest.ToXmlBytes());
        }

        private MsgBiz.SegmentRequirement CreateSegmentForSchedule(DateTime start, DateTime end, string id, MsgBiz.MaterialRequirement matReq)
        {
            return new MsgBiz.SegmentRequirement()
            {
                EarliestStartTime = start,
                LatestEndTime = end,
                ProcessSegmentIdentifier = new MsgBiz.IdentifierType(id),

                MaterialRequirements = new SysColl.List<MsgBiz.MaterialRequirement>()
                { matReq }
            };
        }

        private MsgBiz.MaterialRequirement CreateMaterialReqForSchedule(MsgBiz.MaterialUseType use, string matId, double volume)
        {
            return new MsgBiz.MaterialRequirement()
            {
                // How used
                MaterialUse = new MsgBiz.MaterialUse(use),

                // What used
                MaterialDefinitionIdentifiers = new SysColl.List<MsgBiz.IdentifierType>()
                {
                    new MsgBiz.IdentifierType(matId)
                },

                // How much
                Quantities = new SysColl.List<MsgBiz.QuantityValue>()
                {
                    new MsgBiz.QuantityValue(volume)
                    {
                        UnitOfMeasure = "m3"
                    }
                }
            };
        }

        #endregion SendSchedule


        private void SendToMessageBus(Rmq.IModel channel, string topic, byte[] message)
        {
            var props = channel.CreateBasicProperties();
            props.Expiration = (5 * 60 * 1000).ToString(); // The message expires after 5 minutes

            // Send the message to the topic
            channel.BasicPublish(exchange: ExchangeName,
                                 routingKey: topic,
                                 mandatory: false,
                                 basicProperties: props,
                                 body: message);

            var msgToUi = string.Format("{0} Sent a message to topic {1}", DateTime.Now.ToString("HH:mm:ss"), topic);
            m_ui.PrintLine(msgToUi);
        }
    }
}
