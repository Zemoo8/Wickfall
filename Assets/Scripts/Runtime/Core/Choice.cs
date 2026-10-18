using System;
using UnityEngine;

namespace Wickfall
{
    /// <summary>A question the world asks the player: a pact, the Roi Creux's fate, the Foyer (G12).</summary>
    public sealed class ChoiceRequest
    {
        public string TitleKey = "";
        public string BodyKey = "";
        public string BodyText;              // already-resolved text, used instead of BodyKey when set
        public string[] OptionKeys = new string[0];
        public bool[] Enabled;               // null = all enabled
        public Action<int> OnChosen;

        public bool IsEnabled(int option) => Enabled == null || option >= Enabled.Length || Enabled[option];
    }

    /// <summary>
    /// The one way choices are made: world objects Ask; the choice view shows it to a person, while bots
    /// and tests answer through the same Resolve.
    /// </summary>
    public static class Choice
    {
        public static ChoiceRequest Pending { get; private set; }
        public static bool IsOpen => Pending != null;

        public static event Action<ChoiceRequest> Opened;
        public static event Action Closed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Pending = null;
            Opened = null;
            Closed = null;
        }

        public static void Ask(ChoiceRequest request)
        {
            if (request == null || IsOpen)
                return;
            Pending = request;
            Opened?.Invoke(request);
        }

        /// <summary>Answers the pending question. A disabled option is refused.</summary>
        public static bool Resolve(int option)
        {
            var request = Pending;
            if (request == null || option < 0 || option >= request.OptionKeys.Length || !request.IsEnabled(option))
                return false;
            Pending = null;
            Closed?.Invoke();
            request.OnChosen?.Invoke(option);
            return true;
        }
    }
}
