﻿//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace HostingAdapter.Utility
{
    /// <summary>
    /// Defines the level indicators for log messages.
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Indicates a verbose log message.
        /// </summary>
        Verbose,

        /// <summary>
        /// Indicates a normal, non-verbose log message.
        /// </summary>
        Normal,

        /// <summary>
        /// Indicates a warning message.
        /// </summary>
        Warning,

        /// <summary>
        /// Indicates an error message.
        /// </summary>
        Error
    }

    /// <summary>
    /// Provides a simple logging interface.  May be replaced with a
    /// more robust solution at a later date.
    /// </summary>
    public static class Logger
    {
        private static LogWriter logWriter;

        private static bool isEnabled;

        private static bool isInitialized = false;

        /// <summary>
        /// Initializes the Logger for the current session.
        /// </summary>
        /// <param name="logFilePath">
        /// Optional. Specifies the path at which log messages will be written.
        /// </param>
        /// <param name="minimumLogLevel">
        /// Optional. Specifies the minimum log message level to write to the log file.
        /// </param>
        public static void Initialize(
            string logFilePath,
            LogLevel minimumLogLevel = LogLevel.Normal,
            bool isEnabled = true)
        {
            Logger.isEnabled = isEnabled;

            // return if the logger is not enabled or already initialized
            if (!Logger.isEnabled || Logger.isInitialized)
            {
                return;
            }

            Logger.isInitialized = true;

            // get a unique number to prevent conflicts of two process launching at the same time
            int uniqueId;
            try
            {
                uniqueId = Process.GetCurrentProcess().Id;
            }
            catch (Exception)
            {
                // if the pid look up fails for any reason, just use a random number
                uniqueId = new Random().Next(1000, 9999);
            }

            // make the log path unique
            string fullFileName = string.Format(
                "{0}_{1,4:D4}{2,2:D2}{3,2:D2}{4,2:D2}{5,2:D2}{6,2:D2}{7}.log",
                logFilePath,
                DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second,
                uniqueId);

            if (logWriter != null)
            {
                logWriter.Dispose();
            }

            // TODO: Parameterize this
            logWriter =
                new LogWriter(
                    minimumLogLevel,
                    fullFileName,
                    true);

            Logger.Write(LogLevel.Normal, "Initializing SQL Tools Service Host logger");
        }

        /// <summary>
        /// Closes the Logger.
        /// </summary>
        public static void Close()
        {
            if (logWriter != null)
            {
                logWriter.Dispose();
            }
        }

        /// <summary>
        /// Writes a message to the log file.
        /// </summary>
        /// <param name="logLevel">The level at which the message will be written.</param>
        /// <param name="logMessage">The message text to be written.</param>
        /// <param name="callerName">The name of the calling method.</param>
        /// <param name="callerSourceFile">The source file path where the calling method exists.</param>
        /// <param name="callerLineNumber">The line number of the calling method.</param>
        public static void Write(
            LogLevel logLevel,
            string logMessage,
            [CallerMemberName] string callerName = null,
            [CallerFilePath] string callerSourceFile = null,
            [CallerLineNumber] int callerLineNumber = 0)
        {
            // return if the logger is not enabled or not initialized
            if (!Logger.isEnabled || !Logger.isInitialized)
            {
                return;
            }

            if (logWriter != null)
            {
                logWriter.Write(
                    logLevel,
                    logMessage,
                    callerName,
                    callerSourceFile,
                    callerLineNumber);
            }
        }
    }

    internal class LogWriter : IDisposable
    {
        private object logLock = new object();
        private TextWriter textWriter;
        private LogLevel minimumLogLevel = LogLevel.Verbose;

        public LogWriter(LogLevel minimumLogLevel, string logFilePath, bool deleteExisting)
        {
            this.minimumLogLevel = minimumLogLevel;

            // Ensure that we have a usable log file path
            if (!Path.IsPathRooted(logFilePath))
            {
                logFilePath =
                    Path.Combine(
                        AppContext.BaseDirectory,
                        logFilePath);
            }


            if (!this.TryOpenLogFile(logFilePath, deleteExisting))
            {
                // If the log file couldn't be opened at this location,
                // try opening it in a more reliable path
                this.TryOpenLogFile(
                    Path.Combine(
                        Environment.GetEnvironmentVariable("TEMP"),
                        Path.GetFileName(logFilePath)),
                    deleteExisting);
            }
        }

        public void Write(
            LogLevel logLevel,
            string logMessage,
            string callerName = null,
            string callerSourceFile = null,
            int callerLineNumber = 0)
        {
            if (this.textWriter != null &&
                logLevel >= this.minimumLogLevel)
            {
                // System.IO is not thread safe
                lock (this.logLock)
                {
                    // Print the timestamp and log level
                    this.textWriter.WriteLine(
                        "{0} [{1}] - Method \"{2}\" at line {3} of {4}\r\n",
                        DateTime.Now,
                        logLevel.ToString().ToUpper(),
                        callerName,
                        callerLineNumber,
                        callerSourceFile);

                    // Print out indented message lines
                    foreach (var messageLine in logMessage.Split('\n'))
                    {
                        this.textWriter.WriteLine("    " + messageLine.TrimEnd());
                    }

                    // Finish with a newline and flush the writer
                    this.textWriter.WriteLine();
                    this.textWriter.Flush();
                }
            }
        }

        public void Dispose()
        {
            if (this.textWriter != null)
            {
                this.textWriter.Flush();
                this.textWriter.Dispose();
                this.textWriter = null;
            }
        }

        private bool TryOpenLogFile(
            string logFilePath,
            bool deleteExisting)
        {
            try
            {
                // Make sure the log directory exists
                Directory.CreateDirectory(
                    Path.GetDirectoryName(
                        logFilePath));

                // Open the log file for writing with UTF8 encoding
                this.textWriter =
                    new StreamWriter(
                        new FileStream(
                            logFilePath,
                            deleteExisting ?
                                FileMode.Create :
                                FileMode.Append),
                        Encoding.UTF8);

                return true;
            }
            catch (Exception e)
            {
                if (e is UnauthorizedAccessException ||
                    e is IOException)
                {
                    // This exception is thrown when we can't open the file
                    // at the path in logFilePath.  Return false to indicate
                    // that the log file couldn't be created.
                    return false;
                }

                // Unexpected exception, rethrow it
                throw;
            }
        }
    }
}
