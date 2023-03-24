using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MDC
{
    internal class MarkovDeepChain
    {
        readonly private List<string> words = new();

        internal class ProbabilityPair
        {
            readonly private ushort wordId;
            readonly private ushort nextWordId;
            readonly private byte depthLevel;
            private uint count;

            public ProbabilityPair(ushort wordId, ushort nextWordId, byte depthLevel, uint count)
            {
                this.wordId = wordId;
                this.nextWordId = nextWordId;
                this.depthLevel = depthLevel;
                this.count = count;
            }

            public ushort WordId { get => wordId; }
            public ushort NextWordId { get => nextWordId; }
            public byte DepthLevel { get => depthLevel; }
            public uint Count { get => count; set => count = value; }

            public void Increase()
            {
                count++;
            }

            public bool Matches(ushort wordId, ushort nextWordId, byte depthLevel)
            {
                return WordId == wordId && NextWordId == nextWordId && DepthLevel == depthLevel;
            }

            public bool Matches(ushort wordId, byte depthLevel)
            {
                return WordId == wordId && DepthLevel == depthLevel;
            }
        }

        readonly private List<ProbabilityPair> table = new();

        public void Print()
        {
            foreach (var pair in table)
            {
                string word = words[pair.WordId];
                string nextWord = words[pair.NextWordId];
                byte depthLevel = pair.DepthLevel;
                uint count = pair.Count;

                Console.WriteLine($"{word}({depthLevel}) -> {nextWord} | {count}");
            }
        }

        public void CreateTable(string[] corpus, int maxDepth)
        {
            for (int elId = 0; elId < corpus.Length; elId++)
            {
                for (byte depthLevel = 1; (depthLevel <= maxDepth) && (elId + depthLevel < corpus.Length); depthLevel++)
                {
                    string word = corpus[elId];
                    string nextWord = corpus[elId + depthLevel];
                    Add(word, nextWord, depthLevel);
                }
            }
        }

        public void CreateTable(string corpus, string separator, int maxDepth)
        {
            CreateTable(new Regex(separator).Split(corpus), maxDepth);
        }

        public void WriteToFile(string outputPath)
        {
            using StreamWriter writer = new(outputPath, true);
            foreach (var pair in table)
            {
                string word = words[pair.WordId];
                string nextWord = words[pair.WordId];
                byte depthLevel = pair.DepthLevel;
                uint count = pair.Count;

                Console.WriteLine($"{word} {nextWord} {depthLevel} {count}");
            }
        }

        private int GetPairId(ushort wordId, ushort nextWordId, byte depthLevel)
        {
            for (int id = 0; id < table.Count; id++)
            {
                var pair = table[id];
                bool matched = pair.Matches(wordId, nextWordId, depthLevel);
                if (matched) return id;
            }
            return -1;
        }

        private ProbabilityPair GetPair(ushort wordId, ushort nextWordId, byte depthLevel)
        {
            int pairId = GetPairId(wordId, nextWordId, depthLevel);
            return table[pairId];
        }

        private void Add(string word, string nextWord, byte depthLevel)
        {
            if (!words.Contains(word))
                words.Add(word);
            if (!words.Contains(nextWord))
                words.Add(nextWord);

            ushort wordId = (ushort)words.IndexOf(word);
            ushort nextWordId = (ushort)words.IndexOf(nextWord);

            int pairId = GetPairId(wordId, nextWordId, depthLevel);
            bool pairExists = pairId != -1;

            if (pairExists) table[pairId].Increase();
            else table.Add(new(wordId, nextWordId, depthLevel, 1));
        }

        private List<string> GetPossibleNextWords(string word, byte depthLevel)
        {
            List<string> found = new();
            int wordId = words.IndexOf(word);
            if (wordId == -1) return found;

            foreach (var pair in table)
            {
                if (pair.Matches((ushort)wordId, depthLevel))
                {
                    string nextWord = words[pair.NextWordId];
                    found.Add(nextWord);
                }
            }
            return found;
        }

        private uint GetTotalNextWordCount(in ushort wordId, in byte depthLevel)
        {
            uint totalCount = 0;
            foreach (var pair in table)
            {
                if (pair.Matches(wordId, depthLevel))
                {
                    totalCount += pair.Count;
                }
            }
            return totalCount;
        }

        private float GetPossibility(in ushort wordId, in ushort nextWordId, in byte depthLevel)
        {
            uint totalCount = GetTotalNextWordCount(wordId, depthLevel);
            var pair = GetPair(wordId, nextWordId, depthLevel);

            return 100 * pair.Count / totalCount;
        }

        private string GetRandomNextWordId(string word, byte depthLevel)
        {
            var random = new Random();
            List<string> possibleNextWords = GetPossibleNextWords(word, depthLevel);
            int id = random.Next(possibleNextWords.Count);
            return possibleNextWords[id];
        }

        public List<string> Generate(byte amount = 1, byte maxDepth = 1)
        {
            List<string> generated = new() { GetRandomWord() };

            for (byte i = 0; i < amount; i++)
            {
                var lastWords = generated.TakeLast(maxDepth);
                var possibleNextWords = GetPossibleNextWords(lastWords.Last(), 1);

                if (!possibleNextWords.Any())
                {
                    generated.Add(GetRandomWord());
                    continue;
                }

                for (byte depth = 2; depth <= lastWords.Count(); depth++)
                {
                    string word = lastWords.ElementAt(depth);
                    var deeperNextWords = GetPossibleNextWords(word, depth);
                    var nextStepWords = possibleNextWords.Intersect(deeperNextWords);

                    if (nextStepWords.Any()) possibleNextWords = new(nextStepWords);
                    else break;
                }

                Random random = new();
                int id = random.Next(possibleNextWords.Count);
                generated.Add(possibleNextWords[id]);
            }

            return generated;
        }

        private string GetRandomWord()
        {
            Random random = new();
            int totalWordsCount = words.Count;
            int randomWordId = random.Next(totalWordsCount);
            return words[randomWordId];
        }
    }
}
