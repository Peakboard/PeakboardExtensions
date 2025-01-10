using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESCPOS_NET.Emitters;
using POSPrinter.Helper;

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
                                throw new InvalidOperationException(
                                    "Parameter blocks not defined correctly."
                                );
                            }

                            result.Append("#[");

                            i += 2;
                        }
                        else
                        {
                            if (active)
                            {
                                throw new InvalidOperationException(
                                    "Nested Parameter blocks not allowed."
                                );
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
                            throw new InvalidOperationException(
                                "Parameter blocks not defined correctly."
                            );
                        }

                        result.Append("]#");

                        i += 2;
                        continue;
                    }

                    if (temp[i] == ']' && temp[i + 1] == '#')
                    {
                        if (!active)
                        {
                            throw new InvalidOperationException(
                                "Parameter blocks not defined correctly."
                            );
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

        public static IList<byte[]> CreateCommands(
            string text,
            BaseCommandEmitter emitter,
            IDictionary<string, string> parameters
        )
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
                                throw new InvalidOperationException(
                                    "Command blocks not defined correctly."
                                );
                            }

                            line.Append("~(");

                            i += 2;
                        }
                        else
                        {
                            if (active)
                            {
                                throw new InvalidOperationException(
                                    "Nested commands blocks not allowed."
                                );
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
                            throw new InvalidOperationException(
                                "Command blocks not defined correctly."
                            );
                        }

                        line.Append(")~");

                        i += 2;
                        continue;
                    }

                    if (temp[i] == ')' && temp[i + 1] == '~')
                    {
                        if (!active)
                        {
                            throw new InvalidOperationException(
                                "Command blocks not defined correctly."
                            );
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
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length > 1)
                            {
                                var styles = parts[1]
                                    .Split(
                                        ",".ToCharArray(),
                                        StringSplitOptions.RemoveEmptyEntries
                                    );
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
                                    else if (formattedtStyle == "none")
                                    {
                                        printStyle |= PrintStyle.None;
                                    }
                                    else if (formattedtStyle == "underline")
                                    {
                                        printStyle |= PrintStyle.Underline;
                                    }
                                    else if (formattedtStyle == "fontb")
                                    {
                                        printStyle |= PrintStyle.FontB;
                                    }
                                    else if (formattedtStyle == "condensed")
                                    {
                                        printStyle |= PrintStyle.Condensed;
                                    }
                                    else if (formattedtStyle == "proportional")
                                    {
                                        printStyle |= PrintStyle.Proportional;
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
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length == 2)
                            {
                                var barcodeInfo = parts[1]
                                    .Split(
                                        ",".ToCharArray(),
                                        StringSplitOptions.RemoveEmptyEntries
                                    );

                                if (barcodeInfo.Length > 1)
                                {
                                    var barcodeType = Enum.TryParse<BarcodeType>(
                                        barcodeInfo[0],
                                        true,
                                        out var bct
                                    )
                                        ? bct
                                        : BarcodeType.ITF;
                                    var barcodeText = barcodeInfo[1];                                   

                                    if (!string.IsNullOrEmpty(barcodeText))
                                    {
                                        if (barcodeInfo.Length == 3)
                                        {
                                            var barcodeCode = BarcodeCode.CODE_A;
                                            switch (barcodeInfo[2].ToLowerInvariant())
                                            {
                                                case "codea":
                                                    barcodeCode = BarcodeCode.CODE_A;
                                                    break;
                                                case "codeb":
                                                    barcodeCode = BarcodeCode.CODE_B;
                                                    break;
                                                case "codec":
                                                    barcodeCode = BarcodeCode.CODE_C;
                                                    break;
                                                default:
                                                    barcodeCode = BarcodeCode.CODE_A;
                                                    break;
                                            }     

                                            byteArrays.Add(emitter.PrintBarcode(barcodeType, barcodeText, barcodeCode));

                                        } 
                                        else
                                        {
                                            byteArrays.Add(emitter.PrintBarcode(barcodeType, barcodeText));
                                        }

                                    }
                                    else
                                    {
                                        // TODO: May log problem?
                                    }
                                }
                            }
                        }
                        else if (ctext.StartsWith("seikoqrcode"))
                        {
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length >= 2)
                            {
                                if (!string.IsNullOrEmpty(parts[1]))
                                {
                                    SeikoQRCode seikoQRCode = new SeikoQRCode();
                                    string qrdata = "";

                                    for (int j = 1; j < parts.Length; j++) qrdata += parts[j] + ":";

                                    qrdata = qrdata.Substring(0, qrdata.Length - 1);

                                    byte[] qrcode = seikoQRCode.QRCode(qrdata);
                                    byteArrays.Add(qrcode);
                                }
                                else
                                {
                                    // TODO: May log problem?
                                }
                            }
                        }
                        else if (ctext.StartsWith("pureescpos"))
                        {
                            var parts = command
                                .ToString()
                                .Split(new[] { ':' }, 2, StringSplitOptions.None) // Split in maximal zwei Teile
                                .Where(part => !string.IsNullOrWhiteSpace(part)) // Filtere leere Einträge
                                .ToArray();



                            if (parts.Length >= 2)
                            {

                                
                                if (!string.IsNullOrEmpty(parts[1]))
                                {
                                    PureESCPosBuilder pureESCPosBuilder = new PureESCPosBuilder();
                                    byte[] pureESC = pureESCPosBuilder.BuildEscPosArray(parts[1]);
                                    byteArrays.Add(pureESC);
                                }
                                else
                                {
                                    // TODO: May log problem?
                                }
                            }
                        }
                        else if (ctext.StartsWith("feedlines"))
                        {
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

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
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length == 2 && int.TryParse(parts[1], out var lineCount))
                            {
                                byteArrays.Add(emitter.FullCutAfterFeed(lineCount));
                            }
                        }
                        else if (ctext.StartsWith("reversemode"))
                        {
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length == 2 && bool.TryParse(parts[1], out var state))
                            {
                                byteArrays.Add(emitter.ReverseMode(state));
                            }
                        }
                        else if (ctext.StartsWith("rightcharacterspacing"))
                        {
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length == 2 && int.TryParse(parts[1], out var spacing))
                            {
                                byteArrays.Add(emitter.RightCharacterSpacing(spacing));
                            }
                        }
                        else if (ctext.StartsWith("upsidedownmode"))
                        {
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length == 2 && bool.TryParse(parts[1], out var state))
                            {
                                byteArrays.Add(emitter.UpsideDownMode(state));
                            }
                        }
                        else if (ctext.StartsWith("setbarwidth"))
                        {
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length == 2)
                            {
                                var width = BarWidth.Default;
                                switch (parts[1].ToLowerInvariant())
                                {                                                                      
                                    case "thinnest":
                                        width = BarWidth.Thinnest;
                                        break;
                                    case "thin":
                                        width = BarWidth.Thin;
                                        break;
                                    case "thickest":
                                        width = BarWidth.Thickest;
                                        break;
                                    case "thick":
                                        width = BarWidth.Thick;
                                        break;
                                    default:
                                        width = BarWidth.Default;
                                        break;
                                }
                                byteArrays.Add(emitter.SetBarWidth(width));
                            }
                        }
                        else if (ctext.StartsWith("setbarlabelposition"))
                        {
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length == 2)
                            {
                                var position = BarLabelPrintPosition.None;
                                switch (parts[1].ToLowerInvariant())
                                {
                                    case "above":
                                        position = BarLabelPrintPosition.Above;
                                        break;
                                    case "below":
                                        position = BarLabelPrintPosition.Below;  
                                        break;
                                    case "both":
                                        position = BarLabelPrintPosition.Both;
                                        break;
                                    default:
                                        position = BarLabelPrintPosition.None;
                                        break;
                                }
                                byteArrays.Add(emitter.SetBarLabelPosition(position));
                            }
                        }
                        else if (ctext.StartsWith("setbarcodeheightindots"))
                        {
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length == 2 && int.TryParse(parts[1], out var height))
                            {
                                byteArrays.Add(emitter.SetBarcodeHeightInDots(height));
                            }
                        }
                        else if (ctext.StartsWith("setbarlabelfontb"))
                        {
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length == 2 && bool.TryParse(parts[1], out var state))
                            {
                                byteArrays.Add(emitter.SetBarLabelFontB(state));
                            }
                        }
                        else if (ctext.StartsWith("image"))
                        {
                            var parts = ctext.Split(
                                ":".ToCharArray(),
                                StringSplitOptions.RemoveEmptyEntries
                            );

                            if (parts.Length == 2)
                            {
                                ImageToEscPos imageToEscPos = new ImageToEscPos();
                                byte[] image = imageToEscPos.Image(parts[1]);

                                if (image != null)
                                {
                                    byteArrays.Add(image);
                                }
                                else
                                {
                                    byteArrays.Add(
                                        emitter.Print(
                                            "Please check your image resource and make sure that the width of your image is not greater than 300 pixels.\n"
                                        )
                                    );
                                }
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
