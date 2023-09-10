using Dalamud.Interface;
using Dalamud.Interface.Internal.Notifications;

using Ktisis.Core.Impl;

namespace Ktisis.Core.Services; 

[KtisisService]
public class NotifyService {
	// Service

	private readonly UiBuilder _uiBuilder;

	public NotifyService(UiBuilder _uiBuilder) {
		this._uiBuilder = _uiBuilder;
	}

	// Notifier

	private const uint DefaultTimerMs = 10000;

	public void Notify(NotificationType type, string text, uint timer = DefaultTimerMs)
		=> this._uiBuilder.AddNotification(text, Ktisis.VersionName, type, timer);

	public void Notify(string text, uint timer = DefaultTimerMs)
		=> Notify(NotificationType.None, text, timer);

	public void Success(string text, uint timer = DefaultTimerMs)
		=> Notify(NotificationType.Success, text, timer);

	public void Error(string text, uint timer = DefaultTimerMs)
		=> Notify(NotificationType.Error, text, timer);

	public void Warning(string text, uint timer = DefaultTimerMs)
		=> Notify(NotificationType.Warning, text, timer);

	public void Info(string text, uint timer = DefaultTimerMs)
		=> Notify(NotificationType.Info, text, timer);
}
