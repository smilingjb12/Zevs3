using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Combinatorics.Collections;
using System.Text.RegularExpressions;

namespace Zevs3
{
    public class SymbolCodesGenerator
    {
        private readonly string delimeter;

        public SymbolCodesGenerator(string delimeter)
        {
            this.delimeter = delimeter;
        }

        public List<string> GenerateCombinations()
        {
            int size = 10; // should be enough
            return GenerateBinaryCombinations(size).Where(IsValidCombination).ToList();
        }

        private IEnumerable<string> GenerateBinaryCombinations(int size)
        {
            var result = new List<string>();
            for (int i = 1; i <= size; ++i)
            {
                var variations = new Variations<int>(new[] { 0, 1 }, i, GenerateOption.WithRepetition);
                result.AddRange(variations.Select(v => string.Join("", v)));
            }
            return result;
        }

        private bool IsValidCombination(string value)
        {
            string message = string.Format("{0}{1}{2}", delimeter, value, delimeter);
            string[] splitted = message.Split(new[] { delimeter }, StringSplitOptions.None);
            if (splitted[1] == value) return true;
            return false;
        }
    }

    public class FrequencyTableBuilder
    {
        private readonly string filePath;

        public FrequencyTableBuilder(string filePath)
        {
            this.filePath = filePath;
        }

        public Dictionary<char, double> GenerateFrequencyTable()
        {
            string text = File.ReadAllText(filePath);
            text = Codec.FilterText(text);
            var freqBySymbol = new Dictionary<char, int>();
            foreach (char symbol in text)
            {
                if (!freqBySymbol.ContainsKey(symbol)) freqBySymbol[symbol] = 0;
                freqBySymbol[symbol] += 1;
            }
            return freqBySymbol
                .ToList()
                .OrderByDescending(symbolFreq => symbolFreq.Value)
                .ToDictionary(symbolFreq => symbolFreq.Key, symbolFreq => (double)symbolFreq.Value / text.Length);
        }
    }

    public static class StringExtensions
    {
        public static string ReplaceWithRegex(this string input, string pattern, string replacement)
        {
            return Regex.Replace(input, pattern, replacement);
        }

        public static IEnumerable<string> Chunks(this string input, int size)
        {
            return Enumerable.Range(0, input.Length / size)
                .Select(i => input.Substring(i * size, size));
        }
    }

    public class Codec
    {
        private readonly string delimeter;
        private readonly Dictionary<char, string> codeBySymbol;

        public static string FilterText(string text)
        {
            return text.ToLower()
                .ReplaceWithRegex(@"\s+", " ")
                .ReplaceWithRegex("\n", "")
                .ReplaceWithRegex(@"[^а-яё ]", "");
        }

        public Codec(string delimeter, Dictionary<char, string> codeBySymbol)
        {
            this.delimeter = delimeter;
            this.codeBySymbol = codeBySymbol;
        }

        public string Encode(string text)
        {
            var output = new StringBuilder();
            output.Append(delimeter);
            foreach (char symbol in text)
            {
                if (!codeBySymbol.ContainsKey(symbol)) throw new Exception("Unknown symbol");
                output.Append(codeBySymbol[symbol]);
                output.Append(delimeter);
            }
            return output.ToString();
        }

        public string Decode(string text)
        {
            string[] symbolCodes = text.Split(new[] { delimeter }, StringSplitOptions.RemoveEmptyEntries);
            var output = new StringBuilder();
            Dictionary<string, char> symbolByCode = codeBySymbol.ToDictionary(pair => pair.Value, pair => pair.Key);
            foreach (string symbolCode in symbolCodes)
            {
                if (!symbolByCode.ContainsKey(symbolCode)) throw new Exception("Unknown symbol");
                output.Append(symbolByCode[symbolCode]);
            }
            return output.ToString();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string delimeter = "10";
            string filePath = "text.txt";
            Console.WriteLine("Delimeter:");
            Console.WriteLine(delimeter);
            var codesGenerator = new SymbolCodesGenerator(delimeter: delimeter);
            IEnumerable<string> codes = codesGenerator.GenerateCombinations();
            var frequencyTableBuilder = new FrequencyTableBuilder(filePath);
            Dictionary<char, double> freqBySymbol = frequencyTableBuilder.GenerateFrequencyTable();
            Console.WriteLine("Frequencies:");
            freqBySymbol.ToList().ForEach(pair => Console.WriteLine("{0} => {1:0.00}%", pair.Key, pair.Value * 100));
            Console.WriteLine();

            Dictionary<char, string> codeBySymbol = freqBySymbol
                .Select(symbolFreq => symbolFreq.Key)
                .Zip(codes, (symbol, code) => new { Symbol = symbol, Code = code })
                .ToDictionary(symbolCode => symbolCode.Symbol, symbolCode => symbolCode.Code);
            Console.WriteLine("Symbol-code mappings:");
            codeBySymbol.ToList().ForEach(pair => Console.WriteLine("{0} => {1}", pair.Key, pair.Value));
            Console.WriteLine();

            var codec = new Codec(delimeter, codeBySymbol);
            bool manualInput = false;
            Console.WriteLine("Manual input?:");
            if (Console.ReadLine().Contains('y')) manualInput = true;
            string text = null;
            if (manualInput)
            {
                Console.WriteLine("Input text:");
                text = Console.ReadLine();
            }
            else
            {
                text = File.ReadAllText(filePath);
            }
            text = Codec.FilterText(text);

            string encoded = codec.Encode(text);
            Console.WriteLine("Encoded text:");
            Console.WriteLine(string.Join(" ", encoded.Chunks(size: 8)));
            string decoded = codec.Decode(encoded);
            Console.WriteLine("Decoded text:");
            Console.WriteLine(decoded);

            Console.WriteLine();
            Console.WriteLine("Original text length: {0}", text.Count() * 8);
            Console.WriteLine("Encoded text length: {0}", encoded.Count());
            Console.WriteLine("Compression: {0:0.00}", (double)(text.Count() * 8) / encoded.Count());
        }
    }
}
