using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
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
            //var test = new Test();
            //test.Run();

            var hotfix = new Hotfix();
            hotfix.Run();
            
        }
    }

    public class Hotfix
    {
        DateTime? lastModify = null;
        Bug bug = new Bug();

        public void Run()
        {
            Injector injector = new Injector();
            injector.CILInjectionInit();
            Console.Clear();
            Console.ReadKey();

            var timer = new System.Threading.Timer((e) =>
            {
                Check();
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            var timer1 = new System.Threading.Timer((e) =>
            {
                try
                {
                    bug.DoWork();
                }
                catch
                {
                    Console.WriteLine("Ooops something went wrong");
                }

            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

            Console.ReadKey();
        }

        public void Check()
        { 
            if(lastModify == null)
            {
                lastModify = File.GetLastWriteTimeUtc(@"C:\Projects\Github\SMC\SMC\Demo\DotNet35_x64\Bug.cs");
            }
            else
            {
                var newlmd = File.GetLastWriteTimeUtc(@"C:\Projects\Github\SMC\SMC\Demo\DotNet35_x64\Bug.cs");
                Console.WriteLine(string.Format("old value {0} new value {1}", lastModify, newlmd));
                if (newlmd > lastModify)
                {
                    Console.WriteLine("podmianka");
                    var content = File.ReadAllText(@"C:\Projects\Github\SMC\SMC\Demo\DotNet35_x64\Bug.cs");
                    InjectCode(content, "Test.Bug", "DoWork", BindingFlags.Public | BindingFlags.Instance);
                    lastModify = newlmd;
                }
            }
        }

        public void InjectCode(string code, string typeName, string methodName, BindingFlags flags)
        {
            // Compile code  
            CSharpCodeProvider cProv = new CSharpCodeProvider();
            CompilerParameters cParams = new CompilerParameters();
            cParams.ReferencedAssemblies.Add("mscorlib.dll");
            cParams.ReferencedAssemblies.Add("System.dll");
            cParams.GenerateExecutable = false;
            cParams.GenerateInMemory = true;

            CompilerResults cResults = cProv.CompileAssemblyFromSource(cParams, code);
            var obj = cResults.CompiledAssembly.CreateInstance(typeName);
            var t = obj.GetType();
            var methodInfo = t.GetMethod(methodName, flags);

            var ilCode = methodInfo.GetMethodBody().GetILAsByteArray();
            InjectionHelper.UpdateILCodes(methodInfo, ilCode);
        }
    }

    public class Test
    {
        public void CompareOneAndTwo()
        {
            int a = 1;
            int b = 2;
            Console.WriteLine(string.Format("Porównywanie liczb {0} i {1}", a, b));
            if (a < b)
            {
                System.Diagnostics.Debugger.Break();
                Console.WriteLine(string.Format("liczba {0} jest mniejsza od {1}", a, b));
            }
            else
            {
                System.Diagnostics.Debugger.Break();
                Console.WriteLine(string.Format("liczba {0} jest większa lub równa {1}", a, b));
            }
        }

        public void Run()
        {
            #region Init
            //CILInjectionInit();
            Injector injector = new Injector();
            injector.CILInjectionInit();
            Console.Clear();
            Console.ReadKey();

            //Type type = this.GetType();
            //MethodInfo methodInfo = type.GetMethod("CompareOneAndTwo", BindingFlags.Public | BindingFlags.Instance);

            //var ilCodes = GetUpdatedILCode(methodInfo);
            //InjectionHelper.UpdateILCodes(methodInfo, ilCodes);
            Inject();

            #endregion

            CompareOneAndTwo();

            Console.ReadKey();
        }

        #region Init_methods

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

        #endregion 
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
