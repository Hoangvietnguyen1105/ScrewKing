namespace ScrewKing.Editor
{
    public static class TikTokCapabilitiesFinalVerification
    {
        public static void Run()
        {
            TikTokCapabilitiesBuilder.CreateAndVerify();
            TikTokBuilder.ValidateTikTokPlayer();
            TikTokBuilder.BuildBrowserRelease();
        }
    }
}
