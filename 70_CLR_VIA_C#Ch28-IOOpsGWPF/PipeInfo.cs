
using System;

public partial class PipeDemo
{

    public struct PipeInfo
    {
        public int order;
        public int sstage1;
        public int sstage2;
        public int sstage3;
        public int sexp;
        public int scanceled;
        public int sclosed;
        public int scompleted;

        public int created;
        public int cstage1;
        public int cstage2;
        public int cstage3;
        public int cexp;
        public int ccanceled;
        public int cclosed;
        public int ccompleted;
        public int ccompleted1;
        public int ccompleted2;

        public double[] cProgress;

        public PipeInfo(int slot):this()
        {
            cProgress = new double[slot];
        }

        public void ClearServer()
        {
            sstage1 = sstage2 = sstage3 = sexp = scanceled = sclosed = scompleted = 0;
        }

        public void ClearClient()
        {
            order = created = 0;
            cstage1 = cstage2 = cstage3 = cexp = ccanceled = cclosed = ccompleted = 0;
            Array.Clear(cProgress,0, cProgress.Length);
        }
    }

}