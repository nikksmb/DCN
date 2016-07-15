using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactorizationExample
{
    public static class Factorization
    {
        public static ulong[] Facrotize_v1(ulong input)
        {
            List<ulong> result = new List<ulong>();
            ulong b;
            for (b = 2; input > 1; b++)
            {
                if (input%b == 0)
                {
                    while (input % b == 0)
                    {
                        input /= b;
                        result.Add(b);
                    }
                }
            }
            return result.ToArray();
        }

        public static ulong[] Facrotize_v2(ulong input)
        {
            List<ulong> result = new List<ulong>();
            ulong b;
            while (input % 2 == 0)
            {
                input /= 2;
                result.Add(2);
            }
            for (b = 3; input > 1; b += 2)
            {
                if (input % b == 0)
                {
                    while (input % b == 0)
                    {
                        input /= b;
                        result.Add(b);
                    }
                }
            }
            return result.ToArray();
        }

        public static ulong[] Facrotize_v3(ulong input)
        {
            List<ulong> result = new List<ulong>();
            ulong b;
            while (input % 2 == 0)
            {
                input /= 2;
                result.Add(2);
            }
            ulong lim = (ulong)Math.Sqrt(input) + 1;
            for (b = 3; input > 1; b += 2)
            {
                if (input % b == 0)
                {
                    while (input % b == 0)
                    {
                        input /= b;
                        result.Add(b);
                        lim = (ulong)Math.Sqrt(input) + 1;
                    }
                }
                if (b > lim)
                {
                    if (input != 1)
                    {
                        result.Add(input);
                    }
                    break;
                }
            }
            return result.ToArray();
        }
    }
}
