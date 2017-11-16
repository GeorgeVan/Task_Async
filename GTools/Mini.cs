using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GTools
{
    public static partial class Mini
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Causes compiler to optimize the call away
        public static void NoWarning(this Task task) { /* No code goes in here */ }

        public static void ConsoleHitAndExit()
        {
            Console.WriteLine("George：请按键退出程序！");
            Console.ReadKey();
        }
    }
}
