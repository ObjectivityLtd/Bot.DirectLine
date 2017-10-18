namespace Objectivity.Bot.DirectLine
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Bot.Connector.DirectLine;

    public class BotToUserMessagesEventArgs : EventArgs
    {
        public BotToUserMessagesEventArgs(IList<Activity> activities)
        {
            this.Messages = activities;
        }

        public IList<Activity> Messages { get; }
    }
}
