﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Channels;
using Melville.IOC.IocContainers.ChildContainers;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Compact;
using Serilog.Formatting.Compact.Reader;
using Serilog.Parsing;

namespace Melville.Log.Viewer.EventSink
{
    public class NamedPipeEventSink: ILogEventSink
    {
        private Channel<LogEvent> buffer = Channel.CreateBounded<LogEvent>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = true
        });

        private INamedPipeWriterProtocol targetProtocol;
        private ITextFormatter format = new CompactJsonFormatter();
        
        public NamedPipeEventSink(INamedPipeWriterProtocol targetProtocol)
        {
            this.targetProtocol = targetProtocol;
            TryConnectToServer();
        }

        private async void TryConnectToServer()
        {
             await targetProtocol.Connect();
             var localLogger = new LoggerConfiguration().WriteTo.Sink(this, LogEventLevel.Verbose).CreateLogger();
             localLogger.Information("Source Process: {AssignProcessName}",
                 Assembly.GetEntryAssembly()?.GetName().Name ??"No Name");
             while (true)
             {
                 var logEvent = await buffer.Reader.ReadAsync();
                 using var ms = new MemoryStream();
                 using var write = new StreamWriter(ms);
                 format.Format(logEvent, write);
                 write.Flush();
                 await targetProtocol.Write(ms.GetBuffer(), 0, (int) ms.Length);
             }
        }

        public void Emit(LogEvent logEvent)
        {
            buffer.Writer.TryWrite(logEvent);
        }
    }
}