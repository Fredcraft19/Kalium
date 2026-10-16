using System;
using System.Globalization;
using System.IO;
using System.Diagnostics;


namespace WispAOT
{
    public static class Assembler
    {
        private static List<byte> bytecode = new();
        private static void reset()
        {
            bytecode = new();
        }

        private static string errorMessage = "";

        /// <summary>
        /// Assembles assembly source into a fileName.wbc file
        /// </summary>
        /// <param name="source">The Assembly code that is to be turned into Wisp Byte Code (wbc)</param>
        /// <param name="fileName">The name of the file, without file extension</param>
        public static void AssembleToFile(string source, string fileName)
        {
            reset();
            Stopwatch timer = Stopwatch.StartNew();
            List<string> lines = new();
            string line = "";
            foreach(char ch in source)
            {
                if(ch == '\n')
                {
                    lines.Add(line);
                    line = "";   
                }
                else
                {
                    line += ch;
                }
            }
            lines.Add(line);
            line = "";

            for(int i = 0; i < lines.Count; i++)
            {
                if(errorMessage.Length < 1)
                {
                    // Clears whitespace and assembles it
                    assembleLine( lines[i].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    string lineNumber = i.ToString();
                    int padding = Math.Max(2, lineNumber.Length);
                    Console.WriteLine($"{lineNumber.PadLeft(padding)} | {lines[i-1]}");
                    Console.WriteLine($"{new string(' ', padding)} | {errorMessage}");
                    Console.ResetColor();
                    i = lines.Count;
                    bytecode = [0xFF];
                    timer.Stop();
                    return;
                }
            }

            timer.Stop();
            
            File.WriteAllBytes($"{fileName}.wbc", bytecode.ToArray());

            Console.WriteLine($"Total Assembly Time: {timer.ElapsedMilliseconds}ms");
        }

        private static void assembleLine(string[] asm)
        {
            List<byte> bc = new();

            if(asm.Length < 1)
            {
                return;
            }

            if (BytecodeMap.bytecode.ContainsKey(asm[0]))
            {
                // Its a command
                oc command = (oc)BytecodeMap.bytecode[asm[0]];
                bc.Add((byte)command);

                switch (command)
                {
                    // Command requireing a variable type hardcoded argument in bytecode
                    case oc.PUSH_CONST:
                        {
                            if(asm.Length > 1)
                            {
                                byte[] data = handleNumber(asm[1], false);
                                bc = [..bc, ..data];
                            }
                            else
                            {
                                errorMessage += $"Invalid argument(s) for command";
                            }
                            break;
                        }   

                    // Command requireing a uint32 hardcoded argument in bytecode
                    case oc.ENTER:
                    case oc.JUMP:
                    case oc.JUMPZ:
                    case oc.JUMPNZ:
                    case oc.REF:
                    case oc.LOAD_F64:
                    case oc.LOAD_F32:
                    case oc.LOAD_U64:
                    case oc.LOAD_U32:
                    case oc.LOAD_U16:
                    case oc.LOAD_U8:
                    case oc.LOAD_S64:
                    case oc.LOAD_S32:
                    case oc.LOAD_S16:
                    case oc.LOAD_S8:
                    case oc.STORE_F64:
                    case oc.STORE_F32:
                    case oc.STORE_64:
                    case oc.STORE_32:
                    case oc.STORE_16:
                    case oc.STORE_8:
                        {
                            if(asm.Length > 1)
                            {
                                byte[] data = handleNumber<uint>(asm[1]);
                                bc = [..bc, ..data];
                            }
                            else
                            {
                                errorMessage += $"Invalid argument(s) for command";
                            }
                            break;
                        }
                }

                bytecode = [..bytecode, ..bc];
            }
            else
            {
                if(asm[0][asm[0].Length-1] == ':')
                {
                    // Its a Label Declaration

                }
                else
                {
                    errorMessage += $"Unknown command or label declaration with '{asm[0]}'.";
                }
            }
        }

        private static byte[] handleNumber(string num, bool is32bit)
        {

            if(num[num.Length-1] == 'f')
            {
                // Float
                string value = num.Substring(0, num.Length-1);
                if (is32bit)
                {
                    if(float.TryParse(value, out var f))
                    {
                        return BitConverter.GetBytes(f);
                    }
                    else
                    {
                        errorMessage += $"Failed to parse value '{value}' into a float32.";
                    }
                }
                else
                {
                    if(double.TryParse(value, out var d))
                    {
                        return BitConverter.GetBytes(d);
                    }
                    else
                    {
                        errorMessage += $"Failed to parse value '{value}' into a float64.";
                    }
                }

            }
            else if(num[num.Length-1] == 's')
            {
                string value = num.Substring(0, num.Length-1);
                // Int
                if (is32bit)
                {
                    if(int.TryParse(value, out var i))
                    {
                        return BitConverter.GetBytes(i);
                    }
                    else
                    {
                        errorMessage += $"Failed to parse value '{value}' into a int32.";
                    }
                }
                else
                {
                    if(long.TryParse(value, out var i))
                    {
                        return BitConverter.GetBytes(i);
                    }
                    else
                    {
                        errorMessage += $"Failed to parse value '{value}' into a int64.";
                    }
                }
            }
            else
            {
                // Uint
                if (is32bit)
                {
                    if(uint.TryParse(num, out var u))
                    {
                        return BitConverter.GetBytes(u);
                    }
                    else
                    {
                        errorMessage += $"Failed to parse value '{num}' into a uint32.";
                    }
                }
                else
                {
                    if(ulong.TryParse(num, out var u))
                    {
                        return BitConverter.GetBytes(u);
                    }
                    else
                    {
                        errorMessage += $"Failed to parse value '{num}' into a uint64.";
                    }
                }
            }
            return [];
        }

        private static byte[] handleNumber<T>(string num) where T : IParsable<T>
        {
            byte[] data = [];

            if (string.IsNullOrWhiteSpace(num))
            {
                errorMessage += $"Not enough arguments for command.";
            }

            string value = num;

            if (!char.IsNumber(num[num.Length - 1]))
            {
                //value = num.Substring(0, num.Length-1);
                errorMessage += "Must use uint type for this argument. (no 'i' or 'f' at the end of the number)";
            }

            if(T.TryParse(num, null, out T? res))
            {
                if(res == null)
                {
                    errorMessage += $"Null 'T' when parsing '{value}'";
                }
                data = res switch
                {
                    double x => BitConverter.GetBytes(x),
                    float x =>  BitConverter.GetBytes(x),
                    int x =>    BitConverter.GetBytes(x),
                    long x =>   BitConverter.GetBytes(x),
                    uint x =>   BitConverter.GetBytes(x),
                    ulong x =>  BitConverter.GetBytes(x),
                    _ => []
                };

                if(data.Length == 0)
                {
                    errorMessage += $"Unable to parse '{value}' to number (original: '{num}')";
                }
            }

            return data;
        }

    }
}