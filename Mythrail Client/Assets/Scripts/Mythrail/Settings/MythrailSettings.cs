using Mythrail.Audio;

namespace Mythrail.Settings
{
    public static class MythrailSettings
    {
        public static bool Fullscreen;
        public static float Volume => AudioManager.instance.GetVolumeMultiplier();
        public static int MouseSensitivity;
        public static bool AskToInvite;
        public static bool AlwaysInvite;
        public static bool ShowDeveloperConsole;
        public static bool CompressDeveloperConsole;
    }
}