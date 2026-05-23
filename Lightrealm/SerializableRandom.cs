using System;
using System.Collections.Generic;
using System.Linq;

namespace Lightrealm
{
    [Serializable]
    public class SerializableRandom
    {
        private int seed;
        public int Seed => seed;

        private long usageCount;
        public long UsageCount => usageCount;

        [NonSerialized]
        private Random random;

        public bool IsTracking = true;
        public List<double> TrackedNumbers = new List<double>();
        public string TrackedString => string.Join(" ", TrackedNumbers);

        public enum RandomOp { Next, NextMax, NextRange, NextDouble, NextLong }
        public List<RandomOp> OperationHistory = new List<RandomOp>();

        public SerializableRandom(int seed)
        {
            this.seed = seed;
            this.usageCount = 0;
            this.random = new Random(seed);
        }

        public int Next()
        {
            usageCount++;
            OperationHistory.Add(RandomOp.Next);
            return random.Next();
        }

        public int Next(int maxValue)
        {
            usageCount++;
            OperationHistory.Add(RandomOp.NextMax);
            return random.Next(maxValue);
        }

        public int Next(int minValue, int maxValue)
        {
            usageCount++;
            OperationHistory.Add(RandomOp.NextRange);
            return random.Next(minValue, maxValue);
        }

        public double NextDouble()
        {
            usageCount++;
            OperationHistory.Add(RandomOp.NextDouble);
            return random.NextDouble();
        }

        public long NextLong(long minValue, long maxValue)
        {
            if (minValue >= maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue), "minValue must be less than maxValue");

            usageCount++;
            OperationHistory.Add(RandomOp.NextLong);

            ulong range = (ulong)(maxValue - minValue);
            ulong ulongRand;

            byte[] buf = new byte[8];
            do
            {
                random.NextBytes(buf);
                ulongRand = BitConverter.ToUInt64(buf, 0);
            }
            while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % range) + 1) % range);

            long result = (long)(ulongRand % range + (ulong)minValue);
            /*
            if (IsTracking)
            {
                TrackedNumbers.Add(result); // You may need to cast to double
            }
            */
            return result;
        }

        public void Initialize()
        {
            random = new Random(seed);

            foreach (var op in OperationHistory)
            {
                switch (op)
                {
                    case RandomOp.Next:
                        random.Next();
                        break;
                    case RandomOp.NextMax:
                        random.Next(100);
                        break;
                    case RandomOp.NextRange:
                        random.Next(0, 100);
                        break;
                    case RandomOp.NextDouble:
                        random.NextDouble();
                        break;
                    case RandomOp.NextLong:
                        byte[] dummy = new byte[8];
                        random.NextBytes(dummy); // advance RNG for NextLong
                        break;
                }
            }
        }
    }
}
