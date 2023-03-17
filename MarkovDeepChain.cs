using System.Text.Json;


namespace MDC
{
    using static System.Net.Mime.MediaTypeNames;
    using LettersCount = Dictionary<char, long>;
    internal class MarkovDeepChain
    {
        private struct Pair
        {
            public char letter;
            public char following;
            public int depth;

            public Pair(char letter, char following, int depth)
            {
                this.letter = letter;
                this.following = following;
                this.depth = depth;
            }

            public void Print()
            {
                Console.WriteLine("{0} - {1} - {2}", letter, following, depth);
            }
        }

        readonly private struct Probability
        {
            readonly private LettersCount counts;
            private bool LetterExists(char letter) => counts.ContainsKey(letter) && counts[letter] > 0;

            public void Print()
            {
                foreach (KeyValuePair<char, long> pair in counts)
                    Console.Write("{0}={1} ", pair.Key, pair.Value);
                Console.WriteLine();
            }

            public void AddLetter(char letter)
            {
                if (LetterExists(letter)) counts[letter]++;
                else counts[letter] = 1;
            }

            public Probability()
            {
                counts = new();
            }

            public LettersCount GetFollowingLetters()
            {
                return new(counts);
            }
        }

        readonly private struct DepthLevels
        {
            readonly private Dictionary<int, Probability> depthLevels;
            private bool LevelExists(int depth) => depthLevels.ContainsKey(depth);

            public void Print()
            {
                foreach (KeyValuePair<int, Probability> level in depthLevels)
                {
                    Console.Write("{0} -> ", level.Key);
                    level.Value.Print();
                }
                Console.WriteLine();
            }

            public void AddDepthLevel(int depth, char letter)
            {
                if (!LevelExists(depth))
                    depthLevels[depth] = new Probability();
                depthLevels[depth].AddLetter(letter);
            }

            public DepthLevels()
            {
                depthLevels = new();
            }

            public Probability GetNextProbability(char letter, int depth)
            {
                return depthLevels[depth];
            }

            public LettersCount GetFollowingLettersFromDepth(int depth)
            {
                if (depthLevels.ContainsKey(depth))
                    return depthLevels[depth].GetFollowingLetters();
                else return new();
            }
        }

        readonly private struct ProbabilityTable
        {
            readonly private Dictionary<char, DepthLevels> table;
            private bool PairExists(Pair pair) => table.ContainsKey(pair.letter);

            public void Print()
            {
                foreach (KeyValuePair<char, DepthLevels> letter in table)
                {
                    Console.WriteLine("{0} -------", letter.Key);
                    letter.Value.Print();
                }
                Console.WriteLine('\n');
            }

            public void AddPair(Pair pair)
            {
                if (!PairExists(pair))
                    table[pair.letter] = new DepthLevels();
                table[pair.letter].AddDepthLevel(pair.depth, pair.following);
            }

            public ProbabilityTable(List<Pair> pairs)
            {
                table = new();
                foreach (Pair pair in pairs)
                    AddPair(pair);
            }


            public char GetNext(string sequence)
            {
                var possibleLetter = GetPossibleNext(sequence);
                if (possibleLetter.Count > 0)
                    return GetRandomLetter(possibleLetter);
                else return '\0';
            }

            private static char GetMostLikelyLetter(LettersCount letterCounts)
            {
                char mostLikely = '\0';
                long maxCount = 0;

                foreach (KeyValuePair<char, long> pair in letterCounts)
                {
                    if (pair.Value > maxCount)
                    {
                        mostLikely = pair.Key;
                        maxCount = pair.Value;
                    }
                }

                Console.WriteLine($"From this dictionary: {StringifyLetterCounts(letterCounts)}");
                Console.WriteLine($"Chosen '{mostLikely}'");
                return mostLikely;
            }

            private static char GetRandomLetter(LettersCount letterCounts)
            {
                Random random = new();
                return letterCounts.ElementAt(random.Next(0, letterCounts.Count)).Key;
            }

            private LettersCount GetPossibleNext(string sequence)
            {
                int maxDepth = sequence.Length - 1;
                int depth = 0;
                var letterCounts = GetFollowingLettersFromDepth(sequence[maxDepth], depth);

                for (int i = maxDepth - 1; i >= 0; i--)
                {
                    var nextLevelLetterCounts = GetFollowingLettersFromDepth(sequence[i], depth++);
                    var remains = SubstractLetterCounts(letterCounts, nextLevelLetterCounts);

                    if (remains.Count == 0) break;
                    letterCounts = new(remains);
                }

                return letterCounts;
            }

            private static string StringifyLetterCounts(LettersCount letterCounts)
            {
                string stringified = "";
                foreach (KeyValuePair<char, long> pair in letterCounts)
                {
                    stringified += $"\t\t{pair.Key}={pair.Value}\n";
                }
                return stringified;
            }

            private LettersCount GetFollowingLettersFromDepth(char letter, int depth)
            {
                if (table.ContainsKey(letter))
                    return table[letter].GetFollowingLettersFromDepth(depth);
                else return new();
            }

            private static LettersCount SumLetterCounts(LettersCount one, LettersCount other)
            {
                foreach (KeyValuePair<char, long> pair in other)
                {
                    if (one.ContainsKey(pair.Key)) one[pair.Key] += pair.Value;
                    else one[pair.Key] = pair.Value;
                }
                return one;
            }

            private static LettersCount SubstractLetterCounts(LettersCount one, LettersCount other)
            {
                List<char> banned = new() { '\n', '\t' };
                LettersCount final = new();
                foreach (KeyValuePair<char, long> pair in other)
                {
                    if (!one.ContainsKey(pair.Key) || !other.ContainsKey(pair.Key)) continue;
                    long count = Math.Min(one[pair.Key], other[pair.Key]);
                    if (count > 0) final.Add(pair.Key, count);
                }
                return final;
            }
        }

        ProbabilityTable table = new();

        public void CreateChainFromFile(string path, int depth)
        {
            using StreamReader reader = new(path);
            string text = reader.ReadToEnd();
            CreateFromString(text, depth);
        }

        public void CreateFromString(string corpus, int depth)
        {
            List<Pair> pairs = CreatePairs(corpus, depth);
            table = new ProbabilityTable(pairs);
        }

        private static List<Pair> CreatePairs(in string source, in int maxDepth)
        {
            List<Pair> pairs = new();
            for (int elId = 0; elId < source.Length; elId++)
            {
                for (int nextId = elId + 1; (nextId - elId <= maxDepth) && (nextId < source.Length); nextId++)
                {
                    int depth = nextId - elId - 1;
                    char element = source[elId];
                    char following = source[nextId];
                    pairs.Add(new Pair(element, following, depth));
                }
            }
            return pairs;
        }

        public string ContinueSequence(string start, int count, int depth = 0)
        {
            string continued = "";

            for (int i = 0; i < count; i++)
            {
                string sequence = start + continued;
                bool isFit = (sequence.Length > depth);
                int startIndex = (!isFit) ? 0 : sequence.Length - depth - 1;
                int length = (!isFit) ? sequence.Length : depth;

                string lastSequence = sequence.Substring(startIndex, length);
                continued += table.GetNext(lastSequence);
            }

            return continued;
        }

        public void SerializeToFile(string outputPath)
        {
            string json = JsonSerializer.Serialize(table);
            using StreamWriter outputFile = new(outputPath);
            outputFile.Write(json);
            Console.WriteLine(json);
            table.Print();
        }
    }
}
