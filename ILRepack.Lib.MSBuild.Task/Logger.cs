﻿using System;
using System.IO;
using ILRepacking;

namespace ILRepack.Lib.MSBuild.Task
{
    internal class Logger : ILogger
    {
        private string _outputFile;
        private StreamWriter _writer;

        public bool ShouldLogVerbose { get; set; }

        public void Error(string msg)
        {
            Log($"ERROR: {msg}");
        }

        public void Warn(string msg)
        {
            Log($"WARN: {msg}");
        }

        public void Info(string msg)
        {
            Log($"INFO: {msg}");
        }

        public void Verbose(string msg)
        {
            if (ShouldLogVerbose)
            {
                Log($"VERBOSE: {msg}");
            }
        }

        public void Log(object str)
        {
            var logStr = str.ToString();
            Console.WriteLine(logStr);
            _writer?.WriteLine(logStr);
        }

        public bool Open(string file)
        {
            if (string.IsNullOrEmpty(file))
            {
                return false;
            }

            _outputFile = file;
            _writer = new StreamWriter(_outputFile);
            return true;
        }

        public void Close()
        {
            if (_writer == null)
            {
                return;
            }

            _writer.Close();
            _writer = null;
        }

        public void DuplicateIgnored(string ignoredType, object ignoredObject)
        {
            // TODO: put on a list and log a summary
            //INFO("Ignoring duplicate " + ignoredType + " " + ignoredObject);
        }
    }
}