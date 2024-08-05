using System;
using System.Collections.Generic;
using System.Text;
using ESCPOS_NET.Emitters;

namespace POSPrinter
{
    public class ESCPOSHelper
    {
        public static string ReplaceParameters(string text, IDictionary<string, string> parameters)
        {
            try
            {
                var temp = (text ?? string.Empty) + "\n\n"; // We need to preview 2 chars maximum!
                var active = false;
                var result = new StringBuilder();
                var parameter = new StringBuilder();

                for (var i = 0; i < temp.Length - 2; i++)
                {
                    if (temp[i] == '#' && temp[i + 1] == '[')
                    {
                        // Use #[[ to escape
                        if (temp[i + 2] == '[')
                        {
                            if (active)
                            {
                                throw new InvalidOperationException("Parameter blocks not defined correctly.");
                            }

                            result.Append("#[");

                            i += 2;
                        }
                        else
                        {
                            if (active)
                            {
                                throw new InvalidOperationException("Nested Parameter blocks not allowed.");
                            }

                            active = true;
                            i++;
                        }

                        continue;
                    }

                    // Use ]]# to escape
                    if (temp[i] == ']' && temp[i + 1] == ']' && temp[i + 2] == '#')
                    {
                        if (active)
                        {
                            throw new InvalidOperationException("Parameter blocks not defined correctly.");
                        }

                        result.Append("]#");

                        i += 2;
                        continue;
                    }

                    if (temp[i] == ']' && temp[i + 1] == '#')
                    {
                        if (!active)
                        {
                            throw new InvalidOperationException("Parameter blocks not defined correctly.");
                        }

                        active = false;
                        i++;

                        var ptext = parameter.ToString().Trim();

                        if (parameters.TryGetValue(ptext, out var value))
                        {
                            result.Append(value);
                        }

                        parameter.Length = 0;
                        continue;
                    }

                    if (active)
                    {
                        parameter.Append(temp[i]);
                    }
                    else
                    {
                        result.Append(temp[i]);
                    }
                }

                return result.ToString();
            }
            catch (Exception e)
            {
                // TODO: Log over Extension Host

                return text;
            }
        }

        public static IList<byte[]> CreateCommands(string text, BaseCommandEmitter emitter, IDictionary<string, string> parameters)
        {
            try
            {
                text = ReplaceParameters(text, parameters);

                var temp = (text ?? string.Empty) + "\n\n"; // We need to preview 2 chars maximum!
                temp = temp.Replace("\r\n", "\n");
                temp = temp.Replace("\r", "\n");

                var active = false;
                var line = new StringBuilder();
                var command = new StringBuilder();
                var byteArrays = new List<byte[]>();

                for (var i = 0; i < temp.Length - 2; i++)
                {
                    if (temp[i] == '\n')
                    {
                        byteArrays.Add(emitter.PrintLine(line.ToString()));
                        line.Length = 0;
                        continue;
                    }

                    if (temp[i] == '~' && temp[i + 1] == '(')
                    {
                        // Use ~(( to escape
                        if (temp[i + 2] == '(')
                        {
                            if (active)
                            {
                                throw new InvalidOperationException("Command blocks not defined correctly.");
                            }

                            line.Append("~(");

                            i += 2;
                        }
                        else
                        {
                            if (active)
                            {
                                throw new InvalidOperationException("Nested commands blocks not allowed.");
                            }

                            active = true;
                            i++;
                        }

                        continue;
                    }

                    // Use ))~ to escape
                    if (temp[i] == ')' && temp[i + 1] == ')' && temp[i + 2] == '~')
                    {
                        if (active)
                        {
                            throw new InvalidOperationException("Command blocks not defined correctly.");
                        }

                        line.Append(")~");

                        i += 2;
                        continue;
                    }

                    if (temp[i] == ')' && temp[i + 1] == '~')
                    {
                        if (!active)
                        {
                            throw new InvalidOperationException("Command blocks not defined correctly.");
                        }

                        active = false;
                        i++;

                        var ltext = line.ToString();

                        if (ltext.Length > 0)
                        {
                            byteArrays.Add(emitter.Print(ltext));
                        }

                        var ctext = command.ToString().Trim().ToLowerInvariant();

                        if (ctext == "centralalign")
                        {
                            if (ltext.Length == 0)
                            {
                                byteArrays.Add(emitter.CenterAlign());
                            }
                            else
                            {
                                // TODO: Log that aligen must start at the line begining
                            }
                        }
                        else if (ctext == "leftalign")
                        {
                            if (ltext.Length == 0)
                            {
                                byteArrays.Add(emitter.LeftAlign());
                            }
                            else
                            {
                                // TODO: Log that aligen must start at the line begining
                            }
                        }
                        else if (ctext == "rightalign")
                        {
                            if (ltext.Length == 0)
                            {
                                byteArrays.Add(emitter.RightAlign());
                            }
                            else
                            {
                                // TODO: Log that align must start at the line begining
                            }
                        }
                        else if (ctext.StartsWith("style"))
                        {
                            var parts = ctext.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length > 1)
                            {
                                var styles = parts[1].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                                var printStyle = PrintStyle.None;

                                foreach (var style in styles)
                                {
                                    var formattedtStyle = style.Trim().ToLowerInvariant();

                                    if (formattedtStyle == "bold")
                                    {
                                        printStyle |= PrintStyle.Bold;
                                    }
                                    else if (formattedtStyle == "italic")
                                    {
                                        printStyle |= PrintStyle.Italic;
                                    }
                                    else if (formattedtStyle == "doublewidth")
                                    {
                                        printStyle |= PrintStyle.DoubleWidth;
                                    }
                                    else if (formattedtStyle == "doubleheight")
                                    {
                                        printStyle |= PrintStyle.DoubleHeight;
                                    }
                                    else
                                    {
                                        // TODO: May log that sytle is unknown?
                                    }
                                }

                                if (printStyle != PrintStyle.None)
                                {
                                    byteArrays.Add(emitter.SetStyles(printStyle));
                                }
                            }
                            else
                            {
                                // TODO: May log that sytle must be defined?
                            }
                        }
                        else if (ctext.StartsWith("barcode"))
                        {
                            var parts = ctext.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length == 2)
                            {
                                var barcodeInfo = parts[1].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                                if (barcodeInfo.Length > 1)
                                {
                                    var barcodeType = Enum.TryParse<BarcodeType>(barcodeInfo[0], true, out var bct) ? bct : BarcodeType.ITF;
                                    var barcodeText = barcodeInfo[1];

                                    if (!string.IsNullOrEmpty(barcodeText))
                                    {
                                        byteArrays.Add(emitter.PrintBarcode(barcodeType, barcodeText));
                                    }
                                    else
                                    {
                                        // TODO: May log problem?
                                    }
                                }
                            }
                        }
                        else if (ctext.StartsWith("feedlines"))
                        {
                            var parts = ctext.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length == 2 && int.TryParse(parts[1], out var lineCount))
                            {
                                byteArrays.Add(emitter.FeedLines(lineCount));
                            }
                        }
                        else if (ctext == "fullcut")
                        {
                            byteArrays.Add(emitter.FullCut());
                        }
                        else if (ctext.StartsWith("fullcutafterfeed"))
                        {
                            var parts = ctext.Split(":".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

                            if (parts.Length == 2 && int.TryParse(parts[1], out var lineCount))
                            {
                                byteArrays.Add(emitter.FullCutAfterFeed(lineCount));
                            }
                        }

                        line.Length = 0;
                        command.Length = 0;
                        continue;
                    }

                    if (active)
                    {
                        command.Append(temp[i]);
                    }
                    else
                    {
                        line.Append(temp[i]);
                    }
                }

                return byteArrays.ToArray();
            }
            catch (Exception e)
            {
                // TODO: May log over Extension Host?

                return Array.Empty<byte[]>();
            }
        }
    }
}