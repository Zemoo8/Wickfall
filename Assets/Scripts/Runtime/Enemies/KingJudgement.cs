using Wickfall.Player;
using Wickfall.World;

namespace Wickfall.Enemies
{
    /// <summary>The fallen Roi Creux: interact to judge him — extinguish him or spare him (G12).</summary>
    public sealed class KingJudgement : Interactable
    {
        public BossHollowKing king;

        void Start() => promptKey = "prompt.judge";

        public override bool CanInteract(PlayerController player) =>
            base.CanInteract(player) && king != null && king.Current == BossHollowKing.Mode.Fallen && !Choice.IsOpen;

        public override void Interact(PlayerController player)
        {
            Choice.Ask(new ChoiceRequest
            {
                TitleKey = "king.title",
                BodyKey = "king.body",
                OptionKeys = new[] { "king.extinguish", "king.spare" },
                OnChosen = option => king.Decide(option == 1),
            });
        }
    }
}
