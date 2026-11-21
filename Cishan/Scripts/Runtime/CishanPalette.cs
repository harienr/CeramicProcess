using UnityEngine;

namespace Cishan
{
    /// <summary>Colour and type tokens from the art bible (v0.1).</summary>
    public static class Palette
    {
        public static readonly Color Paper = Hex("F2EBDD");
        public static readonly Color PaperWarm = Hex("E7DECD");
        public static readonly Color Ink = Hex("2E2C28");
        public static readonly Color InkSoft = Hex("4A463F");
        public static readonly Color Muted = Hex("7B7469");
        public static readonly Color Line = Hex("C9BFAE");
        public static readonly Color Blue = Hex("3F6688");
        public static readonly Color Stone = Hex("8D887D");
        public static readonly Color StoneDark = Hex("68645C");
        public static readonly Color Wood = Hex("6B5742");
        public static readonly Color WoodDark = Hex("4F3F30");
        public static readonly Color Plaster = Hex("D8CEBC");
        public static readonly Color Clay = Hex("C6BDAE");
        public static readonly Color Powder = Hex("D9D3C7");
        public static readonly Color Slurry = Hex("CFC9BD");
        public static readonly Color Water = Hex("6D7774");
        public static readonly Color Skin = Hex("C4A489");

        public static Color Hex(string hex)
        {
            Color c;
            if (ColorUtility.TryParseHtmlString("#" + hex, out c)) return c;
            return Color.magenta;
        }

        public static Color Alpha(Color c, float a)
        {
            c.a = a;
            return c;
        }
    }

    /// <summary>
    /// Single source of truth for the workshop massing. All units in metres.
    /// Interior 12.6 x 7.2, eaves 4.2, ridge 5.2, eye height 1.62.
    /// </summary>
    public static class WorkshopLayout
    {
        public const float RoomX = 12.6f;
        public const float RoomZ = 7.2f;
        public const float Eaves = 4.2f;
        public const float Ridge = 5.2f;
        public const float EyeHeight = 1.62f;
        public const float WallT = 0.35f;

        public static readonly Vector3 PlayerStart = new Vector3(1.25f, 0.05f, 3.45f);
        public static readonly Vector3 EntryDoor = new Vector3(0f, 0f, 3.9f);
        public static readonly Vector3 WestDoorCentre = new Vector3(0f, 0f, 3.9f);

        // 06 hammer bay
        public static readonly Vector3 MortarA = new Vector3(7.1f, 0f, 2.9f);
        public static readonly Vector3 MortarB = new Vector3(7.1f, 0f, 4.4f);
        public const float MortarMouthRadius = 0.325f;
        public const float MortarRimY = 0.24f;
        public const float MortarDepth = 0.42f;

        // wheel sits outside the open south edge, at the east end
        public static readonly Vector3 WheelCentre = new Vector3(10.9f, 2.0f, -1.35f);
        public const float WheelRadius = 1.75f;
        public const float WheelWidth = 0.95f;
        public const float AxleY = 2.0f;
        public const float AxleX = 10.9f;

        public static readonly Vector3 BeamPivot = new Vector3(9.6f, 1.31f, 0f); // Z per hammer
        public const float BeamHeadX = 7.1f;
        public const float BeamTailX = 10.9f;
        public const float BeamRestAngle = 16.6f;
        public const float BeamLiftAngle = 12.5f;

        public static readonly Vector3 GateHandle = new Vector3(8.95f, 1.15f, -0.62f);
        public static readonly Vector3 GateAnchor = new Vector3(8.95f, 0.9f, -1.0f);

        // 04 / 05 / 07 / 08 / 09 / 10 anchors
        public static readonly Vector3 Anvil = new Vector3(2.35f, 0f, 1.15f);
        public static readonly Vector3 StonePile = new Vector3(1.1f, 0f, 1.55f);
        public static readonly Vector3 WashTrough = new Vector3(2.0f, 0f, 6.0f);
        public static readonly Vector3 WorkTable = new Vector3(4.55f, 0f, 4.75f);
        public static readonly Vector3 VatWash = new Vector3(4.9f, 0f, 6.0f);
        public static readonly Vector3 VatSettle = new Vector3(6.35f, 0f, 6.05f);
        public static readonly Vector3 VatThicken = new Vector3(7.8f, 0f, 6.05f);
        public static readonly Vector3 SieveStand = new Vector3(9.0f, 0f, 6.05f);

        public const float RailX = 6.3f;

        /// <summary>The area behind the guard rail where hammer heads fall.</summary>
        public static bool InHammerBay(Vector3 p)
        {
            return p.x > RailX && p.x < 11.4f && p.z > 1.75f && p.z < 5.75f;
        }

        public static bool IsBehindRail(Vector3 p)
        {
            return p.x < RailX;
        }
    }
}
