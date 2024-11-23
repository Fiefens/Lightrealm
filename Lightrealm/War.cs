using Lightrealm;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class War : Entity
{
    public Civilization Civilization1 { get; set; } // First civilization involved in the war
    public Civilization Civilization2 { get; set; } // Second civilization involved in the war

    public EntityList<Region> Frontline = new EntityList<Region>();

    public War(Civilization civ1, Civilization civ2)
    {
        Civilization1 = civ1;
        Civilization2 = civ2;

        // Calculate the frontline between the two civilizations
        CalculateFrontline();

        // Get capitals for both civilizations
        Region capitol1 = Civilization1.Capitol?.Region;
        Region capitol2 = Civilization2.Capitol?.Region;

        // Ensure both capitals exist
        if (capitol1 != null && capitol2 != null)
        {
            // Calculate the distance between the two capitals
            double distanceBetweenCapitals = Vector2.Distance(
                new Vector2(capitol1.X, capitol1.Z),
                new Vector2(capitol2.X, capitol2.Z)
            );

            // Seize unmarked territory only if the capitals are within 13 units of each other
            if (distanceBetweenCapitals <= 13)
            {
                SeizeUnmarkedTerritory(Civilization1);
                SeizeUnmarkedTerritory(Civilization2);
            }
        }
    }


    public void CalculateFrontline()
    {
        // Get capitals for both civilizations
        Region capitol1 = Civilization1.Capitol.Region;
        Region capitol2 = Civilization2.Capitol.Region;

        if (capitol1 == null || capitol2 == null) return;

        // Define the direction vector between the two capitals
        Vector2 capitol1Pos = new Vector2(capitol1.X, capitol1.Z);
        Vector2 capitol2Pos = new Vector2(capitol2.X, capitol2.Z);
        Vector2 direction = capitol2Pos - capitol1Pos;

        // Calculate the midpoint and perpendicular vector to create a dividing line
        Vector2 midpoint = (capitol1Pos + capitol2Pos) / 2;
        Vector2 perpendicular = new Vector2(-direction.Y, direction.X);
        perpendicular.Normalize(); // Normalize to unit length

        // Define a reasonable length for the frontline based on the distance between capitals
        int lineLength = (int)Vector2.Distance(capitol1Pos, capitol2Pos) / 2;

        // Generate the frontline along the perpendicular vector
        GenerateFrontline(midpoint, perpendicular, lineLength);
    }

    private void GenerateFrontline(Vector2 start, Vector2 perpendicular, int length)
    {
        int currentX = (int)start.X;
        int currentZ = (int)start.Y;

        for (int i = -length; i <= length; i++)
        {
            // Determine the offset on the hex grid (even/odd row consideration)
            Vector2 offset = perpendicular * i;
            int targetX = currentX + (int)offset.X;
            int targetZ = currentZ + (int)offset.Y;

            if (targetZ % 2 == 0) // Even rows, apply offsets
            {
                targetX += i % 2 == 0 ? 1 : 0;
            }

            // Ensure the tile is within world boundaries
            if (targetX >= 0 && targetX < Game1.GameWorld.Width && targetZ >= 0 && targetZ < Game1.GameWorld.Length)
            {
                Region frontlineRegion = Game1.GameWorld.WorldMap[targetX + targetZ * Game1.GameWorld.Width];

                if (!Frontline.Contains(frontlineRegion))
                {
                    Frontline.Add(frontlineRegion);
                }
            }
        }
    }

    private void SeizeUnmarkedTerritory(Civilization civilization)
    {
        // Get the capitol region for this civilization
        Region capitol = civilization.Capitol.Region;

        if (capitol == null) return;

        // Seize unmarked regions between the capitol and the frontline
        foreach (Region frontlineRegion in Frontline)
        {
            // Get all regions in the world
            for (int x = 0; x < Game1.GameWorld.Width; x++)
            {
                for (int z = 0; z < Game1.GameWorld.Length; z++)
                {
                    Region region = Game1.GameWorld.WorldMap[x + z * Game1.GameWorld.Width];

                    // Skip regions that are part of the frontline or already claimed
                    if (region.Owner != null || Frontline.Contains(region))
                        continue;

                    // Check if the region is within the triangle formed by the capitol and the two endpoints of the frontline
                    if (IsWithinTriangle(capitol, Frontline.First(), Frontline.Last(), region))
                    {
                        // Claim the region for the civilization
                        ClaimTerritory(civilization, region);
                    }
                }
            }
        }
    }

    private void ClaimTerritory(Civilization civilization, Region region)
    {
        if (region.Owner == null) // Only claim unmarked territories
        {
            int Month = ((int)Math.Round((decimal)(Game1.GameWorld.Cycle / 24192000)) % 12) + 1;
            int Year = (int)Math.Round((decimal)(Game1.GameWorld.Cycle / 290304000), MidpointRounding.ToZero);

            string Date = $"({Month}/{Year})";
            region.Owner = civilization;
            Game1.GameWorld.HistoricalEvents.Add(new Event($"{Date} {civilization.Name} has claimed territory in {region.Name}.", region, new EntityList<Entity>() { civilization }));
        }
    }

    // Check if a region lies within a triangle (capitol, left, right)
    private bool IsWithinTriangle(Region capitol, Region left, Region right, Region region)
    {
        Vector2 point = new Vector2(region.X, region.Z);
        Vector2 A = new Vector2(capitol.X, capitol.Z);
        Vector2 B = new Vector2(left.X, left.Z);
        Vector2 C = new Vector2(right.X, right.Z);

        // Using the barycentric coordinate method to check if the point lies within the triangle
        float denominator = (B.Y - C.Y) * (A.X - C.X) + (C.X - B.X) * (A.Y - C.Y);
        float a = ((B.Y - C.Y) * (point.X - C.X) + (C.X - B.X) * (point.Y - C.Y)) / denominator;
        float b = ((C.Y - A.Y) * (point.X - C.X) + (A.X - C.X) * (point.Y - C.Y)) / denominator;
        float c = 1 - a - b;

        return a >= 0 && b >= 0 && c >= 0;
    }
}
