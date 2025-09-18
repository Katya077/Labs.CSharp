using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;

struct GeneticData
{
    public string protein;
    public string organism;
    public string amino_acids;
}

class Program
{
    static List<GeneticData> data = new List<GeneticData>();
    static List<string> outputLines = new List<string>();
    static int operationCounter = 1;

    static HashSet<char> validAminoAcids = new HashSet<char> { 'A', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'K', 'L', 'M', 'N', 'P', 'Q', 'R', 'S', 'T', 'V', 'W', 'Y' };

    static void Main(string[] args)
    {
  
        ReadGeneticData("sequences.0.txt");
        var commands = File.ReadAllLines("commands.0.txt");

        outputLines.Add("Katya");
        outputLines.Add("Генетический поиск");

        foreach (var commandLine in commands)
        {
            if (string.IsNullOrWhiteSpace(commandLine)) continue;

            var parts = commandLine.Split('\t');
            if (parts.Length == 0) continue;

            string command = parts[0].Trim();

            switch (command)
            {
                case "search":
                    if (parts.Length >= 2)
                    {
                        string searchQuery = parts[1].Trim();
                        string decodedQuery = RLDecoding(searchQuery);
                        AddOperationSeparator($"search\t{decodedQuery}");
                        SearchCommand(searchQuery);
                    }
                    break;

                case "diff":
                    if (parts.Length >= 3)
                    {
                        string protein1 = parts[1].Trim();
                        string protein2 = parts[2].Trim();
                        AddOperationSeparator($"diff\t{protein1}\t{protein2}");
                        DiffCommand(protein1, protein2);
                    }
                    break;

                case "mode":
                    if (parts.Length >= 2)
                    {
                        string proteinName = parts[1].Trim();
                        AddOperationSeparator($"mode\t{proteinName}");
                        ModeCommand(proteinName);
                    }
                    break;
            }
            operationCounter++;
        }

        File.WriteAllLines("genedata.txt", outputLines);
        Console.WriteLine("Обработка завершена. Результаты записаны в genedata.txt");
    }

    static void ReadGeneticData(string filename)
    {
        try
        {
            var lines = File.ReadAllLines(filename);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split('\t');
                if (parts.Length >= 3)
                {
                    data.Add(new GeneticData
                    {
                        protein = parts[0].Trim(),
                        organism = parts[1].Trim(),
                        amino_acids = parts[2].Trim()
                    });
                }
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"Файл {filename} не найден");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при чтении файла {filename}: {ex.Message}");
        }
    }

    static void AddOperationSeparator(string commandLine = "")
    {
        outputLines.Add("---------------------------------------------------------------------");
        if (!string.IsNullOrEmpty(commandLine))
        {
            outputLines.Add($"{operationCounter:D3}\t{commandLine}");
        }
        else
        {
            outputLines.Add($"{operationCounter:D3} ");
        }
    }

    static void SearchCommand(string originalQuery)
    {
        var found = new List<(string organism, string protein)>();
        foreach (var g in data)
        {
            if (g.amino_acids.Contains(originalQuery))
                found.Add((g.organism, g.protein));
        }

        if (found.Count == 0)
        {
            outputLines.Add("organism\t\t\tprotein");
            outputLines.Add("NOT FOUND");
        }
        else
        {
            outputLines.Add("organism\t\t\tprotein");

            foreach (var t in found)
            {
                outputLines.Add($"{t.organism}\t{t.protein}");
            }
        }
    }

    static void DiffCommand(string p1, string p2)
    {
        var g1 = data.FirstOrDefault(x => x.protein.Equals(p1, StringComparison.OrdinalIgnoreCase));
        var g2 = data.FirstOrDefault(x => x.protein.Equals(p2, StringComparison.OrdinalIgnoreCase));

        bool missing1 = string.IsNullOrEmpty(g1.protein);
        bool missing2 = string.IsNullOrEmpty(g2.protein);

        outputLines.Add("amino-acids difference:");

        if (missing1 || missing2)
        {
            string missing = "";
            if (missing1) missing += p1 + " ";
            if (missing2) missing += p2 + " ";
            outputLines.Add($"Отсутствует: {missing.Trim()}");
        }
        else
        {
            int diff = AminoDifference(g1.amino_acids, g2.amino_acids);
            outputLines.Add(diff.ToString());
        }
    }

    static void ModeCommand(string pname)
    {
        var g = data.FirstOrDefault(x => x.protein.Equals(pname, StringComparison.OrdinalIgnoreCase));
        outputLines.Add("amino-acids occurs: ");

        if (string.IsNullOrEmpty(g.protein))
        {
            outputLines.Add($"Отсутствует: {pname}");
        }
        else
        {
            var (am, count) = ModeAminoAcid(g.amino_acids);
            outputLines.Add($"{am}\t\t {count} ");
        }
    }

    static string RLDecoding(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (char.IsDigit(c))
            {
                int n = c - '0';
                if (i + 1 < s.Length)
                {
                    char a = s[i + 1];
                    for (int k = 0; k < n; k++) sb.Append(a);
                    i++;
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    static string RLEncoding(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var sb = new StringBuilder();
        int i = 0;
        while (i < s.Length)
        {
            char c = s[i];
            int j = i + 1;
            while (j < s.Length && s[j] == c && j - i < 9) j++;
            int run = j - i;
            if (run >= 3)
            {
                sb.Append(run);
                sb.Append(c);
            }
            else
            {
                for (int k = 0; k < run; k++) sb.Append(c);
            }
            i = j;
        }
        return sb.ToString();
    }

    static int AminoDifference(string a, string b)
    {
        int max = Math.Max(a.Length, b.Length);
        int diff = 0;
        for (int i = 0; i < max; i++)
        {
            char ca = i < a.Length ? a[i] : '\0';
            char cb = i < b.Length ? b[i] : '\0';
            if (ca != cb) diff++;
        }
        return diff;
    }

    static (char am, int count) ModeAminoAcid(string s)
    {
        var counts = new Dictionary<char, int>();
        foreach (char c in s)
        {
            if (!counts.ContainsKey(c)) counts[c] = 0;
            counts[c]++;
        }
        if (counts.Count == 0) return ('?', 0);
        int max = counts.Values.Max();
        var candidates = counts.Where(p => p.Value == max).Select(p => p.Key).ToList();
        candidates.Sort();
        return (candidates[0], max);
    }

    static bool IsValidAminoAcidSequence(string sequence)
    {
        return sequence.All(c => validAminoAcids.Contains(c));
    }

    static void SearchAminoAcids(string query)
    {
        var found = new List<(string organism, string protein, string amino_acids)>();
        foreach (var g in data)
        {
            if (g.amino_acids.Contains(query))
                found.Add((g.organism, g.protein, g.amino_acids));
        }

        if (found.Count == 0)
        {
            Console.WriteLine("Не найдено");
        }
        else
        {
            foreach (var t in found)
            {
                Console.WriteLine($"{t.organism}\t{t.protein}\t{t.amino_acids}");
            }
        }
    }

    static void findModeAminoAcid(string pname)
    {
        var g = data.FirstOrDefault(x => x.protein == pname);
        if (string.IsNullOrEmpty(g.protein))
        {
            Console.WriteLine("Отсутствует: " + pname);
        }
        else
        {
            var (aa, count) = ModeAminoAcid(g.amino_acids);
            Console.WriteLine("Аминокислота встречается: " + aa + " " + count);
        }
    }

    string test(StringBuilder sb)
    {
        string res = sb.ToString();
        foreach (char c in res)
        {
            if (c == 'a')
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}