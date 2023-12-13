using Newtonsoft.Json;
using BarRaider.SdTools.Communication;
using BarRaider.SdTools.Communication.Messages;

namespace com.ceridwen.audio
{
	public class SetTriggerDescriptionMessage : IMessage
    {
        #region Private Classes

        private class PayloadClass : IPayload
        {
            [JsonProperty("rotate")]
            public string Rotate { get; private set; }
            [JsonProperty("push")]
            public string Push { get; private set; }
            [JsonProperty("touch")]
            public string Touch { get; private set; }
            [JsonProperty("longTouch")]
            public string LongTouch { get; private set; }

            public PayloadClass(string rotate, string push, string touch, string longTouch)
            {
                this.Rotate = rotate;
                this.Push = push;
                this.Touch = touch;
                this.LongTouch = longTouch;
            }
        }

        #endregion
        
        #region Public Members

        [JsonProperty("event")]
        public string Event { get { return "setTriggerDescription"; } }

        [JsonProperty("context")]
        public string Context { get; private set; }

        #endregion

        #region Private Members

        [JsonProperty("payload")]
        internal IPayload Payload { get; private set; }

        #endregion

        #region Constructors/Destructors

        public SetTriggerDescriptionMessage(string rotate, string push, string touch, string longTouch, string context)
        {
            this.Context = context;
            this.Payload = new PayloadClass(rotate, push, touch, longTouch);
        }

        #endregion

    }
}