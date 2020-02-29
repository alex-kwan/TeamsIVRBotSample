namespace ThoughtStuff.TeamsSamples.IVRBotSample
{
    extern alias BetaLib;
    using Beta = BetaLib.Microsoft.Graph;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Graph;
    using Microsoft.Graph.Communications.Calls;
    using Microsoft.Graph.Communications.Common.Telemetry;
    using Microsoft.Graph.Communications.Resources;

    /// <summary>
    /// Base class for call handler for event handling, logging and cleanup.
    /// </summary>
    public class CallHandler : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallHandler"/> class.
        /// </summary>
        /// <param name="bot">The bot</param>
        /// <param name="call">The call</param>
        public CallHandler(Bot bot, ICall call)
        {
            this.Bot = bot;
            this.Call = call;

            // Use the call GraphLogger so we carry the call/correlation context in each log record.
            this.Logger = call.GraphLogger.CreateShim(component: this.GetType().Name);

            var outcome = Serializer.SerializeObject(call.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Call Created:\n" + outcome);

            this.Call.OnUpdated += this.OnCallUpdated;
            this.Call.Participants.OnUpdated += this.OnParticipantsUpdated;
        }

        /// <summary>
        /// Gets the call interface
        /// </summary>
        public ICall Call { get; }

        /// <summary>
        /// Gets the outcomes log - maintained for easy checking of async server responses
        /// </summary>
        /// <value>
        /// The outcomes log.
        /// </value>
        public LinkedList<string> OutcomesLogMostRecentFirst { get; } = new LinkedList<string>();

        /// <summary>
        /// Gets the bot
        /// </summary>
        protected Bot Bot { get; }

        /// <summary>
        /// Gets the logger
        /// </summary>
        protected IGraphLogger Logger { get; }

        /// <summary>
        /// Gets the serializer
        /// </summary>
        private static Serializer Serializer { get; } = new Serializer();

        /// <inheritdoc />
        public void Dispose()
        {
            this.Call.OnUpdated -= this.OnCallUpdated;
            this.Call.Participants.OnUpdated -= this.OnParticipantsUpdated;

            foreach (var participant in this.Call.Participants)
            {
                participant.OnUpdated -= this.OnParticipantUpdated;
            }
        }

        /// <summary>
        /// The event handler when call is updated.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguments</param>
        protected virtual void CallOnUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            // do nothing in base class.
        }

        /// <summary>
        /// The event handler when participants are updated.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguments</param>
        protected virtual void ParticipantsOnUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
        {
            // do nothing in base class.
        }

        /// <summary>
        /// Event handler when participan is updated.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguments</param>
        protected virtual void ParticipantOnUpdated(IParticipant sender, ResourceEventArgs<Participant> args)
        {
            // do nothing in base class.
        }

        /// <summary>
        /// Event handler for call updated.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="args">The event arguments.</param>
        private void OnCallUpdated(ICall sender, ResourceEventArgs<Call> args)
        {
            var outcome = Serializer.SerializeObject(sender.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Call Updated:\n" + outcome);

            this.CallOnUpdated(sender, args);
        }

        /// <summary>
        /// Event handler when participan is updated.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguments</param>
        private void OnParticipantUpdated(IParticipant sender, ResourceEventArgs<Participant> args)
        {
            var outcome = Serializer.SerializeObject(sender.Resource);
            this.OutcomesLogMostRecentFirst.AddFirst("Participant Updated:\n" + outcome);

            this.ParticipantOnUpdated(sender, args);
        }

        /// <summary>
        /// The event handler when participants are updated.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The arguments</param>
        private void OnParticipantsUpdated(IParticipantCollection sender, CollectionEventArgs<IParticipant> args)
        {
            foreach (var participant in args.AddedResources)
            {
                var outcome = Serializer.SerializeObject(participant.Resource);
                this.OutcomesLogMostRecentFirst.AddFirst("Participant Added:\n" + outcome);

                participant.OnUpdated += this.OnParticipantUpdated;
            }

            foreach (var participant in args.RemovedResources)
            {
                var outcome = Serializer.SerializeObject(participant.Resource);
                this.OutcomesLogMostRecentFirst.AddFirst("Participant Removed:\n" + outcome);

                participant.OnUpdated -= this.OnParticipantUpdated;
            }

            this.ParticipantsOnUpdated(sender, args);
        }

        internal void SubscribeToTone()
        {
            Task.Run(async () =>
            {
                try
                {
                    
                    await this.Call.SubscribeToToneAsync().ConfigureAwait(false);
                    this.Logger.Info("Started subscribing to tone.");
                }
                catch (Exception ex)
                {
                    this.Logger.Error(ex, $"Failed to subscribe to tone. ");
                    throw;
                }
            });
        }

    }
}
