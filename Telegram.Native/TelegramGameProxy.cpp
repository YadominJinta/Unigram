#include "pch.h"
#include "TelegramGameProxy.h"
#if __has_include("TelegramGameProxy.g.cpp")
#include "TelegramGameProxy.g.cpp"
#endif

namespace winrt::Telegram::Native::implementation
{
	TelegramGameProxy::TelegramGameProxy(TelegramGameProxyDelegate delegate)
	{
		_delegate = delegate;
	}

	void TelegramGameProxy::PostEvent(hstring eventName, hstring eventData)
	{
		if (eventName == L"share_game")
		{
			_delegate(false);
		}
		else if (eventName == L"share_score")
		{
			_delegate(true);
		}
	}
} // namespace winrt::Telegram::Native::implementation