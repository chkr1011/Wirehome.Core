﻿using System;

namespace Wirehome.Core.Scheduler;

public sealed class ActiveCountdown
{
    public ActiveCountdown(string uid, Action<CountdownElapsedParameters> callback, object state)
    {
        Uid = uid ?? throw new ArgumentNullException(nameof(uid));
        Callback = callback ?? throw new ArgumentNullException(nameof(callback));
        State = state;
    }

    public Action<CountdownElapsedParameters> Callback { get; }

    public object State { get; }

    public TimeSpan TimeLeft { get; set; }

    public string Uid { get; }
}