namespace ScrewKing
{
    // Request tickets reject late callbacks, including callbacks from a previous round.
    public sealed class ScrewKingRoundReward
    {
        public bool BaseGranted { get; private set; }
        public bool Doubled { get; private set; }
        public bool Pending { get; private set; }
        int ticket;
        public void Reset(){ticket++;BaseGranted=false;Doubled=false;Pending=false;}
        public bool GrantBase(){if(BaseGranted)return false;BaseGranted=true;return true;}
        public bool BeginDouble(out int request){request=ticket;if(!BaseGranted||Doubled||Pending)return false;Pending=true;request=++ticket;return true;}
        public bool CompleteDouble(int request,bool completed)
        {
            if(!Pending||request!=ticket)return false;
            Pending=false;if(!completed||Doubled)return false;Doubled=true;return true;
        }
    }
}
