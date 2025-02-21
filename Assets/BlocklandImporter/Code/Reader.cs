using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

namespace Blockland
{
    public class Reader : StreamReader
    {
        public string Line => lineCache;
        public IReadOnlyList<Run> Runs => stringRuns;
        public IEnumerable<string> RunStrings
        {
            get
            {
                foreach (Run run in Runs)
                {
                    yield return lineCache.Substring(run.start, run.count);
                }
            }
        }
        string lineCache;
        List<Run> stringRuns;
        public Reader(Stream stream) : base(stream)
        {
            stringRuns = new();
            //sb = new StringBuilder();
        }
        public override string ReadLine()
        {
            lineCache = base.ReadLine();

            SetStringRuns(lineCache);

            return lineCache;
        }
        public void SkipLine()
        {
            base.ReadLine();
            SetStringRuns(string.Empty);
        }
        bool IsComment(string line)
        {
            for (int i = 0; i < line.Length; i++)
            {
                if (char.IsWhiteSpace(line[i])) continue;

                return line[i] == '/';
            }

            return true;
        }
        public string ReadNextNonEmptyLine(bool skipComments = true)
        {
            while (!base.EndOfStream)
            {
                string line = base.ReadLine();
                bool blank = string.IsNullOrEmpty(line);
                bool isComment = IsComment(line);
                bool doSkip = (skipComments && isComment);
                if (!blank && !doSkip)
                {
                    SetStringRuns(line);
                    return line;
                }
            }

            SetStringRuns(string.Empty);
            return string.Empty;
        }
        public void SkipNonEmptyLine(int count, bool skipComments = true)
        {
            for (int i = 0; i < count; i++)
                ReadNextNonEmptyLine(skipComments);
        }
        public void SetStringRuns(string line)
        {
            lineCache = line;
            int startIndex = 0;
            stringRuns.Clear();
            bool startedRun = false;
            for (int i = 0; i < lineCache.Length; i++)
            {
                char c = lineCache[i];
                bool whiteSpace = char.IsWhiteSpace(c);
                bool endOfLine = i == lineCache.Length - 1;

                if (!whiteSpace && !startedRun)
                {
                    // begin new run
                    startedRun = true;
                    startIndex = i;
                }
                else if ((whiteSpace && startedRun) || (endOfLine && startedRun))
                {
                    // push to stringRuns
                    startedRun = false;
                    int count = i - startIndex;
                    if (endOfLine)
                        count++;
                    stringRuns.Add(new Run { count = count, start = startIndex });
                }
            }

            if (startedRun)
            {
                stringRuns.Add(new Run { count = line.Length - startIndex, start = startIndex });
            }
        }
        public bool TryParseLineElement(int index, out int value)
        {
            value = 0;
            if (!TryParseLineElement(index, out string str)) return false;

            return int.TryParse(str, out value);
        }
        public bool TryParseLineElement(int index, out float value)
        {
            value = 0;
            if (!TryParseLineElement(index, out string str)) return false;

            return float.TryParse(str, out value);
        }
        public bool TryParseLineElement(int index, out string value)
        {
            if (index < stringRuns.Count)
            {
                Run run = stringRuns[index];
                if (run.start + run.count <= lineCache.Length)
                {
                    value = lineCache.Substring(run.start, run.count);
                    return true;
                }
            }

            value = string.Empty;
            return false;
        }
        public float ParseLineFloat(int index)
        {
            if (TryParseLineElement(index, out float value))
                return value;

            return 0;
        }
        public int ParseLineInt(int index)
        {
            if (TryParseLineElement(index, out int value))
                return value;

            return 0;
        }
        public struct Run
        {
            public int start;
            public int count;
        }
    }
}
