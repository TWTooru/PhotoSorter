using System.Drawing;

namespace photo.UI
{
    public static class ThemePalette
    {
        // Dark theme
        public static readonly Color DarkBackground = Color.FromArgb(30, 30, 30);
        public static readonly Color DarkSurface = Color.FromArgb(45, 45, 45);
        public static readonly Color DarkButton = Color.FromArgb(217, 60, 60, 60);
        public static readonly Color DarkSplitLine = Color.FromArgb(60, 60, 60);
        public static readonly Color DarkText = Color.White;
        public static readonly Color DarkMutedText = Color.FromArgb(160, 160, 160);

        // Light theme
        public static readonly Color LightBackground = SystemColors.Control;
        public static readonly Color LightSurface = Color.FromArgb(240, 240, 240);
        public static readonly Color LightSplitLine = Color.FromArgb(200, 200, 200);
        public static readonly Color LightText = SystemColors.ControlText;
        public static readonly Color LightMutedText = Color.FromArgb(120, 120, 120);

        // Action Buttons
        public static readonly Color OrganizeGreen = Color.FromArgb(217, 34, 139, 34);
        public static readonly Color PurgeRed = Color.FromArgb(217, 220, 50, 50);
        public static readonly Color RemoveRawGray = Color.FromArgb(217, 80, 80, 80);

        public static readonly Color StarYellow = Color.FromArgb(255, 202, 40);
        public static readonly Color StarYellowHover = Color.FromArgb(255, 179, 0);

        public static readonly Color TrashRed = Color.FromArgb(239, 83, 80);
        public static readonly Color TrashRedHover = Color.FromArgb(211, 47, 47);

        // Overlays & Notifications
        public static readonly Color NavOverlay = Color.FromArgb(102, 0, 0, 0);
        public static readonly Color NotificationSuccess = Color.FromArgb(180, 40, 167, 69);
        public static readonly Color NotificationDanger = Color.FromArgb(180, 220, 50, 50);
        public static readonly Color NotificationWarning = Color.FromArgb(180, 255, 215, 0);
        public static readonly Color NotificationInfo = Color.FromArgb(180, 50, 50, 220);
        public static readonly Color FileNameBackground = Color.FromArgb(150, 0, 0, 0);
    }
}
