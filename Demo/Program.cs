using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Forms;

namespace Test
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var test = new Test();
            test.Run();
            
        }
    }

    public class Test
    {
        public void CompareOneAndTwo()
        {
            int a = 1;
            int b = 2;
            if (a < b)
            {
                Console.WriteLine("Number 1 is less than 2");
            }
            else
            {
                Console.WriteLine("Number 1 is greater than 2 (O_o)");
            }
        }

        public void Run()
        {
            //CILInjectionInit();
            Injector injector = new Injector();
            injector.CILInjectionInit();

            Console.ReadKey();

            Type type = this.GetType();
            MethodInfo methodInfo = type.GetMethod("CompareOneAndTwo", BindingFlags.Public | BindingFlags.Instance);

            var ilCodes = GetUpdatedILCode(methodInfo);
            InjectionHelper.UpdateILCodes(methodInfo, ilCodes);
            //Inject();

            CompareOneAndTwo();

            Console.ReadKey();
        }

        public byte[] GetUpdatedILCode(MethodInfo methodInfo)
        {
            // the following line is unnecessary actually
            // Here we use it to cause the method to be compiled by JIT
            // so that we can verify this also works for JIT-compiled method :)
            RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle);

            // get the original IL Codes for the method 
            byte[] ilCodes = methodInfo.GetMethodBody().GetILAsByteArray();

            // this is not a good way to search OpCode without parsing
            // but it works for our sample :)
            for (int i = 0; i < ilCodes.Length; i++)
            {
                if (ilCodes[i] == OpCodes.Bge_S.Value)
                {
                    // Replacing Bge_S with Blt_S
                    ilCodes[i] = (byte)OpCodes.Blt_S.Value;
                }
            }

            return ilCodes;
        }

        public void Inject()
        {
            Type type = this.GetType();
            MethodInfo methodInfo = type.GetMethod("CompareOneAndTwo", BindingFlags.Public | BindingFlags.Instance);

            var ilCodes = GetUpdatedILCode(methodInfo);
            InjectionHelper.UpdateILCodes(methodInfo, ilCodes);
        }

        public void CILInjectionInit()
        {
            InjectionHelper.Initialize();

            Thread thread = new Thread(WaitForInitialization);
            thread.Start();
        }

        private delegate void InitializationCompletedDelegate(InjectionHelper.Status status);

        private void WaitForInitialization()
        {
            InjectionHelper.Status status = InjectionHelper.WaitForIntializationCompletion();
            InitializationCompletedDelegate del = new InitializationCompletedDelegate(InitializationCompleted);
            del(status);
        }

        private void InitializationCompleted(InjectionHelper.Status status)
        {
            if (status == InjectionHelper.Status.Ready)
            {
                Console.WriteLine(@"Initialization is completed successfully, enjoy!");
            }
            else
            {
                Console.WriteLine(string.Format(@"Initialization is failed with error [{0}]!", status.ToString()));
            }
        }
    }

    public class Injector
    {
        public void Inject()
        {
            // get the target method first
            Type type = Type.GetType("Test.Test");
            MethodInfo methodInfo = type.GetMethod("CompareOneAndTwo", BindingFlags.Public | BindingFlags.Instance);

            // the following line is unnecessary actually
            // Here we use it to cause the method to be compiled by JIT
            // so that we can verify this also works for JIT-compiled method :)
            RuntimeHelpers.PrepareMethod(methodInfo.MethodHandle);

            // get the original IL Codes for the method 
            byte[] ilCodes = methodInfo.GetMethodBody().GetILAsByteArray();

            // this is not a good way to search OpCode without parsing
            // but it works for our sample :)
            for (int i = 0; i < ilCodes.Length; i++)
            {
                if (ilCodes[i] == OpCodes.Bge_S.Value)
                {
                    // Replacing Bge_S with Blt_S
                    ilCodes[i] = (byte)OpCodes.Blt_S.Value;
                }
            }

            // update the IL
            InjectionHelper.UpdateILCodes(methodInfo, ilCodes);
        }

        public void CILInjectionInit()
        {
            InjectionHelper.Initialize();

            Thread thread = new Thread(WaitForInitialization);
            thread.Start();
        }

        private delegate void InitializationCompletedDelegate(InjectionHelper.Status status);

        private void WaitForInitialization()
        {
            InjectionHelper.Status status = InjectionHelper.WaitForIntializationCompletion();
            InitializationCompletedDelegate del = new InitializationCompletedDelegate(InitializationCompleted);
            del(status);
        }

        private void InitializationCompleted(InjectionHelper.Status status)
        {
            if (status == InjectionHelper.Status.Ready)
            {
                Console.WriteLine(@"Initialization is completed successfully, enjoy!");
            }
            else
            {
                Console.WriteLine(string.Format(@"Initialization is failed with error [{0}]!", status.ToString()));
            }
        }
    }
}
