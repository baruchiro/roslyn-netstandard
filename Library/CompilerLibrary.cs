using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Library
{
    public static class CompilerLibrary
    {
        public static void Compile()
        {
            var testTree = CSharpSyntaxTree.ParseText(@"using System; 
        namespace test{

         public class Power
         {

           public void power(int number)
          { 
            Console.WriteLine(number * number);
          } 

         }

        }");

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

            options = options.WithAssemblyIdentityComparer(AssemblyIdentityComparer.Default);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            };

            if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedAssemblies)
            {
                Console.WriteLine(trustedAssemblies);
                references.AddRange(
                    trustedAssemblies.Split(Path.PathSeparator).Select(path =>
                        MetadataReference.CreateFromFile(path))
                );
            }

            var compilation = CSharpCompilation.Create("DummyAssembly", options: options, references: references)
                .AddSyntaxTrees(testTree);

            using (var stream = new MemoryStream())
            {
                var compilationResult = compilation.Emit(stream);

                if (!compilationResult.Success)
                {
                    Console.WriteLine(string.Join("\n", compilationResult.Diagnostics.Select(d => d.GetMessage())));
                }
                else
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    var assembly = Assembly.Load(stream.ToArray());
                    var type = assembly.GetType("test.Power");
                    var power = Activator.CreateInstance(type);
                    type.InvokeMember("power", BindingFlags.Default | BindingFlags.InvokeMethod, null, power, new object[] { 2 });
                }
            }
        }
    }
}
