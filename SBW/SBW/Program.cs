using System;
using System.Linq;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using SharpDX;
using SharpDX.Direct3D9;
using EloBuddy.SDK.Menu.Values;

namespace SBW
{
    class Program
    {
        static void Main(string[] args) { Loading.OnLoadingComplete += Game_OnGameLoad; }

        public static Font Tahoma13;
        public static Menu Config, Advanced;
        public static int LastMouseTime = Environment.TickCount;
        public static Vector2 LastMousePos = Game.CursorPos.To2D();
        public static int NewPathTime = Environment.TickCount;
        public static int LastType = 0; // 0 Move , 1 Attack, 2 Cast spell
        public static int LastUserClickTime = Environment.TickCount;
        public static int PathPerSecInfo;



        private static void Game_OnGameLoad(EventArgs args)
        {
            Tahoma13 = new Font(Drawing.Direct3DDevice, new FontDescription
            { FaceName = "Tahoma", Height = 14, OutputPrecision = FontPrecision.Default, Quality = FontQuality.ClearType });

            Config = MainMenu.AddMenu("SBW", "SBW");
            Config.Add("enable", new CheckBox("ENABLE", true));
            Config.Add("ClickTime", new Slider("Minimum Click Time (100)", 100, 300, 0));
            Config.AddLabel("0 - 100 scripter");
            Config.AddLabel("100 - 200 pro playerr");
            Config.AddLabel("200 + normal player");
            Config.Add("showCPS", new CheckBox("Show action per sec", true));
            Config.Add("showWay", new CheckBox("Show way points", true));
            Advanced = Config.AddSubMenu("Advanced", "AdvancedSettings");
            Advanced.AddGroupLabel("Advanced Settings");
            Advanced.Add("cut", new CheckBox("CUT SKILLSHOTS", true));
            Advanced.Add("skill", new CheckBox("BLOCK inhuman skill cast", true));

            Obj_AI_Base.OnNewPath += Obj_AI_Base_OnNewPath;
            Player.OnIssueOrder += Player_OnIssueOrder;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Game.OnWndProc += Game_OnWndProc;
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Config["showCPS"].Cast<CheckBox>().CurrentValue)
            {
                var h = Drawing.Height * 0.7f;
                var w = Drawing.Width * 0.15f;
                var color = Color.Yellow;
                if (PathPerSecInfo < 5)
                    color = Color.GreenYellow;
                else if (PathPerSecInfo > 8)
                    color = Color.OrangeRed;

                DrawFontTextScreen(Tahoma13, "SBW Server action per sec: " + PathPerSecInfo, h, w, color);
            }

            if (Config["showWay"].Cast<CheckBox>().CurrentValue)
            {
                var lastWaypoint = ObjectManager.Player.Path.Last();
                if (lastWaypoint.IsValid())
                {
                    drawLine(ObjectManager.Player.Position, lastWaypoint, 1, System.Drawing.Color.Red);
                }
            }
        }

        public static int PathPerSecCounter = 0;
        public static int PathTimer = Environment.TickCount;

        private static void Game_OnUpdate(EventArgs args)
        {
            if (Environment.TickCount - PathTimer > 1000)
            {
                PathPerSecInfo = PathPerSecCounter;
                PathTimer = Environment.TickCount;
                PathPerSecCounter = 0;
            }
        }

        private static void Obj_AI_Base_OnNewPath(Obj_AI_Base sender, GameObjectNewPathEventArgs args)
        {
            if (!sender.IsMe)
                return;

            PathPerSecCounter++;
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (args.Msg == 123)
            {
                LastUserClickTime = Environment.TickCount;
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            if (!Config["enable"].Cast<CheckBox>().CurrentValue)
                return;

            // IGNORE TARGETED SPELLS
            if (args.EndPosition.IsZero)
            return;

            if (args.Slot != SpellSlot.Q && args.Slot != SpellSlot.W && args.Slot != SpellSlot.E && args.Slot != SpellSlot.R)
                return;

            var spell = ObjectManager.Player.Spellbook.Spells.FirstOrDefault(x => x.Slot == args.Slot);

            // LINE CUT SPELL RANGE
            if (Advanced["cut"].Cast<CheckBox>().CurrentValue && spell != null && spell.SData.LineWidth != 0 && args.EndPosition.Distance(args.StartPosition) > 700)
            {
                Random rnd = new Random();
                var cutDest = args.StartPosition.Extend(args.EndPosition, rnd.Next(400, 600)).To3D();                
                ObjectManager.Player.Spellbook.CastSpell(args.Slot, cutDest);
                Console.WriteLine("CUT SPELL");
                args.Process = false;
                return;
            }
            
            var screenPos = Drawing.WorldToScreen(args.EndPosition);    
            if (Advanced["skill"].Cast<CheckBox>().CurrentValue && Environment.TickCount - LastMouseTime < LastMousePos.Distance(screenPos) / 15)
            {
                Console.WriteLine("BLOCK SPELL");
                args.Process = false;
                return;
            }
            LastType = 2;
            LastMouseTime = Environment.TickCount;
            LastMousePos = screenPos;
            PathPerSecCounter++;
        }

        private static void Player_OnIssueOrder(Obj_AI_Base sender, PlayerIssueOrderEventArgs args)
        {
            if (!Config["enable"].Cast<CheckBox>().CurrentValue)
                return;

            // ignore user clicks
            if (Environment.TickCount - LastUserClickTime < 500)
                return;

            var screenPos = Drawing.WorldToScreen(args.TargetPosition);
          
            //Console.WriteLine(args.Order);
            if (Environment.TickCount - LastMouseTime < Config["ClickTime"].Cast<Slider>().CurrentValue + (LastMousePos.Distance(screenPos) / 15))
            {
                //Console.WriteLine("BLOCK " + args.Order);
                args.Process = false;
                return;
            }

            //Console.WriteLine("DIS " + LastMousePos.Distance(screenPos) + " TIME " + (Utils.TickCount - LastMouseTime));
            if (args.Order == GameObjectOrder.AttackUnit)
                LastType = 1;
            else
                LastType = 0;

            LastMouseTime = Environment.TickCount;
            LastMousePos = screenPos;
        }

        public static void DrawFontTextScreen(Font vFont, string vText, float vPosX, float vPosY, ColorBGRA vColor)
        {
            vFont.DrawText(null, vText, (int)vPosX, (int)vPosY, vColor);
        }

        public static void drawLine(Vector3 pos1, Vector3 pos2, int bold, System.Drawing.Color color)
        {
            var wts1 = Drawing.WorldToScreen(pos1);
            var wts2 = Drawing.WorldToScreen(pos2);

            Drawing.DrawLine(wts1[0], wts1[1], wts2[0], wts2[1], bold, color);
        }
    }
}
