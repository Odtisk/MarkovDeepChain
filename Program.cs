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
            const int DEPTH = 100;
            var MDC = new MarkovDeepChain();
            //MDC.CreateChainFromFile("war.txt", DEPTH);

            string start = "Иисус сказал им: истинно, истинно вам говорю, ";
            MDC.CreateFromString("Кришна шнобель льёт ебать батя я", DEPTH);
            string continued = MDC.ContinueSequence(start, 50, DEPTH);
            Console.WriteLine($"Result: {start}{continued}");
        }
    }

}


