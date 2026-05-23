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

        public List<TextStorage> Intrigue = new List<TextStorage>();

        public Architect originalLeader = null;

        public int MapCursorX;
        public int MapCursorZ;

        public bool ReceivedPlanMessageThisLoad = true;

        public EntityList<Room> RoomsUnsearched = new EntityList<Room>();

        public EntityList<Objective> ActiveObjectives = new EntityList<Objective>();

        public EntityList<Architect> IntriguingArchitects { get; set; } = new EntityList<Architect>();
        public List<Region> CurrentlyMarkedRegions { get; set; } = new List<Region>();
        public Unit CurrentEvent;

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

            originalLeader = leader;
        }

    }
}
