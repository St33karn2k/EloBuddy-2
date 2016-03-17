using System;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Events;

namespace CameraFocus
{
    class Program
    {
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += GameLoaded;
        }

        private static void GameLoaded(EventArgs args)
        {
            Game.OnTick += (EventArgs Tick) =>
            Camera.ScreenPosition = Player.Instance.Position.To2D();
        }
    }
}
