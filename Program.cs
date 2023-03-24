// See https://aka.ms/new-console-template for more information
using MDC;
using System.IO;
using System.Text;

namespace Program
{
    class Program
    {

        static void Main(string[] args)
        {
            MarkovDeepChain MDC = new();
            MDC.CreateTable("mama myla ramu, ramu myla mama. papa liubit mamu. mama liubit papu. papa gde?", " ", 2);

            foreach (string word in MDC.Generate(10))
            {
                Console.Write($"{word} ");
            }
        }
    }

}


