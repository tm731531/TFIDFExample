using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosineSimilarity
{
    public class CosineSimilarity
    {

        public static double GetCosineSimilarity(double[] v1, double[] v2)
        {
            int N = 0;
             N = ((v2.ToList().Count < v1.ToList().Count) ? v2.ToList().Count : v1.ToList().Count);
            double dot = 0.0d;
            double mag1 = 0.0d;
            double mag2 = 0.0d;
            for (int n = 0; n < N; n++)
            {
                dot += v1[n] * v2[n];
                mag1 += Math.Pow(v1[n], 2);
                mag2 += Math.Pow(v2[n], 2);
            }
            if ((Math.Sqrt(mag1) * Math.Sqrt(mag2)) == 0)
            {
                return 0;
            }
            return dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2));
        }

    }
}
