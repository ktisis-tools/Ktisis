using System;
using System.Collections.Generic;

using Dalamud.Plugin.Services;

using Ktisis.Core.Attributes;

using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Ktisis.Services.Plugin;

[Singleton]
public class LoggingService {

	public IPluginLog DalamudLog { get; private set; } = null!;
	public ILogger Logger { get; }
	public Queue<string> Logs { get; } = new();


	private readonly LoggingLevelSwitch levelSwitch;


	public LoggingService(
		IPluginLog logger
	) {

		this.DalamudLog = logger;

		this.levelSwitch = new LoggingLevelSwitch(this.GetDefaultLevel());

		var loggerConfiguration = new LoggerConfiguration()
			.Enrich.WithProperty("Dalamud.PluginName", "Ktisis")
			.MinimumLevel.ControlledBy(this.levelSwitch)
			.WriteTo.Logger(Log.Logger);

		this.Logger = loggerConfiguration.CreateLogger();

	}

	public void Fatal(string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Fatal, null, messageTemplate, values);

	public void Fatal(Exception? exception, string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Fatal, exception, messageTemplate, values);

	public void Error(string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Error, null, messageTemplate, values);

	public void Error(Exception? exception, string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Error, exception, messageTemplate, values);

	public void Warning(string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Warning, null, messageTemplate, values);

	public void Warning(Exception? exception, string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Warning, exception, messageTemplate, values);

	public void Information(string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Information, null, messageTemplate, values);

	public void Information(Exception? exception, string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Information, exception, messageTemplate, values);

	public void Info(string messageTemplate, params object[] values) =>
		this.Information(messageTemplate, values);

	public void Info(Exception? exception, string messageTemplate, params object[] values) =>
		this.Information(exception, messageTemplate, values);

	public void Debug(string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Debug, null, messageTemplate, values);

	public void Debug(Exception? exception, string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Debug, exception, messageTemplate, values);

	public void Verbose(string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Verbose, null, messageTemplate, values);

	public void Verbose(Exception? exception, string messageTemplate, params object[] values) =>
		this.Write(LogEventLevel.Verbose, exception, messageTemplate, values);

	public void Write(LogEventLevel level, Exception? exception, string messageTemplate, params object[] values) {

		if (this.Logs.Count >= 50)
			for (var i = this.Logs.Count; i >= 50; i--)
				this.Logs.Dequeue();

		this.Logs.Enqueue($"{level} | {DateTime.Now}   : {messageTemplate}\n");

		this.DalamudLog.Write(
			level,
			exception,
			$"{messageTemplate}",
			values);
	}
	private LogEventLevel GetDefaultLevel() => LogEventLevel.Verbose;

}
