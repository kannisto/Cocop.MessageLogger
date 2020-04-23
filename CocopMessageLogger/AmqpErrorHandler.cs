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
using Rmq = RabbitMQ.Client;

namespace CocopMessageLogger
{
    /// <summary>
    /// Handles AMQP errors.
    /// </summary>
    static class AmqpErrorHandler
    {
        /// <summary>
        /// Resolves the reason for an error reason that has resulted from an
        /// AMQP-related operation. Use this when there was an error while
        /// connecting to a broker or trying to declare an AMQP exchange.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <returns>Suspected error reason.</returns>
        public static ErrorReasonType GetErrorReason(Exception exception)
        {
            if (exception is Rmq.Exceptions.OperationInterruptedException)
            {
                return HandleOperationInterruptedException((Rmq.Exceptions.OperationInterruptedException)exception);
            }
            else if (exception is Rmq.Exceptions.BrokerUnreachableException)
            {
                return HandleBrokerUnreachableException((Rmq.Exceptions.BrokerUnreachableException)exception);
            }
            else
            {
                return ErrorReasonType.Other;
            }
        }

        private static ErrorReasonType HandleOperationInterruptedException(Rmq.Exceptions.OperationInterruptedException exception)
        {
            if (exception.ShutdownReason == null)
            {
                return ErrorReasonType.Broker_Other;
            }

            switch (exception.ShutdownReason.ReplyCode)
            {
                case Rmq.Framing.Constants.AccessRefused:

                    // The client tries to access a resource (an exchange?) that
                    // it does not have permissions for.
                    return ErrorReasonType.Broker_Permissions;

                default:

                    return ErrorReasonType.Broker_Other;
            }
        }

        private static ErrorReasonType HandleBrokerUnreachableException(Rmq.Exceptions.BrokerUnreachableException exception)
        {
            var innerException = exception.InnerException;

            if (innerException == null)
            {
                return ErrorReasonType.Other;
            }
            else if (innerException is Rmq.Exceptions.ConnectFailureException)
            {
                // This exception type indicates a network-related error.
                return HandleConnectFailureException((Rmq.Exceptions.ConnectFailureException)innerException);
            }
            else if (innerException is Rmq.Exceptions.AuthenticationFailureException)
            {
                // This type of exception comes when authentication fails at the AMQP server.
                return ErrorReasonType.Broker_Credentials;
            }
            else if (innerException is System.Security.Authentication.AuthenticationException)
            {
                // If the authentication error does not come from the AMQP server, suspecting (server) certificate.
                return ErrorReasonType.Certificate;
            }
            else
            {
                return ErrorReasonType.Other;
            }
        }

        private static ErrorReasonType HandleConnectFailureException(Rmq.Exceptions.ConnectFailureException exception)
        {
            var innerEx = exception.InnerException;

            if (innerEx == null || !(innerEx is System.Net.Sockets.SocketException))
            {
                return ErrorReasonType.Network_Other;
            }

            var socketEx = (System.Net.Sockets.SocketException)innerEx;

            switch (socketEx.SocketErrorCode)
            {
                case System.Net.Sockets.SocketError.HostDown:
                case System.Net.Sockets.SocketError.HostNotFound:
                case System.Net.Sockets.SocketError.HostUnreachable:
                case System.Net.Sockets.SocketError.NetworkDown:
                case System.Net.Sockets.SocketError.NetworkUnreachable:

                    // Either the host is not found or the network is down.
                    return ErrorReasonType.Network_NotReachable;

                case System.Net.Sockets.SocketError.ConnectionRefused:

                    // This occurs at least when the server expects a secure connection
                    // but the client do not use such.
                    return ErrorReasonType.Network_ConnectionRefused;

                default:

                    return ErrorReasonType.Network_Other;
            }
        }
    }
}
