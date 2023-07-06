using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Robit.TextAdventure
{
    public class GameManager
    {
        public enum GameMode
        {
            Private,
            Public
        }

        public struct Event
        {
            public string Description;
            public string Question;
            public string Answer;
            public string Result;
        }

        private DiscordMember[] players;

        private GameMode gamemode;

        private uint turnCount;

        private List<Event> events;

        public GameManager(DiscordMember[] players, GameMode gameMode)
        {
            this.players = players;
            this.gamemode = gameMode;
            this.turnCount = 0;
            this.events = new List<Event>();
        }
    }
}
