using System;
using System.Collections.Generic;

namespace Lightrealm
{
    [Serializable]
    public class SerializableRandom
    {
        private int seed;
        private int usageCount; // Tracks how many times Random.Next() has been called
        [NonSerialized]
        private Random random; // Do not serialize the Random instance directly

        public bool IsTracking = false;
        public List<double> TrackedNumbers = new List<double>();
        public string TrackedString
        {
            get
            {
                return string.Join(" ", TrackedNumbers);
            }
        }

        public SerializableRandom(int seed)
        {
            this.seed = seed;
            this.usageCount = 0;
            this.random = new Random(seed);
        }

        // Next(int maxValue)
        public int Next(int maxValue)
        {
            usageCount++;
            int result = random.Next(maxValue);
            if (IsTracking)
            {
                TrackedNumbers.Add(result);
            }
            return result;
        }

        public int Next()
        {
            usageCount++;
            int result = random.Next();
            if (IsTracking)
            {
                TrackedNumbers.Add(result);
            }
            return result;
        }

        // Next(int minValue, int maxValue)
        public int Next(int minValue, int maxValue)
        {
            usageCount++;
            int result = random.Next(minValue, maxValue);
            if (IsTracking)
            {
                TrackedNumbers.Add(result);
            }
            return result;
        }

        // NextDouble()
        public double NextDouble()
        {
            usageCount++;
            double result = random.NextDouble();
            if (IsTracking)
            {
                TrackedNumbers.Add(result);
            }
            return result;
        }

        // Reinitialize the Random instance after deserialization
        public void Initialize()
        {
            random = new Random(seed);

            // Advance the Random state to the correct usage count
            for (int i = 0; i < usageCount; i++)
            {
                random.Next();
            }
        }

        // Properties for serialization
        public int Seed => seed;
        public int UsageCount => usageCount;
    }
}
