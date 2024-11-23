using Microsoft.Xna.Framework.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Lightrealm
{
    [Serializable]
    public class Party : Group
    {
        public int MapCursorDistrict { get; set; } = 0;

        public EntityList<TextStorage> Intrigue = new EntityList<TextStorage>();

        public int MapCursorX;
        public int MapCursorZ;


        private int _currentEventId;

        public Unit CurrentEvent
        {
            get => EntityGet<Unit>(_currentEventId);
            set => _currentEventId = value?.ID ?? 0;
        }

        public EntityList<Architect> IntriguingArchitects { get; set; } = new EntityList<Architect>();
        public EntityList<Region> CurrentlyMarkedRegions { get; set; } = new EntityList<Region>();

        public Party() : base()
        {
        }

        public void ClearSkillData()
        {
            foreach (Architect a in Architects)
            {
                a.BodySlamReady = false;
                a.DoubleStrikeReady = false;
                a.DropKickReady = false;
                a.FinaleReady = false;
                a.LegSweepReady = false;
                a.QuickStrikeReady = false;
                a.SeveringStrikeReady = false;

                a.UsedSkills = new EntityList<Entity>();
                a.ExtraFocusTicks = 0;
            }
        }

        public Party(EntityList<Architect> architects, string type, Architect leader, Location basee)
            : base(architects, type, leader, basee)
        {
            // Constructor logic, if any, goes here.
        }

    }
}
