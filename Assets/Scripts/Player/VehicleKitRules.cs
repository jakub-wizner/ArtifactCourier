namespace ArtifactCourier.Player
{
    // Tier numbers are the equipped appearance: 0 stock, 1 level 2, 2 level 4, 3 level 6.
    public static class VehicleKitRules
    {
        public static string Description(int vehicle,int tier)
        {
            if(vehicle==0)return tier==1?"ENDURANCE TANK\n40% longer nitro":tier==2?"TWIN BLADES\nSlashes forward and behind":"GHOST RIDER\nBoost grants 0.8s protection\n8s recharge";
            if(vehicle==1)return tier==1?"BULLDOZER\n360-degree slam pushes enemies":tier==2?"REACTIVE ARMOR\nSlam hits restore 12 shield\n5s recharge":"AFTERSHOCK\n360-degree slam echoes\nafter 0.35 seconds";
            return tier==1?"REGENERATOR\nHits return 10% nitro\n1s recharge":tier==2?"INTERFERENCE\nPulse hits disrupt for 1.5s":"CHAIN COIL\nPulse arcs to two extra\nenemies within 6m";
        }
        public static bool InBasicArc(int vehicle,int tier,float forwardDot)
        {return vehicle!=0 || forwardDot>=.2f || (tier==2 && forwardDot<=-.2f);}
    }
}
