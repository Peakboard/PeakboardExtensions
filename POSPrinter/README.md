# Peakboard extension for POS printers supporting ESC/POS or ZPL protocol
This extension provides two custom lists, one supporting the ESC/POS protocol from EPSON, the other supporting the ZPL from Zebra. POS printers that support either of these protocols can be integrated into a Peakboard application without the need to install a driver.

The ESC/POS source also allows to add the POS printer via the USB port using a virtual serial port. To use the the printer directly a printer driver must be installed. This data source also provides an easy to use and simple way to generate the print format (see below).

Both data sources offer a function called print. The function takes the formatted text as the first parameter and in the case of ESC/POS you can pass a second parameter with tokens to replace in the text.

You can use the Zebra ZPL protocol to create more sophisticated labels by using the direct protocol formats.

# ESC/POS format

The ESC/POS custom format provides a set of easy-to-use commands that are always enclosed in **\~(COMMAND)\~**. The following commands are currently supported:

**\~(CentralAlign)\~**\
Centers all text following the command.

**\~(LeftAlign)\~**\
Left-aligns all text following the command.

**\~(RightAlign)\~**\
Right-aligns all text following the command.

**\~(Style:Bold,Italic,DoubleWidth,DoubleHeight,Underline,FontB,Condensed,Proportional)\~**\
Sets the style of the following text. Not all printers support all styles or combinations of styles.

**\~(Style:None)\~**\
Resets the style to the printer defaults.

**\~(Barcode:CODE128,12345)\~**\
Prints a barcode of type CODE128 with the numbers 12345. You can also use ITF as the barcode type.

**\~(SetBarWidth:Thin)\~**\
Sets the barcode line width. Possible arguments: Thinnest, Thin, Thickest, Thick, Default.

**\~(SetBarcodeHeightInDots:150)\~**\
Sets the barcode height in dots, for example, 150 dots.

**\~(SetBarLabelPosition)\~**\
Enables and sets the position to print the barcode text. Possible arguments: Above, Below, Both, None.

**\~(SetBarLabelFontB:true)\~**\
Uses the second printer font.

**\~(SeikoQRCode:Peakboard)\~**\
Special command to print a QR code with the text "Peakboard" on Seiko printers.

**\~(ReverseMode:true)\~**\
Prints the text in reverse.

**\~(RightCharacterSpacing:10)\~**\
Sets the spacing between characters in dots, for example, 10 dots.

**\~(UpsideDownMode:true)\~**\
Prints the text upside down.

**\~(Image:peakboard_logo.png)\~**\
Prints an image. The image needs to be stored as a local resource and must not be wider than 300px. For best results, use black and white PNG files with a transparent background.

**\~(FullCut)\~**\
Full cut after this command.

**\~(FeedLines:2)\~**\
Feed 3 lines.

**\~(FullCutAfterFeed:3)\~**\
Full cut after a feed of 3 lines.

**\~(PureESCPOS: \x1b\x61\x01\x1b\x21\x10Peakboard Caramel Macchiato\x0A\x1b\x21\x00Size: Grande\x0ACustomizations: Extra Shot, Soy Milk\x0A)\~**\
Use pure ESC/POS. This can be used as the only source or inside our Peakboard Markup to extend the capabilities.

**\~(PosTable:ColumnWidth1,ColumnWidth2,ColumnWidth3:Content)\~**\
The PosTable command allows you to create a table with flexible column widths. The table content is defined using an HTML-like syntax.
**ColumnWidth:** Specify the width of each column in characters.\
**Content:** Define the table rows and cells using \<tr\>, \<th\> for headers, and \<td\> (for data).\
For a detailed implementation example, refer to the Table Sample section below.

## Easy Sample
	~(CentralAlign)~
	Heading with #[param1]#
	~(Style:Bold)~
	Row 2
	Row 3
	~(LeftAlign)~
	~(Style:DoubleHeight)~
	~(Barcode:CODE128,1234567890)~
	Row 4
	~(Style:Bold,Italic,DoubleHeight)~
	Row 5
	~(FullCutAfterFeed:1)~

## Table Sample
	local table = [[
    <tr>
        <th>Item</th>
        <th>Price</th>
        <th>Quantity</th>
    </tr>
    <tr>
        <td>Apple</td>
        <td>1.20 EUR</td>
        <td>10</td>
    </tr>
    <tr>
        <td>Banana</td>
        <td>0.99 EUR</td>
        <td>5</td>
    </tr>
	]]

	'~(PosTable:20,14,14:' .. table .. ')~'
 
## Virtual printer apps for testing
1. iOS App 'Virtual Thermal Printer' by Pascal Kimmel used as a virtual printer for the ESC/POS protocol. Not all commands are supported.
2. Virtual ZPL Printer by Daniel Porrey used as a virtual printer for the Zebra ZPL protocol. Available on GitHub. 

## Screenshots
![image](https://github.com/user-attachments/assets/56dff34a-c3bd-45fe-bada-4459069fe3df)
![image](https://github.com/user-attachments/assets/15b8d1d0-af07-4838-bc85-4654c5db3981)
![image](https://github.com/user-attachments/assets/f9391d90-4714-40ef-8211-bedf3cad6349)
![image](https://github.com/user-attachments/assets/dafd8455-dfc6-4e3e-a455-0135d9fc76ae)
![image](https://github.com/user-attachments/assets/2fda0f64-1b96-4118-a5bb-5a36395cef40)
